using Photon.Pun;

namespace HudOverlay.Hud.Stats;

public class RoomNameStat : IHudStat
{
    public string Label => "Room";

    public string GetValue()
    {
        return PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "Not in room";
    }
}
