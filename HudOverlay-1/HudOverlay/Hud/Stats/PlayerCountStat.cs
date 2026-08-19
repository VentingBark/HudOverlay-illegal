using Photon.Pun;

namespace HudOverlay.Hud.Stats;

public class PlayerCountStat : IHudStat
{
    public string Label => "Players";

    public string GetValue()
    {
        if (!PhotonNetwork.InRoom)
            return "N/A";

        var room = PhotonNetwork.CurrentRoom;
        return $"{room.PlayerCount}/{room.MaxPlayers}";
    }
}
