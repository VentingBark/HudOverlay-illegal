using System.Linq;
using Photon.Pun;

namespace HudOverlay.Hud.Stats;

/// <summary>
/// Multi-line stat: one line per player currently in the room. HudController
/// splits on '\n' and indents each line automatically, so this is the only
/// place player-list formatting needs to live.
/// </summary>
public class PlayerListStat : IHudStat
{
    public string Label => "In room";

    public string GetValue()
    {
        if (!PhotonNetwork.InRoom)
            return "N/A";

        var players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0)
            return "(none)";

        return string.Join("\n", players.Select(FormatPlayer));
    }

    private static string FormatPlayer(Photon.Realtime.Player p)
    {
        string name = string.IsNullOrEmpty(p.NickName) ? $"Actor {p.ActorNumber}" : p.NickName;
        string tag = p.IsMasterClient ? " [host]" : "";
        string you = p.IsLocal ? " (you)" : "";
        return $"{name}{tag}{you}";
    }
}
