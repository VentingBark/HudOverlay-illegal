using BepInEx;
using HudOverlay.Hud;
using UnityEngine;

namespace HudOverlay;

[BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        var host = new GameObject("HudOverlay");
        host.AddComponent<HudController>();
        DontDestroyOnLoad(host);

        Logger.LogInfo($"{Constants.Name} v{Constants.Version} loaded.");
    }
}
