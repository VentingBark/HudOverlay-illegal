# HUD Overlay

A minimal, display-only info overlay, now split into pages: **Self**
(FPS, ping, name, color) and **Server** (room name, player count, region,
host, full player list). No game values are modified — every stat here
just *reads* something and prints it.

## Structure

```
Constants.cs          Plugin id/name/version
Plugin.cs             BepInEx entry point, spawns the HUD object
Hud/
  IHudStat.cs          The interface every stat implements
  HudController.cs     Draws the box, owns the page list, loops over stats
  Stats/
    FpsStat.cs
    PingStat.cs
    NameStat.cs
    ColorStat.cs
    RoomNameStat.cs
    PlayerCountStat.cs
    RegionStat.cs
    MasterClientStat.cs
    PlayerListStat.cs
```

## Adding a new stat

1. Create `Hud/Stats/YourStat.cs` implementing `IHudStat`:

```csharp
public class RoomNameStat : IHudStat
{
    public string Label => "Room";
    public string GetValue() => PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "None";
}
```

2. Add one line to the relevant page's stat list inside `HudController.cs`'s
   `_pages` field:

```csharp
new RoomNameStat(),
```

That's it — no other file changes. Layout, sizing, and background all adapt
automatically to however many stats/lines are on the current page.

### Multi-line stats

If `GetValue()` returns a string containing `\n` (see `PlayerListStat`), the
controller automatically expands it into one indented line per entry and
resizes the box to fit. Useful for anything list-shaped (players, teams,
nearby rooms, etc.) without touching layout code.

## Adding a new page

Add another `new HudPage("PageName", new List<IHudStat> { ... })` entry to
the `_pages` list in `HudController.cs`. Pages cycle in the order listed.

## Keybinds

- **Numpad 1** — show/hide the whole HUD (`HudController.ToggleKey`)
- **Tab** — cycle to the next page (`HudController.NextPageKey`)

Both are single constants at the top of `HudController.cs` if you want to
rebind them.

## Notes

- `ColorStat` reads `VRRig.LocalRig.playerColor`. That's the standard field
  name, but if the game has updated its Assembly-CSharp since this was
  written, you may need to re-point it to whatever the current build calls
  the local player's color (check with a decompiler like dnSpy/ILSpy).
- Each stat is wrapped in a try/catch when read, so if one stat's game API
  changes or throws, the rest of the HUD keeps working — that line just
  shows "N/A".
