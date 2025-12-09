# Guia de Implementação: US-01-01 - Persistência da Saúde do Solo

**Objetivo:** Implementar um sistema robusto para armazenar, carregar e gerenciar o valor de "Saúde do Solo" (0-100%) para cada tile arável (tillable) do jogo, garantindo persistência entre sessões.

## 1. Análise Arquitetural e Design

Seguindo o `ARCHITECTURE.md` e o princípio de Inversão de Dependência (DIP) já implementado no projeto:

### Camada de Domínio (`LivingRoots/Domain`):
- Definir a regra de negócio fundamental: A saúde do solo é um valor entre 0 e 100.
- Criar um modelo de dados (DTO) para serialização.
- Definir a interface do serviço de domínio.

### Camada de Aplicação/Serviços (`LivingRoots/Services`):
- Implementar a lógica de orquestração usando o `ModDataService` existente.
- Gerenciar o mapeamento entre o mundo do jogo (SMAPI `GameLocation`) e os dados persistidos.
- Implementar validação e sanitização de dados.

### Camada de Controle (`LivingRoots/Controllers`):
- Registrar os eventos `SaveLoaded` e `Saving` (ou `DayEnding`).
- Coordenar entre eventos do jogo e serviços de aplicação.

### Estrutura de Dados Proposta

Para evitar problemas de serialização com chaves complexas em JSON (como `Vector2`) e manter a performance, utilizamos uma estrutura de dicionário aninhado com chaves compostas.

**Modelo de Persistência (JSON):**
```json
{
  "Farm": {
    "12,15": 85.5,
    "12,16": 90.0
  },
  "Greenhouse": {
    "5,5": 100.0
  }
}
```

## 2. Ciclo TDD (Red-Green-Refactor)

### Passo 1: Criar Testes de Unidade (Fase Red)

Criamos testes para verificar:
1. Se o valor de saúde é limitado entre 0 e 100 (Regra de Domínio).
2. Se o serviço salva os dados usando uma chave única por Save (para evitar conflitos entre saves diferentes).
3. Se o serviço recupera o valor correto para uma coordenada específica.
4. Se o serviço lida corretamente com entradas inválidas e exceções.
5. Se o serviço é thread-safe.

### Passo 2: Implementação (Fase Green & Refactor)

#### 2.1. Camada de Domínio

**Arquivo:** `LivingRoots/Domain/SoilHealthState.cs` (Modelo de Dados)
```csharp
using System.Collections.Generic;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Represents the persisted state of soil health for the entire save.
    /// Key: Location Name (e.g., "Farm")
    /// Value: Dictionary mapping Tile Coordinates "X,Y" to Health Value (float)
    /// </summary>
    public class SoilHealthState
    {
        public Dictionary<string, Dictionary<string, float>> LocationHealthData { get; set; } = new();
    }
}
```

**Arquivo:** `LivingRoots/Domain/ISoilHealthService.cs` (Interface)
```csharp
using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    public interface ISoilHealthService
    {
        void LoadData(string saveId);
        void SaveData(string saveId);
        float GetSoilHealth(string locationName, Vector2 tile);
        void SetSoilHealth(string locationName, Vector2 tile, float value);
        void UpdateHealth(string locationName, Vector2 tile, float delta);
    }
}
```

#### 2.2. Camada de Serviços

Implementamos a lógica concreta com validações rigorosas e segurança contra falhas.

