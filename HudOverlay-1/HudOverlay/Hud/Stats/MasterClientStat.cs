using Photon.Pun;

namespace HudOverlay.Hud.Stats;

/// <summary>
/// Shows who the current room's Master Client is (the "host" from PUN's
/// perspective — the client authoritative for room logic).
/// </summary>
public class MasterClientStat : IHudStat
{
    public string Label => "Host";

    public string GetValue()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.MasterClient == null)
            return "N/A";

        var master = PhotonNetwork.MasterClient;
        string name = string.IsNullOrEmpty(master.NickName) ? $"Actor {master.ActorNumber}" : master.NickName;
        return PhotonNetwork.IsMasterClient ? $"{name} (you)" : name;
    }
}
