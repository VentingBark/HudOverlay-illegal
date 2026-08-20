using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HudOverlay.Hud.Stats;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.InputSystem;
using HudOverlay.Hud;

namespace HudOverlay.Hud;

public class HudController : MonoBehaviour
{
    private readonly List<HudPage> _pages = new()
    {
        new HudPage("Self", new List<IHudStat>
        {
            new FpsStat(),
            new PingStat(),
            new NameStat(),
            new ColorStat(),
        }),
        new HudPage("Server", new List<IHudStat>
        {
            new RoomNameStat(),
            new PlayerCountStat(),
            new RegionStat(),
            new MasterClientStat(),
        }),
    };

    private const Key ToggleKey = Key.Numpad1;
    private const Key NextPageKey = Key.Tab;
    private const Key BackKey = Key.Backspace;

    private int _pageIndex;
    private bool _visible = true;
    private Player _selectedPlayer;

    // New: controls the dedicated "all properties" scrollable view
    private bool _showAllProps;
    private Vector2 _propsScroll;
    private const int PropsBoxWidth = 320;
    private const int PropsBoxHeight = 260;

    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _buttonStyle;

    private readonly Vector2 _screenOffset = new(15f, 15f);
    private const int Width = 240;
    private const int LineHeight = 22;
    private const int HeaderHeight = 26;
    private const int Padding = 10;

    private static readonly Dictionary<string, string> _creationDateCache = new();
    private static readonly Dictionary<string, float> _waiting = new();


    private static string GetCreationDate(Player player)
    {
        if (player == null || string.IsNullOrEmpty(player.UserId))
            return "N/A";

        string userId = player.UserId;

        if (_creationDateCache.TryGetValue(userId, out string cached))
            return cached;

        if (_waiting.TryGetValue(userId, out float nextTry) && Time.time < nextTry)
            return "Loading...";

        _waiting[userId] = Time.time + 8f;
        _creationDateCache[userId] = "Loading...";

        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest
        {
            PlayFabId = userId
        },
        result =>
        {
            if (result?.AccountInfo?.Created != null)
            {
                _creationDateCache[userId] = result.AccountInfo.Created.ToString("yyyy-MM-dd");
            }
            else
            {
                _creationDateCache[userId] = "Unknown";
            }
        },
        error =>
        {
            _creationDateCache[userId] = "Error";
            Debug.LogWarning($"[HudOverlay] Failed to get creation date for {player.NickName}: {error.GenerateErrorReport()}");
        });

