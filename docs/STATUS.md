# Where we left off

Last updated: 2026-09-07. This is the handoff document. If you are picking this
project up — including the author, in a month — read this first.

The rule this project runs on: **nothing is marked working until someone has
watched it work.** Two claims in this file were wrong when first written and are
corrected here. That is the point of the file.

---

## The state of everything

| Thing | State | What is actually known |
|---|---|---|
| Launcher — game detection | ✅ Works | Resolves Steam via `libraryfolders.vdf`. No hardcoded paths. Builds self-contained. |
| Launcher — health check | ✅ Works | Correctly identified the silent loader failure that cost this project hours. |
| Launcher — toggles | ✅ Works | Parse-modify-re-emit with `.zsbak` backups. |
| Launcher — VSync fix | ✅ Works | `bUseVSync=False` while the menu reports on. Cause of exclusive-fullscreen tearing. |
| Launcher — quality auto-detect | ✅ Works | `bHasDoneAutoDetect=True` with both benchmark results `-1`; everything pinned to Epic unmeasured. |
| Launcher — Squad Six support | ✅ Works | Detects, toggles, surfaces its status line. Mod itself not redistributed. |
| Launcher — render resolution | ⚠️ Built, **untested** | Sets `ResolutionSizeX/Y` to the selected resolution and lifts `ResolutionPercentage` to the game's stated minimum. Nobody has launched with it yet. |
| Launcher — HDR / dynres flag pairs | ⚠️ Built, **untested** | Sets both halves of each contradictory pair together. Which half the engine reads is still unknown. |
| Resolution-scale toggle | ❌ Removed | The game recomputes `ResolutionPercentage` at runtime and rewrites it on exit. A toggle that cannot hold its value is worse than none. |
| Bridge — object dump | ✅ Works | Produced a 160 MB dump on request from inside a live mission. |
| ZeroCam #4 clip-through | ⚠️ Applies, effect **unverified** | `applied=3` then `9` as more camroids spawn — exactly the tactical positioning arms. Nobody has yet seen it stop a camera going through a wall. |
| ZeroCam #6 floaty controls | ⚠️ Applies, effect **unverified** | `applied=7` then `12`. Never tested in an exploration section, which is the only place it does anything. |
| ZeroCam #7 cut plane | 🔬 Stage 1, read-only | Writes nothing. Waiting on one log line. |
| ZeroCam #1 #2 #3 #5 | 🚧 Stubs | No-ops, toggled off. Blocked on runtime event timing, not on more dumping. |
| Cutscene FPS drops | 🔬 Cause unknown | Two candidates, one measurement away from being settled. |
| Stability | ⚠️ Improved, not proven | One crash caused by our code, fixed. Three earlier crashes with the mod disabled. |

---

## Next action, in priority order

**1. Fix #7 needs one log line.** Restart, load a mission, then search `UE4SS.log`
for `cut_plane:`. Stage 1 prints the live values of `BitReactorObjectHidingSettings`,
including `ApplyCutPlaneParameterName` — the scalar the game drives the plane's
appearance through. With that name, stage 2 is a short piece of work: create a
dynamic material instance and set the scalar from `cut_plane_opacity` in
`config.lua` (1.0 solid, 0.0 invisible; lower is more see-through, which is the
direction requested).

**2. Verify #4 for real.** Find geometry the camera used to punch through and pan
into it hard. The blue plane is *not* that test — see the correction below.

**3. Verify #6.** Needs an exploration section. It touches `Explore_Pro_*`
camroids only, so it will legitimately report nothing useful in combat.

**4. Settle the FPS question.** `stat unit` and `stat gpu` during an ability
cutscene. `Game` spiking and `GPU` spiking point at entirely different fixes,
and guessing wrong costs a build cycle.

---

## The game reverts some settings by itself (measured 2026-09-07)

Applied with the game closed, then launched. Reading the file afterwards:

| Setting | Written | Survived? |
|---|---|---|
| `ResolutionSizeX/Y` = 3840x2160 | yes | present in the file, **but see below** |
| `LastUserConfirmedResolutionSizeX/Y` | yes | present in the file, **but see below** |
| `bUseVSync=True` | yes | **yes** |
| `ResolutionPercentage` -> 74.01 | yes | **no** — back to 66.699997 |
| `bUseHDRDisplayOutput` / `bHDROutputEnabled` | yes | **no** — contradictory pair restored |
| `bUseDynamicResolution` / `bUserDynResEnabled` | yes | **no** — contradictory pair restored |

The sidecar recorded originals for every one of these, so the writes did land;
the game reverted them afterwards.

