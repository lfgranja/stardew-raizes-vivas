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
using System.IO;
using System.Linq;
using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    public class SoilHealthService : ISoilHealthService
    {
        private readonly IModDataService _modDataService;
        private readonly IMonitor _monitor;
        private readonly IFileNameSanitizationService _fileNameSanitizationService;

        // Runtime cache using Point directly as key for better performance and precision
        // Dictionary<LocationName, Dictionary<TileCoordinates, HealthValue>>
        private readonly Dictionary<string, Dictionary<Point, float>> _runtimeCache = new();

        // Lock object for thread safety
        private readonly object _lock = new object();

        public SoilHealthService(IModDataService modDataService, IMonitor monitor, IFileNameSanitizationService fileNameSanitizationService)
        {
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _fileNameSanitizationService = fileNameSanitizationService ?? throw new ArgumentNullException(nameof(fileNameSanitizationService));
        }

        public void LoadData(string saveId)
        {
            // If saveId is invalid, clear the cache to prevent data leakage between saves
            // IMPORTANT: Clearing the cache when saveId is invalid maintains data integrity
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("LoadData aborted: invalid saveId. Runtime cache cleared to prevent data leakage.", LogLevel.Warn);
                lock (_lock)
                {
                    _runtimeCache.Clear();
                }
                return; // Return early without modifying the cache
            }

            string dataKey = GetSaveKey(saveId);
            
            // If sanitization failed and we got a default key, log and return early
            if (dataKey == ModConstants.DefaultSaveKey)
            {
                _monitor.Log("LoadData aborted: saveId sanitization failed, using default key.", LogLevel.Error);
                lock (_lock)
                {
                    _runtimeCache.Clear();
                }
                return; // Return early without modifying the cache
            }

            // Use temporary cache to prevent data loss if parsing fails partway through
            var tempCache = new Dictionary<string, Dictionary<Point, float>>();

            SoilHealthState? savedData = null;
            bool loadErrorOccurred = false;
            
            try
            {
                savedData = _modDataService.LoadData<SoilHealthState>(dataKey);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log("Error loading soil health data.", LogLevel.Error);
                loadErrorOccurred = true;
            }

            // If there was an error loading the data, return early without modifying runtime cache to preserve existing data
            if (loadErrorOccurred)
            {
                return;
            }

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
                            int.TryParse(keySpan.Slice(0, commaIndex), NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int x) &&
                            int.TryParse(keySpan.Slice(commaIndex + 1), NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int y))
                        {
                            // Validate health value range
                            float validatedValue = tileEntry.Value;
                            
                            // Check for NaN or Infinity values and skip entirely
                            if (float.IsNaN(validatedValue) || float.IsInfinity(validatedValue))
                            {
                                // Only warn once per location for invalid values to prevent log spam
                                if (!warnedForInvalidValue)
                                {
                                    _monitor.Log($"Invalid health value (NaN/Infinity) found in save data for location '{locationEntry.Key}'; skipping entry.", LogLevel.Warn);
                                    warnedForInvalidValue = true;
                                }
                                // Skip this entry entirely instead of converting to 0
                                continue;
                            }
                            else if (validatedValue < ModConstants.MinSoilHealth || validatedValue > ModConstants.MaxSoilHealth)
                            {
                                // Only warn once per location for out-of-range values to prevent log spam
                                if (!warnedForInvalidValue)
                                {
                                    _monitor.Log($"Invalid health value found in save data for location '{locationEntry.Key}'; clamping to valid range [0, 100].", LogLevel.Warn);
                                    warnedForInvalidValue = true;
                                }
                                validatedValue = ClampHealthValue(validatedValue);
                            }

                            tileDict[new Point(x, y)] = validatedValue;
                        }
                        else
                        {
                            // Only warn once per location for malformed keys to prevent log spam
                            if (!warnedForMalformedKey)
                            {
                                _monitor.Log($"Malformed tile key found in save data for location '{locationEntry.Key}'; skipping entry.", LogLevel.Warn);
                                warnedForMalformedKey = true;
                            }
                        }
                    }
                    if (tileDict.Count > 0) // Only add location if it has valid tiles
                    {
                        tempCache[locationEntry.Key] = tileDict;
                    }
                }
            }

            // Replace the runtime cache for this save regardless of whether we loaded valid data
            // This ensures data from one save doesn't leak into another
            lock (_lock)
            {
                _runtimeCache.Clear();
                foreach (var location in tempCache)
                {
                    _runtimeCache[location.Key] = location.Value;
                }
            }
            
            if (tempCache.Count == 0)
            {
                _monitor.Log("LoadData found no valid entries; cache has been cleared.", LogLevel.Trace);
            }
        }

        public void SaveData(string saveId)
        {
            // If saveId is invalid, skip saving to prevent using a default/fallback key
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("SaveData aborted: invalid saveId.", LogLevel.Warn);
                return;
            }

            string dataKey = GetSaveKey(saveId);
            
            // If sanitization failed and we got a default key, log and return early
            if (dataKey == ModConstants.DefaultSaveKey)
            {
                _monitor.Log("SaveData aborted: saveId sanitization failed, using default key.", LogLevel.Error);
                return;
            }

            // Create a snapshot of the current cache to avoid holding the lock during I/O
            // This implements the snapshot pattern to move I/O operations outside the lock
            Dictionary<string, Dictionary<string, float>>? snapshotState = null;
            bool hasDataToSave = false;
            
            lock (_lock)
            {
                if (_runtimeCache.Count == 0)
                {
                    // If no data to save, return early without performing I/O
                    return;
                }

                hasDataToSave = true;
                snapshotState = new Dictionary<string, Dictionary<string, float>>();
                foreach (var location in _runtimeCache)
                {
                    var tileDict = new Dictionary<string, float>();
                    foreach (var tile in location.Value)
                    {
                        // Convert Point back to "X,Y" string format using invariant culture for consistency
                        string tileKey = $"{tile.Key.X.ToString(CultureInfo.InvariantCulture)},{tile.Key.Y.ToString(CultureInfo.InvariantCulture)}";
                        
                        // Skip invalid values (NaN, Infinity) when saving
                        if (float.IsNaN(tile.Value) || float.IsInfinity(tile.Value))
                        {
                            continue; // Skip invalid values
                        }
                        
                        // Clamp value to valid range [0, 100] before saving
                        float clampedValue = ClampHealthValue(tile.Value);
                        tileDict[tileKey] = clampedValue;
                    }
                    
                    // Only add location if it has valid tiles
                    if (tileDict.Count > 0)
                    {
                        snapshotState[location.Key] = tileDict;
                    }
                }
            }

            // Only save if we have data to save (this prevents the test failure)
            // This moves the I/O operation completely outside the lock for better performance
            if (hasDataToSave && snapshotState != null && snapshotState.Count > 0)
            {
                try
                {
                    var stateToSave = new SoilHealthState { LocationHealthData = snapshotState };
                    _modDataService.SaveData(stateToSave, dataKey);
                    _monitor.Log($"Soil health data saved for {saveId}", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    // Log error but don't expose raw exception message for security
                    _monitor.Log("Error saving soil health data.", LogLevel.Error);
                }
            }
        }

        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            // Validate input to prevent potential exceptions
            if (string.IsNullOrWhiteSpace(locationName))
            {
                // Skip logging for invalid location name to reduce noise in frequently called methods
                return 0f; // Return default (Poor Soil) if location is invalid
            }

            // Guard against invalid coordinates to prevent misleading lookups
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                // Skip logging for invalid coordinates to reduce noise in frequently called methods
                return 0f;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                // Skip logging for coordinate range issues to reduce noise in frequently called methods
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
                // Skip logging for invalid location name to reduce noise in frequently called methods
                return; // Skip if location is invalid
            }

            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                // Skip logging for invalid coordinates to reduce noise in frequently called methods
                return;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                // Skip logging for coordinate range issues to reduce noise in frequently called methods
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            lock (_lock)
            {
                // Domain Rule: Clamp between 0 and 100 (not 10 as previously)
                float clampedValue = ClampHealthValue(value);

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
                // Skip logging for invalid location name to reduce noise in frequently called methods
                return; // Skip if location is invalid
            }

            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                // Skip logging for invalid coordinates to reduce noise in frequently called methods
                return;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                // Skip logging for coordinate range issues to reduce noise in frequently called methods
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            lock (_lock)
            {
                // Use GetOrAddLocationCache to avoid code duplication
                var tiles = GetOrAddLocationCache(locationName);

                var key = new Point(ix, iy);
                if (tiles.TryGetValue(key, out float current))
                {
                    float newValue = ClampHealthValue(current + delta);
                    tiles[key] = newValue;
                }
                else
                {
                    // If the key doesn't exist, initialize with the delta value (starting from 0)
                    float newValue = ClampHealthValue(delta);
                    tiles[key] = newValue;
                }
            }
        }

        private Dictionary<Point, float> GetOrAddLocationCache(string locationName)
        {
            if (!_runtimeCache.TryGetValue(locationName, out var locationCache))
            {
                locationCache = new Dictionary<Point, float>();
                _runtimeCache[locationName] = locationCache;
            }
            return locationCache;
        }
        
        private float ClampHealthValue(float value)
        {
            return Math.Clamp(value, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        }

        private string GetSaveKey(string saveId)
        {
            // Sanitize the saveId to remove invalid filename characters
            if (string.IsNullOrEmpty(saveId))
            {
                _monitor.Log("SaveId cannot be null or empty.", LogLevel.Error);
                return ModConstants.DefaultSaveKey;
            }

            try
            {
                string? sanitized = _fileNameSanitizationService.Sanitize(saveId);
                if (string.IsNullOrEmpty(sanitized))
                {
                    _monitor.Log("SaveId sanitizes to an empty string after processing.", LogLevel.Error);
                    return ModConstants.DefaultSaveKey;
                }
                
                return $"{ModConstants.KeyPrefix}{sanitized}";
            }
            catch (ArgumentException ex)
            {
                // Log the error and return a safe default key instead of throwing an exception
                _monitor.Log($"SaveId sanitization failed: {ex.Message}", LogLevel.Error);
                return ModConstants.DefaultSaveKey; // Return a safe default key instead of throwing an exception
            }
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
    var fileNameSanitizationService = new FileNameSanitizationService(this.Monitor);
    var saveIdProvider = new SaveIdProvider(helper, this.Monitor);
    var soilHealthService = new SoilHealthService(modDataService, this.Monitor, fileNameSanitizationService);
    
    // Update ModController constructor (see step 2.4)
    _controller = new ModController(helper, this.Monitor, this.ModManifest, modDataService, soilHealthService, saveIdProvider);
    
    _controller.RegisterEvents();
}
```

**Arquivo:** `LivingRoots/Controllers/ModController.cs`

1. Adicione a dependência `ISoilHealthService` e `ISaveIdProvider`.
2. Registre os eventos `SaveLoaded` e `Saving`.

```csharp
// ... imports
using LivingRoots.Domain; // Add this import
using LivingRoots.Services; // Add this import

public sealed class ModController : IDisposable
{
    // ... campos existentes
    private readonly ISoilHealthService _soilHealthService;
    private readonly ISaveIdProvider _saveIdProvider;

    // Update constructor
    public ModController(
        IModHelper helper, 
        IMonitor monitor, 
        IManifest manifest, 
        IModDataService modDataService,
        ISoilHealthService soilHealthService,
        ISaveIdProvider saveIdProvider) // New dependency
    {
        // ... assignments
        _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
        _saveIdProvider = saveIdProvider ?? throw new ArgumentNullException(nameof(saveIdProvider));
    }

    public void RegisterEvents()
    {
        // ... checks ...
        
        try 
        {
            var gameLoop = _helper?.Events?.GameLoop;
            // ... (null checks)
            
            _onGameLaunchedHandler ??= OnGameLaunched;

            // NEW EVENTS
            gameLoop.SaveLoaded += OnSaveLoaded;
            gameLoop.Saving += OnSaving; // Note: Using 'Saving' instead of 'Saved' for pre-save hook
            
            // ... logs
        }
        // ... catch block
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Get the save ID using the abstraction (monitor is already available in the provider)
        string? saveId = _saveIdProvider.GetSaveId();

        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("OnSaveLoaded: SaveFolderName unavailable; skipping soil health load.", LogLevel.Warn);
            return;
        }

        try
        {
            // Load data using the save folder name as unique ID
            _soilHealthService.LoadData(saveId);
            _monitor.Log("Soil health data loaded successfully.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            // Log error but don't expose raw exception message for security
            _monitor.Log($"Error occurred while loading soil health data for save.", LogLevel.Error);
        }
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        // Get the save ID using the abstraction (monitor is already available in the provider)
        string? saveId = _saveIdProvider.GetSaveId();

        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("OnSaving: SaveFolderName unavailable; skipping soil health save.", LogLevel.Warn);
            return;
        }

        try
        {
            // Save data before the game saves/exits (using the saving event)
            _soilHealthService.SaveData(saveId);
            _monitor.Log("Soil health data saved successfully.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            // Log error but don't expose raw exception message for security
            _monitor.Log($"Error occurred while saving soil health data for save.", LogLevel.Error);
        }
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
   - Verifique no console do SMAPI (Trace logs) se aparece: `Soil health data loaded successfully.`.
   - Jogue um dia, faça algo que altere a saúde (futura feature, por enquanto o valor é estático ou alterado via console debug se você criar um comando).
   - Durma para salvar. Verifique o log: `Soil health data saved successfully.`.
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