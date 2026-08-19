using UnityEngine;

namespace HudOverlay.Hud.Stats;

/// <summary>
/// Shows the local player's cosmetic color as a hex code.
/// NOTE: "VRRig.LocalRig.playerColor" is the commonly used field for this,
/// but exact member names can shift between game updates. If this doesn't
/// compile against your current Assembly-CSharp.dll, open it in a decompiler
/// (e.g. dnSpy/ILSpy) and search VRRig for the local color field, then
/// update the line below — nothing else in the project needs to change.
/// </summary>
public class ColorStat : IHudStat
{
    public string Label => "Color";

    public string GetValue()
    {
        var rig = VRRig.LocalRig;
        if (rig == null)
            return "N/A";

        Color c = rig.playerColor;
        return $"#{ColorUtility.ToHtmlStringRGB(c)}";
    }
}