This matters more than it looks. It means **the contradictory flag pairs are not
a stale artifact — the game actively rewrites them into disagreement on every
launch.** Something in the settings code writes one half of each pair and not
the other. Fixing HDR and dynamic resolution by editing the ini therefore cannot
work; whatever writes those values has to be reached at runtime instead, which
makes them mod territory rather than launcher territory.

It also confirms the resolution-scale finding for a second time. That is now
twice a `ResolutionPercentage` edit has failed to stick, by two different
routes. Do not attempt a third without a new mechanism.

VSync sticks and is real.

**Resolution is NOT confirmed, and the way it was briefly mis-confirmed is worth
recording.** The ini reads 3840x2160 in every relevant key, and that was taken as
proof the fix worked. The in-game menu still read 1920x1080. Process times
settled it:

```
game process started   9:55:29
our write (sidecar)    14:55
ini last modified      14:56   (by the game)
```

The game launched in the same minute as the write and had already read the old
values. The file was evidence about the file, not about the game.

**The lesson, which is the same one this project keeps relearning:** a value in a
config file is not an observed behaviour. The in-game menu is ground truth for a
display setting, the way `applied=` in the log is ground truth for a camera fix.
Do not mark a row confirmed from the artefact you wrote yourself.

The clean test still to run: quit to desktop, confirm the file reads 3840,
launch fresh, read the menu.

## Two corrections worth keeping

Both of these were confidently believed and both were wrong. They are recorded
because the reasoning that produced them will recur.

**There is no camroid actor leak.** An early count suggested 36 Explore and 35
Tactical camroids where there should be one of each — an obvious smoking gun for
the FPS problem. Checking the instance indices showed `_C_0` and nothing else.
The count was counting *references* in the dump, not distinct objects. A grep
count is not an object count.

**The blue plane is not a camera bug.** It looked exactly like the camera falling
through the world, and it was reported as possible evidence that fix #4 was
failing. It is `BP_ObjectHidingCutPlane_C`, a deliberate part of the game's
object-hiding system, with 14 live instances. It is supposed to be there. Fix #4
was never implicated, and that screenshot is not evidence about #4 in either
direction.

---

## The crash

A fatal error occurred on 2026-09-07 at 08:47:21, about 90 seconds into combat.

Cause, almost certainly ours: a retry added earlier that day wrote to each spring
arm 250 ms after construction **with no validity check**. Combat creates and
destroys cinematic camera arms constantly — a single mission skipped over 400 of
them — so some of those deferred writes landed on freed memory.

Fixed: the retry is off by default (`reapply_delay_ms = 0`) and validates the
object with `IsValid` when enabled. A subsequent run went 14+ minutes in combat
with no crash dump and no fatal error in the log.

**Do not treat this as closed.** The same machine produced three crash dumps that
morning (06:52, 07:23, 07:24) with ZeroCam disabled entirely, so the game crashes
on its own. What we know is that our crash signature stopped; we do not know that
the game is stable.

**The rule this produced, which matters beyond this bug:** a deferred write to a
UObject must check `IsValid` immediately before writing, every time. A `pcall`
does not save you — a write to freed memory takes the process down before Lua
can catch anything.

---

## One source of truth

`mod/` and `launcher/` in this repo are the source. The copies in the game
folder are deployment targets. Use `bin/deploy` and
`launcher/bin/release`; do not edit the deployed copies.

This rule was written after a session in which two launcher trees and three
copies of the same Lua files existed at once, and the wrong one got tested
twice — once wasting a full game restart on unchanged code.

## Things a newcomer will get wrong

- **Reloading a save does not reload the mod.** UE4SS starts Lua mods once, when
  the process launches. Editing a script and reloading a save tests the old code.
  Exit to desktop.
- **`FindAllOf` at startup returns nothing.** Mods initialize on the menu, where
  no camroid exists. This is normal. The first version of the summary reported at
  init and therefore printed `applied=0` unconditionally — it was structurally
  incapable of reporting success. Report late, not at init.
- **Do not poll.** `NotifyOnNewObject`, never a `LoopAsync` sweep. A recurring
  scan of 100+ spring arms is a framerate regression that gets filed as a camera
  bug. This mistake is already in this codebase's history once.
- **Family scoping is not optional.** `Ability_*` and `Cinematic_*` camroids pass
  through geometry and carry motion deliberately. Touching them breaks every
  ability cutscene. `classify_arm` in `main.lua` is the single source of truth;
  keep it that way.

---

## Not in this repository

- **The UE4SS signature derivation method.** Held back pending a patent filing
  decision. The resulting signature file is required and documented; how it is
  produced is not. When the game patches and the signature breaks, the public
  answer is "open an issue".
- **Squad Six.** Third-party mod, no licence file, therefore no redistribution
  right. Only our patches ship, in `patches/squad-six/`, offered upstream.
