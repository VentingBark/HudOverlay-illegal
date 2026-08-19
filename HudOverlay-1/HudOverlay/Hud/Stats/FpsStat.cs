using UnityEngine;

namespace HudOverlay.Hud.Stats;

public class FpsStat : IHudStat
{
    public string Label => "FPS";

    private float _smoothedFps;
    private const float SmoothingSpeed = 10f;

    public string GetValue()
    {
        float current = 1f / Time.unscaledDeltaTime;
        _smoothedFps = Mathf.Lerp(_smoothedFps, current, Time.unscaledDeltaTime * SmoothingSpeed);
        return Mathf.RoundToInt(_smoothedFps).ToString();
    }
}