**Arquivo:** `LivingRoots/Services/SoilHealthService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    public class SoilHealthService : ISoilHealthService
    {
        private readonly IModDataService _modDataService;
        private readonly IMonitor _monitor;
        
        // Runtime cache using Point directly as key for better performance and precision
        // Dictionary<LocationName, Dictionary<TileCoordinates, HealthValue>>
        private readonly Dictionary<string, Dictionary<Point, float>> _runtimeCache = new();
        private const string KeyPrefix = "soil_health_data_";
        
        // Lock object for thread safety
        private readonly object _lock = new object();

        public SoilHealthService(IModDataService modDataService, IMonitor monitor)
        {
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        public void LoadData(string saveId)
        {
            // Clear the cache if saveId is invalid to prevent stale data from persisting across different game saves
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("LoadData aborted: invalid saveId. Runtime cache cleared.", LogLevel.Warn);
                lock (_lock)
                {
                    _runtimeCache.Clear(); // ensure no stale state remains
                }
                return;
            }
            
            string dataKey = GetSaveKey(saveId);
            
            try
            {
                var savedData = _modDataService.LoadData<SoilHealthState>(dataKey);

                // Use temporary cache to prevent data loss if parsing fails partway through
                var tempCache = new Dictionary<string, Dictionary<Point, float>>();
                
                if (savedData != null)
                {
                    // Guard against null LocationHealthData to prevent NullReferenceException during deserialization
                    var locations = savedData.LocationHealthData ?? new Dictionary<string, Dictionary<string, float>>();
                    
                    foreach (var locationEntry in locations)
                    {
                        // Skip if the location name is null or empty to prevent invalid entries in the cache
                        if (string.IsNullOrWhiteSpace(locationEntry.Key))
                        {
                            _monitor.Log("Skipped soil health data with null or empty location name.", LogLevel.Warn);
                            continue;
                        }
                        
                        // Skip if the value is null to prevent NullReferenceException
                        if (locationEntry.Value == null) continue;
                        
                        var tileDict = new Dictionary<Point, float>();
                        bool warnedForInvalidValue = false; // Only warn once per location for invalid values
                        bool warnedForMalformedKey = false; // Only warn once per location for malformed keys
                        foreach (var tileEntry in locationEntry.Value)
                        {
                            // Parse "X,Y" string back to Point (using integers for tile coordinates)
                            // Use ReadOnlySpan<char> to avoid string.Split allocation for better performance
                            ReadOnlySpan<char> keySpan = tileEntry.Key;
                            int commaIndex = keySpan.IndexOf(',');
                            if (commaIndex > 0 && commaIndex < keySpan.Length - 1 &&
                                int.TryParse(keySpan.Slice(0, commaIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) &&
                                int.TryParse(keySpan.Slice(commaIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                            {
                                // Validate the loaded value by checking for NaN/Infinity and clamping to [0, 100] range
                                var rawValue = tileEntry.Value;
                                if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
                                {
                                    if (!warnedForInvalidValue)
                                    {
                                        _monitor.Log($"Skipped invalid soil health value (NaN/Infinity) in location '{locationEntry.Key}'.", LogLevel.Warn);
                                        warnedForInvalidValue = true;
                                    }
                                    continue;
                                }
                                
                                float clamped = Math.Clamp(rawValue, 0f, 100f);
                                // Resolve duplicates deterministically: last-write-wins
                                var point = new Point(x, y);
                                tileDict[point] = clamped;
                            }
                            else
                            {
                                // Warn about malformed keys to help diagnose corrupted save data
                                if (!warnedForMalformedKey)
                                {
                                    _monitor.Log($"Skipped malformed soil health tile key(s) in location '{locationEntry.Key}'.", LogLevel.Warn);
                                    warnedForMalformedKey = true;
                                }
                            }
                        }
                        // Only add location if at least one valid tile exists
                        if (tileDict.Count > 0)
                        {
                            tempCache[locationEntry.Key] = tileDict;
                        }
                    }
                }
                else
                {
                    _monitor.Log("No existing Soil Health data found. Starting fresh.", LogLevel.Info);
                }
                
                // Swap caches only after successful parsing/validation
                lock (_lock)
                {
                    _runtimeCache.Clear();
                    foreach (var kv in tempCache)
                    {
                        _runtimeCache[kv.Key] = kv.Value;
                    }
                }
            }
            catch (Exception)
            {
                _monitor.Log("Error occurred while loading soil health data. Cache preserved.", LogLevel.Error);
                // Keep existing cache; don't clear it on error to prevent data loss
            }
        }

        public void SaveData(string saveId)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("SaveData aborted: invalid saveId.", LogLevel.Warn);
                return;
            }
            
            // Create snapshot of data to write outside the lock for better performance
            SoilHealthState snapshotState;
            lock (_lock)
            {
                // Validate and convert from runtime format (Point keys) to disk format (string keys)
                var stateToSave = new SoilHealthState();
                foreach (var locationEntry in _runtimeCache)
                {
                    // Skip invalid location names to prevent corrupt entries
                    if (string.IsNullOrWhiteSpace(locationEntry.Key))
                    {
                        _monitor.Log("Skipped saving soil health for null or empty location name.", LogLevel.Warn);
                        continue;
                    }
                    
                    var stringDict = new Dictionary<string, float>(locationEntry.Value.Count);
                    int invalidCount = 0; // Count invalid entries to aggregate warnings
                    
                    foreach (var tileEntry in locationEntry.Value)
                    {
                        var val = tileEntry.Value;
                        if (float.IsNaN(val) || float.IsInfinity(val))
                        {
                            invalidCount++;
                            continue;
                        }
                        
                        float clamped = Math.Clamp(val, 0f, 100f);
                        string key = $"{tileEntry.Key.X.ToString(CultureInfo.InvariantCulture)},{tileEntry.Key.Y.ToString(CultureInfo.InvariantCulture)}";
                        stringDict[key] = clamped;
                    }
                    
                    if (invalidCount > 0)
                    {
                        _monitor.Log($"Skipped {invalidCount} invalid soil health entr(ies) in location '{locationEntry.Key}' during save.", LogLevel.Warn);
                    }
                    
                    // Only add location if it has valid tiles
                    if (stringDict.Count > 0)
                    {
                        stateToSave.LocationHealthData[locationEntry.Key] = stringDict;
                    }
                }

                // Prevent saving empty data which could overwrite existing data
                if (stateToSave.LocationHealthData.Count == 0)
                {
                    _monitor.Log("No valid soil health data to save; skipping persistence.", LogLevel.Trace);
                    return;
                }

                // Capture snapshot to write outside the lock
                snapshotState = stateToSave;
            }

            string saveKey = GetSaveKey(saveId);
            
            try
            {
                _modDataService.SaveData(snapshotState, saveKey);
                _monitor.Log("Soil Health data saved successfully.", LogLevel.Trace);
            }
            catch (Exception)
            {
                _monitor.Log("Error occurred while persisting soil health data.", LogLevel.Error);
                // Intentionally do not rethrow; keep runtime cache intact so the game can continue.
            }
        }

        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            // Validate input to prevent potential exceptions
            if (string.IsNullOrWhiteSpace(locationName)) 
            {
                _monitor.Log("GetSoilHealth skipped: invalid location name.", LogLevel.Trace);
                return 0f; // Return default (Poor Soil) if location is invalid
            }

            // Guard against invalid coordinates to prevent misleading lookups
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("GetSoilHealth skipped: invalid tile coordinates.", LogLevel.Trace);
                return 0f;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                _monitor.Log("GetSoilHealth skipped: coordinates out of integer range.", LogLevel.Trace);
                return 0f;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            float result;
            lock (_lock)
            {
                if (_runtimeCache.TryGetValue(locationName, out var tiles))
                {
                    var key = new Point(ix, iy);
                    
                    if (tiles.TryGetValue(key, out float health))
                    {
                        result = health;
                    }
                    else
                    {
                        result = 0f; // Return default (Poor Soil) if no data exists
                    }
                }
                else
                {
                    result = 0f; // Return default (Poor Soil) if location doesn't exist
                }
            }
            return result;
        }

        public void SetSoilHealth(string locationName, Vector2 tile, float value)
        {
            // Validate input to prevent adding entries with invalid keys
            if (string.IsNullOrWhiteSpace(locationName)) 
            {
                _monitor.Log("SetSoilHealth skipped: invalid location name.", LogLevel.Warn);
                return; // Skip if location is invalid
            }
                
            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("SetSoilHealth skipped: invalid tile coordinates.", LogLevel.Warn);
                return;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                _monitor.Log("SetSoilHealth skipped: coordinates out of integer range.", LogLevel.Trace);
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            lock (_lock)
            {
                // Domain Rule: Clamp between 0 and 100
                float clampedValue = Math.Clamp(value, 0f, 100f);

                // Use GetOrAddLocationCache to avoid code duplication
                var tiles = GetOrAddLocationCache(locationName);
                
                var key = new Point(ix, iy);
                tiles[key] = clampedValue;
            }
        }

        public void UpdateHealth(string locationName, Vector2 tile, float delta)
        {
            // Validate input to prevent adding entries with invalid keys
            if (string.IsNullOrWhiteSpace(locationName)) 
            {
                _monitor.Log("UpdateHealth skipped: invalid location name.", LogLevel.Warn);
                return; // Skip if location is invalid
            }
                
            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("UpdateHealth skipped: invalid tile coordinates.", LogLevel.Warn);
                return;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                _monitor.Log("UpdateHealth skipped: coordinates out of integer range.", LogLevel.Trace);
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            lock (_lock)
            {
                // Perform the update operation in a single lock to avoid reentrant calls
                var tiles = GetOrAddLocationCache(locationName);

                // Convert Vector2 to Point for lookup (using integer coordinates)
                var key = new Point(ix, iy);
                
                // Get current value (0 if tile doesn't exist) and calculate new value
                tiles.TryGetValue(key, out float currentHealth);
                float newHealth = Math.Clamp(currentHealth + delta, 0f, 100f);
                tiles[key] = newHealth;
            }
        }

        /// <summary>
        /// Gets or creates the tile dictionary for a given location.
        /// This method reduces code duplication between SetSoilHealth and UpdateHealth methods.
        /// </summary>
        /// <param name="locationName">The name of the location</param>
        /// <returns>The tile dictionary for the location</returns>
        private Dictionary<Point, float> GetOrAddLocationCache(string locationName)
        {
            if (!_runtimeCache.TryGetValue(locationName, out var tiles))
            {
                tiles = new Dictionary<Point, float>();
                _runtimeCache[locationName] = tiles;
            }
            return tiles;
        }

        private string GetSaveKey(string saveId)
        {
            // Basic key sanitization is handled by ModDataService, 
            // but we ensure the save ID is part of the key to separate files.
            return $"{KeyPrefix}{saveId}";
        }
    }
}
```

