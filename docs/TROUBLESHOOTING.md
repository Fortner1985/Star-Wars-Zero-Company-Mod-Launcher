# Troubleshooting

Everything here is read from `SWZeroCompany/Binaries/Win64/ue4ss/UE4SS.log`.
The launcher's health panel reads the same file.

## "The mod does nothing"

Ninety percent of the time this is the loader, not the mod.

| Log line | What it means | What to do |
|---|---|---|
| `Fatal Error: PS scan timed out` | UE4SS never started. **No mod ran.** | You are on stock UE4SS v3.0.1. Install experimental-latest. |
| `Failed to find StaticConstructObject_Internal` | Signature missing or stale | Add `UE4SS_Signatures/StaticConstructObject.lua`. If the game just updated, it may need regenerating — open an issue. |
| `PS scan successful` | Loader is healthy | Look at the mod lines next. |
| `Mod 'ZeroCam' disabled in mods.txt.` | It is switched off | Set `ZeroCam : 1` in `mods.txt`. |
| `Starting Lua mod 'ZeroCam'` | It started | Good. Check `applied=` next. |

## `clip_through: applied=0`

The fix ran but changed nothing. Most likely one of:

- **No mission loaded.** The gameplay camroids do not exist on the main menu.
  Load into a mission and check the log again.
- **The game re-applies its own values on mission load.** We suspect this is
  possible and have not proven otherwise. If you see a non-zero `applied=` at
  startup and clipping still happens in play, this is very likely the cause —
  please say so in an issue, it is exactly the evidence we lack.

## The game crashed with ZeroCam enabled

Known possibility, and we want the report. A fatal error occurred during the
first live run of these fixes. The suspected cause — an unguarded deferred write
to camera components — has been removed, but this is not proven, and the same
machine crashed three times that morning with the mod disabled.

If it happens to you, open an issue with your `UE4SS.log` and whether
`reapply_delay_ms` is anything other than `0`. Do not re-enable that setting
unless you are debugging this specific question.

## The camera feels wrong after installing

Both active fixes are scoped to gameplay cameras only. If ability or cinematic
shots look wrong, that should not be us — but check by setting both to `false`
in `ZeroCam/Scripts/config.lua` and relaunching. Either way, open an issue.

## The game updated and everything broke

The signature file is tied to a specific game build. When the game patches, the
address it points at can move. This presents as the loader failing again. Open
an issue with your game build number.

## Crash dumps

UE4SS writes `crash_*.dmp` next to the log. They are ~43 MB each and accumulate
silently — worth deleting occasionally. Their presence does not by itself mean a
mod caused the crash.

## Still stuck

Open an issue with:

- Your game build number
- Your UE4SS version
- The `UE4SS.log` (attach it — do not paste; it is long)
- What you expected and what happened
