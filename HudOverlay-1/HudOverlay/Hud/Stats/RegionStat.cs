using Photon.Pun;

namespace HudOverlay.Hud.Stats;

public class RegionStat : IHudStat
{
    public string Label => "Region";

    public string GetValue()
    {
        return string.IsNullOrEmpty(PhotonNetwork.CloudRegion) ? "N/A" : PhotonNetwork.CloudRegion;
    }
}