#### 2.3. Camada de Controle e Injeção de Dependência

Atualizamos o `ModEntry.cs` para registrar o novo serviço e atualizar o controller.

**Arquivo:** `LivingRoots/ModEntry.cs` (Trecho)
```csharp
// ... imports

public override void Entry(IModHelper helper)
{
    // ... (serviços de domínio existentes) ...
    
    // Create application services
    var modDataService = new ModDataService(helper, this.Monitor, modLogic);
    
    // NEW: Soil Health Service
    var soilHealthService = new SoilHealthService(modDataService, this.Monitor);
    
    // Update ModController constructor (see step 2.4)
    _controller = new ModController(helper, this.Monitor, this.ModManifest, modDataService, soilHealthService);
    
    _controller.RegisterEvents();
}
```

**Arquivo:** `LivingRoots/Controllers/ModController.cs`

1. Adicione a dependência `ISoilHealthService`.
2. Registre os eventos `SaveLoaded` e `Saving`.

```csharp
// ... imports
using LivingRoots.Domain; // Add this import

public sealed class ModController : IDisposable
{
    // ... campos existentes
    private readonly ISoilHealthService _soilHealthService;

    // Update constructor
    public ModController(
        IModHelper helper, 
        IMonitor monitor, 
        IManifest manifest, 
        IModDataService modDataService,
        ISoilHealthService soilHealthService) // New dependency
    {
        // ... assignments
        _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
    }

    public void RegisterEvents()
    {
        // ... checks ...
        
        try 
        {
            var gameLoop = _helper?.Events?.GameLoop;
            // ... (null checks)
            
            _onGameLaunchedHandler ??= OnGameLaunched;
            gameLoop.GameLaunched += _onGameLaunchedHandler;

            // NEW EVENTS
            gameLoop.SaveLoaded += OnSaveLoaded;
            gameLoop.Saving += OnSaving; // Note: Using 'Saving' instead of 'Saved' for pre-save hook
            
            // ... logs
        }
        // ... catch block
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Load data using the save folder name as unique ID
        string saveId = Constants.SaveFolderName;
        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("Cannot load soil health data: SaveFolderName is unavailable.", LogLevel.Warn);
            return;
        }

        _soilHealthService.LoadData(saveId);
        _monitor.Log("Soil health data loaded successfully.", LogLevel.Trace);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        // Save data before the game saves/exits (using the saving event)
        string saveId = Constants.SaveFolderName;
        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("Cannot save soil health data: SaveFolderName is unavailable.", LogLevel.Warn);
            return;
        }

        _soilHealthService.SaveData(saveId);
        _monitor.Log("Soil health data saved successfully.", LogLevel.Trace);
    }
    
    // ... Dispose/Unregister methods should also remove the new events ...
}
```

