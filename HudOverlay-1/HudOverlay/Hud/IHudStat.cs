namespace HudOverlay.Hud;

/// <summary>
/// One line on the HUD. To add a new stat: create a class that implements
/// this interface, then add one line registering it in HudController's
/// stat list. Nothing else needs to change.
/// </summary>
public interface IHudStat
{
    /// <summary>Short label shown before the value, e.g. "FPS".</summary>
    string Label { get; }

    /// <summary>Called every frame the HUD is visible. Return the current display value as text.</summary>
    string GetValue();
}
