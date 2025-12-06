using System;
using System.Collections.Generic;
using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    public class SoilHealthService : ISoilHealthService
    {
        private readonly IModDataService _modDataService;
        private readonly IMonitor _monitor;
        
        // Cache em memória para acesso rápido durante o jogo
        private SoilHealthState _currentState = new SoilHealthState();
        private const string KeyPrefix = "soil_health_data_";

        public SoilHealthService(IModDataService modDataService, IMonitor monitor)
        {
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        public void LoadData(string saveId)
        {
            string key = GetSaveKey(saveId);
            var data = _modDataService.LoadData<SoilHealthState>(key);
            
            if (data != null)
            {
                _currentState = data;
                _monitor.Log($"Soil Health data loaded for save {saveId}", LogLevel.Trace);
            }
            else
            {
                _currentState = new SoilHealthState();
                _monitor.Log($"No existing Soil Health data found for save {saveId}. Starting fresh.", LogLevel.Info);
            }
        }

        public void SaveData(string saveId)
        {
            string key = GetSaveKey(saveId);
            _modDataService.SaveData(_currentState, key);
            _monitor.Log($"Soil Health data saved for {saveId}", LogLevel.Trace);
        }

        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            if (_currentState.LocationHealthData.TryGetValue(locationName, out var tiles))
            {
                string tileKey = $"{tile.X},{tile.Y}";
                if (tiles.TryGetValue(tileKey, out float health))
                {
                    return health;
                }
            }
            return 0f; // Valor padrão (Solo Pobre) se não houver dados
        }

        public void SetSoilHealth(string locationName, Vector2 tile, float value)
        {
            // Regra de Domínio: Clamp entre 0 e 100
            float clampedValue = Math.Clamp(value, 0f, 100f);

            if (!_currentState.LocationHealthData.ContainsKey(locationName))
            {
                _currentState.LocationHealthData[locationName] = new Dictionary<string, float>();
            }

            string tileKey = $"{tile.X},{tile.Y}";
            _currentState.LocationHealthData[locationName][tileKey] = clampedValue;
        }

        public void UpdateHealth(string locationName, Vector2 tile, float delta)
        {
            float current = GetSoilHealth(locationName, tile);
            SetSoilHealth(locationName, tile, current + delta);
        }

        private string GetSaveKey(string saveId)
        {
            // Sanitização básica da chave é feita pelo ModDataService, 
            // mas garantimos que o ID do save faça parte da chave para separar arquivos.
            return $"{KeyPrefix}{saveId}";
        }
    }
}