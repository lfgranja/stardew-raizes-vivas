using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace RaizesVivas
{
    // Esta é a classe principal do seu Mod. SMAPI vai carregá-la.
    public class ModEntry : Mod
    {
        // O "helper" é nossa porta de entrada para todas as APIs do SMAPI.
        public override void Entry(IModHelper helper)
        {
            // 1. "Ouvir" um evento do jogo (quando o jogo termina de carregar)
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        // 2. Método que é chamado quando o evento acontece
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // 3. Registrar uma mensagem no console do SMAPI
            this.Monitor.Log("O Mod 'Raízes Vivas' foi carregado com sucesso!", LogLevel.Info);

            // 4. Registrar um comando no console (ex: 'rv_versao')
            helper.ConsoleCommands.Add("rv_versao", "Mostra a versão do Raízes Vivas.", this.PrintVersion);
        }

        private void PrintVersion(string command, string[] args)
        {
            this.Monitor.Log("Versão 0.0.1 (Sprint 0)", LogLevel.Info);
        }
    }
}