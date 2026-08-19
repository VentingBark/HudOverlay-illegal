using Photon.Pun;

namespace HudOverlay.Hud.Stats;

public class NameStat : IHudStat
{
    public string Label => "Name";

    public string GetValue()
    {
        return string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Unknown" : PhotonNetwork.NickName;
    }
}
