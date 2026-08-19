using Photon.Pun;

namespace HudOverlay.Hud.Stats;

public class PingStat : IHudStat
{
    public string Label => "Ping";

    public string GetValue()
    {
        if (!PhotonNetwork.IsConnected)
            return "N/A";

        return $"{PhotonNetwork.GetPing()}ms";
    }
}