        return "Loading...";
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current[ToggleKey].wasPressedThisFrame)
        {
            _visible = !_visible;
        }

        if (Keyboard.current[NextPageKey].wasPressedThisFrame)
        {
            _selectedPlayer = null;
            _showAllProps = false;
            _pageIndex = (_pageIndex + 1) % _pages.Count;
        }

        if (_selectedPlayer != null && Keyboard.current[BackKey].wasPressedThisFrame)
        {
            if (_showAllProps)
                _showAllProps = false;
            else
                _selectedPlayer = null;
        }
    }

    private void OnGUI()
    {
        if (!_visible || _pages.Count == 0) return;

        EnsureStylesLoaded();

        if (_selectedPlayer != null)
        {
            if (_showAllProps)
                DrawAllProps(_selectedPlayer);
            else
                DrawPlayerDetail(_selectedPlayer);
            return;
        }

        HudPage page = _pages[_pageIndex];
        bool isServerPage = page.Name == "Server";

        List<string> lines = BuildLines(page);
        Player[] players = isServerPage && PhotonNetwork.InRoom ? PhotonNetwork.PlayerList : System.Array.Empty<Player>();

        int height = HeaderHeight + Padding * 2 + LineHeight * lines.Count
                     + (isServerPage ? LineHeight * (players.Length + 1) : 0);
        var box = new Rect(_screenOffset.x, _screenOffset.y, Width, height);

        GUI.Box(box, GUIContent.none, _boxStyle);

        var headerRect = new Rect(box.x + Padding, box.y + Padding - 2, Width - Padding * 2, HeaderHeight);
        string pageIndicator = _pages.Count > 1 ? $"{page.Name} ({_pageIndex + 1}/{_pages.Count})" : page.Name;
        GUI.Label(headerRect, pageIndicator, _headerStyle);

        int y = 0;
        for (; y < lines.Count; y++)
        {
            var lineRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight);
            GUI.Label(lineRect, lines[y], _labelStyle);
        }

        if (isServerPage)
        {
            var playersHeaderRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight);
            GUI.Label(playersHeaderRect, "Players (click for info):", _labelStyle);
            y++;

            foreach (Player p in players)
            {
                var buttonRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight - 2);
                string rawName = string.IsNullOrEmpty(p.NickName) ? $"Actor {p.ActorNumber}" : p.NickName;
                string label = ColorizeName(rawName, RigUtilities.GetPlatform(p));
                if (p.IsMasterClient) label += " [host]";
                if (p.IsLocal) label += " (you)";

                if (GUI.Button(buttonRect, label, _buttonStyle))
                {
                    _selectedPlayer = p;
                    _showAllProps = false;
                }

                y++;
            }
        }
    }

    private void DrawPlayerDetail(Player player)
    {
        string platform = RigUtilities.GetPlatform(player);
        string displayName = string.IsNullOrEmpty(player.NickName) ? "Unknown" : player.NickName;

        var lines = new List<string>
        {
            $"Name: {ColorizeName(displayName, platform)}",
            $"Actor #: {player.ActorNumber}",
            $"Creation Date: {GetCreationDate(player)}",
            $"Time in Room: {RigUtilities.GetTimeInRoom(player)}",
            $"Platform: {platform}",
            $"Suspicious: {(RigUtilities.HasSuspiciousProps(player) ? "Yes" : "No")}",
            $"Creation Date: {RigUtilities.GetCreationDate(player.UserId)}",
            $"UserId: {(string.IsNullOrEmpty(player.UserId) ? "N/A" : player.UserId)}",
            $"Host: {(player.IsMasterClient ? "Yes" : "No")}",
            $"You: {(player.IsLocal ? "Yes" : "No")}",
            $"Active: {(!player.IsInactive ? "Yes" : "No")}",
        };

        string props = RigUtilities.GetSuspiciousProps(player);

        if (player.CustomProperties != null && player.CustomProperties.Count > 0)
        {
            lines.Add("Custom Properties:");
            foreach (System.Collections.DictionaryEntry entry in player.CustomProperties)
            {
                lines.Add($"  {entry.Key}: {entry.Value}");
            }
        }

        // Reserve THREE extra line-heights: "Get Props", "Show All Props", and "Back".
        int height = HeaderHeight + Padding * 2 + LineHeight * lines.Count + LineHeight * 3;
        var box = new Rect(_screenOffset.x, _screenOffset.y, Width, height);

        GUI.Box(box, GUIContent.none, _boxStyle);

        var headerRect = new Rect(box.x + Padding, box.y + Padding - 2, Width - Padding * 2, HeaderHeight);
        GUI.Label(headerRect, "Player Info", _headerStyle);

        int y = 0;
        for (; y < lines.Count; y++)
        {
            var lineRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight);
            GUI.Label(lineRect, lines[y], _labelStyle);
        }

        var TPRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight - 2);
        if (GUI.Button(TPRect, "Teleport To Player", _buttonStyle))
        {
            VRRig rig = RigUtilities.GetVRRigFromPhotonPlayer(player);
            Vector3 vector3Tp = TPlayer.GetVector3(rig);
            TPlayer.TeleportPlayer(vector3Tp);
        }

        var getPropsRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight - 2);
        if (GUI.Button(getPropsRect, "Get Props", _buttonStyle))
        {
            if (!string.IsNullOrEmpty(props))
            {
                HudOverlay.Hud.Stats.NotifiLib.SendNotification($"<color=red>[ALERT]</color> {player.NickName}: {props}");
            }
        }
        y++;
        // 
        var allPropsRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight - 2);
        if (GUI.Button(allPropsRect, $"Show All Props ({player.CustomProperties?.Count ?? 0})", _buttonStyle))
        {
            _showAllProps = true;
        }
        y++; // advance past the Show All Props row so Back doesn't overlap it
        var sayHiRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight - 2);
        if (GUI.Button(sayHiRect, "Say hi", _buttonStyle))
        {
            NetPlayer netPlayer = NetworkSystem.Instance.GetPlayer(player.ActorNumber);
            VRRig rig = netPlayer != null ? RigUtilities.GetVRRigFromPlayer(netPlayer) : null;

            if (rig != null)
            {
                TPlayer.TagPlayer(rig);
                Debug.Log($"[HudOverlay] Say hi -> TPlayer called for {(string.IsNullOrEmpty(player.NickName) ? "Unknown" : player.NickName)}");
            }
            else
            {
                Debug.LogWarning($"[HudOverlay] Say hi: could not resolve VRRig for {(string.IsNullOrEmpty(player.NickName) ? "Unknown" : player.NickName)}");
            }
        }
        y++;




        var backRect = new Rect(box.x + Padding, box.y + Padding + HeaderHeight + y * LineHeight, Width - Padding * 2, LineHeight - 3);
        if (GUI.Button(backRect, "< Back (Backspace)", _buttonStyle))
        {
            _selectedPlayer = null;
        }
    }

    private void DrawAllProps(Player player)
    {
        var box = new Rect(_screenOffset.x, _screenOffset.y, PropsBoxWidth, PropsBoxHeight);
        GUI.Box(box, GUIContent.none, _boxStyle);

        string displayName = string.IsNullOrEmpty(player.NickName) ? "Unknown" : player.NickName;
        string coloredName = ColorizeName(displayName, RigUtilities.GetPlatform(player));

        var headerRect = new Rect(box.x + Padding, box.y + Padding - 2, PropsBoxWidth - Padding * 2, HeaderHeight);
        GUI.Label(headerRect, $"All Properties: {coloredName}", _headerStyle);

        var scrollAreaRect = new Rect(
            box.x + Padding,
            box.y + Padding + HeaderHeight,
            PropsBoxWidth - Padding * 2,
            PropsBoxHeight - HeaderHeight - LineHeight - Padding * 3);

        // Build the entry list defensively. A LINQ Cast<> failure or unexpected
        // value type here used to abort the rest of OnGUI silently, leaving the
        // list area blank. Catch it and surface the error instead.
        var entries = new List<System.Collections.DictionaryEntry>();
        string errorMsg = null;

        try
        {
            if (player.CustomProperties != null)
            {
                foreach (System.Collections.DictionaryEntry entry in player.CustomProperties)
                {
                    entries.Add(entry);
                }
                entries.Sort((a, b) => string.Compare(
                    a.Key?.ToString() ?? "",
                    b.Key?.ToString() ?? "",
                    StringComparison.Ordinal));
            }
        }
        catch (Exception ex)
        {
            errorMsg = ex.Message;
            Debug.LogError($"[HudOverlay] Failed to read CustomProperties for {player.NickName}: {ex}");
        }

        if (errorMsg != null)
        {
            GUI.Label(scrollAreaRect, $"Error reading props:\n{errorMsg}", _labelStyle);
        }
        else if (entries.Count == 0)
        {
            GUI.Label(scrollAreaRect, "No custom properties.", _labelStyle);
        }
        else
        {
            float contentHeight = Mathf.Max(entries.Count * LineHeight, scrollAreaRect.height);
            var viewRect = new Rect(0, 0, scrollAreaRect.width - 20, contentHeight);

            _propsScroll = GUI.BeginScrollView(scrollAreaRect, _propsScroll, viewRect);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string valueStr;
                try
                {
                    valueStr = entry.Value == null ? "null" : FormatPropValue(entry.Value);
                }
                catch (Exception ex)
                {
                    valueStr = $"<error: {ex.Message}>";
                }

                var lineRect = new Rect(0, i * LineHeight, viewRect.width, LineHeight);
                GUI.Label(lineRect, $"{entry.Key}: {valueStr}", _labelStyle);
            }

            GUI.EndScrollView();
        }

        // Draw Back last so it always renders regardless of what happened above.
        var backRect = new Rect(box.x + Padding, box.y + PropsBoxHeight - LineHeight - 6, PropsBoxWidth - Padding * 2, LineHeight - 3);
        if (GUI.Button(backRect, "< Back (Backspace)", _buttonStyle))
        {
            _showAllProps = false;
        }
    }

    private static readonly Color SteamColor = new(0.35f, 0.65f, 1f);   // blue
    private static readonly Color QuestColor = new(0.75f, 0.45f, 1f);   // purple
    private static readonly Color OtherColor = new(1f, 0.35f, 0.35f);   // red

    private static Color GetPlatformColor(string platform)
    {
        if (string.IsNullOrEmpty(platform)) return OtherColor;

        string p = platform.ToLowerInvariant();
        if (p.Contains("steam") || p.Contains("pc") || p.Contains("windows"))
            return SteamColor;
        if (p.Contains("quest") || p.Contains("oculus") || p.Contains("android"))
            return QuestColor;

        return OtherColor;
    }

    private static string ColorizeName(string name, string platform)
    {
        Color color = GetPlatformColor(platform);
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{name}</color>";
    }

    private static string FormatPropValue(object value)
    {
        // Arrays/objects stringify uselessly by default (e.g. "System.Object[]") — expand them
        if (value is Array arr)
        {
            var items = arr.Cast<object>().Select(v => v?.ToString() ?? "null");
            return $"[{string.Join(", ", items)}]";
        }
        return value.ToString();
    }

    private static List<string> BuildLines(HudPage page)
    {
        var lines = new List<string>();
        foreach (IHudStat stat in page.Stats)
        {
            string raw = SafeGetValue(stat);
            string[] parts = raw.Split('\n');

            if (parts.Length == 1)
            {
                lines.Add($"{stat.Label}: {parts[0]}");
            }
            else
            {
                lines.Add($"{stat.Label}:");
                lines.AddRange(parts.Select(p => $"  {p}"));
            }
        }
        return lines;
    }

    private static string SafeGetValue(IHudStat stat)
    {
        try { return stat.GetValue(); }
        catch { return "N/A"; }
    }

    private void EnsureStylesLoaded()
    {
        if (_labelStyle != null) return;

        var background = MakeTexture(new Color(0f, 0f, 0f, 0.55f));
        _boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = background } };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            richText = true,
            normal = { textColor = Color.white }
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            richText = true,
            normal = { textColor = new Color(1f, 0.85f, 0.3f) }
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            richText = true,
            alignment = TextAnchor.MiddleLeft
        };
    }

    private static Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private sealed class HudPage
    {
        public string Name { get; }
        public List<IHudStat> Stats { get; }

        public HudPage(string name, List<IHudStat> stats)
        {
            Name = name;
            Stats = stats;
        }
    }
}