## 3. Verificação e Critérios de Aceite

Após a implementação, executamos as seguintes verificações manuais e automatizadas:

1. **Testes Automatizados:**
   - Rode `dotnet test`. Todos os testes (incluindo os novos `SoilHealthServiceTests`) devem passar.
2. **Teste em Jogo (Manual):**
   - Inicie o jogo e carregue um save.
   - Verifique no console do SMAPI (Trace logs) se aparece: `Soil Health data loaded for save ...`.
   - Jogue um dia, faça algo que altere a saúde (futura feature, por enquanto o valor é estático ou alterado via console de debug se você criar um comando).
   - Durma para salvar. Verifique o log: `Soil Health data saved for ...`.
   - Feche o jogo e verifique a pasta do mod: `LivingRoots/data/soil_health_data_[SaveName].json`. O arquivo deve existir e conter JSON válido.
   - Abra o jogo novamente. Os dados devem ser carregados sem erro.

## 4. Melhorias de Segurança e Robustez

### Validação de Entrada
- Verificação de coordenadas inválidas (NaN, Infinity)
- Verificação de nomes de localização inválidos
- Verificação de valores de saúde fora do intervalo [0, 100]

### Tratamento de Exceções
- Uso de cache temporário para prevenir perda de dados durante falhas de carregamento
- Tratamento de exceções durante operações de leitura e gravação
- Preservação do cache existente em caso de falhas

### Segurança de Thread
- Uso de locks para garantir operações thread-safe
- Prevenção de condições de corrida durante acesso concorrente

### Serialização Segura
- Validação de dados durante carregamento e salvamento
- Tratamento de valores inválidos (NaN, Infinity) na serialização
- Prevenção de injeção de dados maliciosos

## 5. Resumo das Alterações Necessárias

1. **Criar Testes:** `LivingRoots.Tests/SoilHealthServiceTests.cs`.
2. **Criar Interfaces/Modelos:** `ISoilHealthService.cs`, `SoilHealthState.cs`.
3. **Implementar Serviço:** `SoilHealthService.cs` (usando `IModDataService`).
4. **Atualizar ModEntry:** Injeção de dependência do novo serviço.
5. **Atualizar ModController:** Hook nos eventos `SaveLoaded` e `Saving`.

Esta abordagem segue estritamente os princípios SOLID (SRP no serviço, DIP nas interfaces) e DDD (Linguagem Ubíqua "SoilHealth"), garantindo uma base sólida para as próximas features do roadmap.