using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LivingRoots.Controllers;

public class CompostingBinController(
    IModHelper helper,
    IMonitor monitor,
    ICompostingBinService compostingBinService) : IDisposable
{
    private readonly IModHelper _helper = helper ?? throw new ArgumentNullException(nameof(helper));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly ICompostingBinService _compostingBinService = compostingBinService ?? throw new ArgumentNullException(nameof(compostingBinService));

    private bool _disposed = false;

    public void RegisterEvents()
    {
        _helper.Events.Input.ButtonPressed += OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (_disposed) return;
        if (e.Button != SButton.MouseRight) return;

        var cursorPos = _helper.Input.GetCursorPosition();
        var tile = cursorPos.GrabTile;
        var location = Game1.currentLocation;

        if (location == null) return;

        var state = _compostingBinService.GetBinState(location.Name, tile);
        var heldItem = Game1.player.CurrentItem;

        if (heldItem != null)
        {
            if (state == CompostingBinState.Empty)
            {
                _compostingBinService.AddWaste(location.Name, tile, heldItem);
            }
        }
        else if (state == CompostingBinState.Ready)
        {
            _compostingBinService.CollectCompost(location.Name, tile, Game1.player);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _helper.Events.Input.ButtonPressed -= OnButtonPressed;
        _disposed = true;
    }
}
