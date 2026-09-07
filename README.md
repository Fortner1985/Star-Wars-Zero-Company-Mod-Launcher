# Star Wars Zero Company — Mod Launcher (ZeroSuite)

A launcher and camera-fix mod for the PC port of **Star Wars Zero Company**
(Steam App ID `2075800`), built on [UE4SS](https://github.com/UE4SS-RE/RE-UE4SS).

Most mod managers install files and stop there. This project exists because
installing the files turned out to be the easy half. On this game, UE4SS can
fail to start its Lua VM at all — and when that happens **every mod silently
does nothing, with no mod-specific error anywhere**. You are left staring at a
correctly-installed mod that appears broken. ZeroSuite detects that state and
says so in plain language.

---

## Honest status

This table is the most important thing in this README. Nothing here is
oversold, and "untested" means untested.

| Component | State | How we know |
|---|---|---|
| Game detection | ✅ Confirmed | Resolves your Steam install via `libraryfolders.vdf`, any library drive |
| Health check | ✅ Confirmed | Correctly diagnosed the silent loader failure that cost this project hours |
| Mod toggles | ✅ Confirmed | Parses and re-emits `mods.txt` / `config.lua`, keeps `.zsbak` backups |
| **VSync fix** | ✅ Confirmed | The in-game menu reports VSync on while the engine flag that controls it is off. Fixes the screen tearing in exclusive fullscreen. |
| **Quality auto-detect repair** | ✅ Confirmed | Auto-detect was marked complete with both benchmark results at `-1`, pinning every quality group to Epic without ever measuring the machine. |
| **Squad Six support** | ✅ Confirmed | Detects the mod and surfaces its self-reported status. The mod itself is third-party and not shipped here. |
| Object dump bridge | ✅ Confirmed | Produced a 160 MB UE4SS object dump on request |
| ZeroCam #4 — terrain clip-through | ⚠️ Applies correctly, effect unverified | Live run 2026-09-07: `applied=3` — exactly the three tactical positioning arms. Whether it *stops the clipping* is not yet confirmed. |
| ZeroCam #6 — floaty exploration camera | ⚠️ Applies correctly, effect unverified | Live run: `applied=7`. Same caveat. |
| Stability | 🔬 Under investigation | A fatal error occurred during the first live run. See below. |
| ZeroCam #7 — cut-plane opacity | 🔬 Stage 1, read-only | Writes nothing. Waiting on one log line to learn the material parameter name. |
| ZeroCam #1 #2 #3 #5 | 🚧 Stubs | No-ops, shipped off by default. See Issues. |
| Cutscene FPS drops | 🔬 Investigating | Cause not established. Help wanted. |

### Stability warning — read this before installing

The first live run of these fixes ended in a fatal error. The most likely cause
was a deferred re-write to spring arms that did not check whether the object
still existed — combat destroys cinematic camera arms constantly, and writing to
a destroyed one is an access violation. **That retry is now off by default and
guarded when enabled** (`reapply_delay_ms = 0`).

Honest uncertainty: this machine also produced three crash dumps earlier the
same morning *before ZeroCam was ever enabled*, so the game crashes on its own
too. We do not yet know whether the mod caused that crash or joined an existing
problem. Treat this release as experimental and back up your saves.

If you hit a crash with ZeroCam enabled, please open an issue with your
`UE4SS.log` — that is what will settle it.

---

## What it does

**The launcher (ZeroSuite)**

- Finds your game automatically across every Steam library drive
- Reads `UE4SS.log` and tells you whether the loader actually started — the
  difference between "your mod is broken" and "nothing ran at all"
- Toggles mods and individual camera fixes, backing up each file it edits
- Requests a UE4SS object dump from inside a running mission
- Reads (never rewrites) third-party `zcom-mod.json` manifests, so it coexists
  with ZCOM Mod Manager instead of fighting it

**Display and performance fixes** — these are launcher-side, they need no mod
running, and they are the most immediately useful thing here:

- **VSync** — the in-game menu shows VSync as on while `bUseVSync` is actually
  off. That mismatch is what produces screen tearing in exclusive fullscreen.
  The menu tells you it is on; the engine disagrees.
- **Quality auto-detect** — `bHasDoneAutoDetect=True` with
  `LastGPUBenchmarkResult=-1` and `LastCPUBenchmarkResult=-1`. The game recorded
  that it had benchmarked your machine while both results are the "never ran"
  sentinel, so every quality group was pinned to Epic without measuring
  anything. This forces a real benchmark on next launch.

Every display change is **reversible** — the previous value is recorded before
anything is written, and the launcher keeps a `.zsbak` of each file it edits.

One thing deliberately absent: a resolution-scale toggle was built and then
removed. The game recomputes `ResolutionPercentage` at runtime and rewrites it
on exit, so the toggle could not hold its value. A control that silently reverts
is worse than no control, because it looks like it worked.

**Squad Six** — the six-unit deployment mod is a **third-party community mod and
is not distributed here** (no licence file, so no redistribution right). The
launcher detects it, toggles it, and surfaces its own status line so you can see
whether it actually started. Our patches to it live in `patches/squad-six/`.

**ZeroSuiteBridge (the mod)** — a small companion mod that lets the launcher
request UE4SS dumps from inside a running mission. It polls for a request file
rather than acting at startup, on purpose: the dumpers only record what is
currently loaded, so a menu-time dump misses every combat class.

**ZeroCam (the mod)** — two fixes active, one read-only, four stubs:

- **#4 Terrain clip-through** — forces collision probing on the spring arms that
  position the tactical and exploration cameras
- **#6 Floaty controls** — cuts interpolation lag on the exploration camera

Both are scoped to gameplay cameras only. Ability and cinematic camera rigs are
left alone on purpose: those shots pass through geometry and carry motion
deliberately, and "fixing" them would wreck every ability cutscene. See
[docs/CAMERA_ARCHITECTURE.md](docs/CAMERA_ARCHITECTURE.md).

---

## Install

Full guide: **[docs/INSTALL.md](docs/INSTALL.md)**. Short version:

1. Download the latest release, unzip anywhere, run `ZeroSuite.exe`.
   No .NET install needed — it ships self-contained.
2. Let it locate your game, or browse to it.
3. Use the health panel to confirm UE4SS is actually running.
4. Toggle the fixes you want, click Apply, launch the game.

**Requires UE4SS `v3.0.1-1125-g527a483b` (experimental-latest) or newer.** The
stock v3.0.1 release from Feb 2024 **cannot load this game at all** — its
pattern scan times out and no mod runs. If mods appear to do nothing, this is
almost always why, and the health panel will tell you.

UE4SS is fetched from its official release page and is **not** redistributed here.

### UE 5.6 signature file

This game needs `ue4ss/UE4SS_Signatures/StaticConstructObject.lua` present or
UE4SS cannot finish its scan. This is a known upstream UE 5.6 issue affecting
many titles (RE-UE4SS #1197, #1204, #1289, #1311). The file is game-build
specific: **if the game updates, it may stop working.** Open an issue if that
happens rather than assuming ZeroCam regressed.

---

## Where this stands right now

**[docs/STATUS.md](docs/STATUS.md) is the handoff document** — current state,
next actions in priority order, the crash post-mortem, two corrections to
things that were confidently believed and wrong, and the mistakes a newcomer
will make. Read it before touching anything.

## Repository layout

One source of truth. `mod/` is the source for the Lua mods, `launcher/` for the
launcher; the copies inside the game folder are deployment targets, never edited
directly.

```
bin/deploy "<game folder>"     copy mod/ into the game's ue4ss/Mods
cd launcher && bash bin/release    build the launcher -> launcher/dist/
```

This project spent a session with three copies of the same Lua files and tested
the wrong one more than once. `bin/deploy` exists so that cannot happen again.

## Contributing

Yes, please — see **[CONTRIBUTING.md](CONTRIBUTING.md)**. The four stub fixes
are deliberately left in the repo with their reasoning written out, so anyone
can pick one up. The most valuable contribution right now is not code: it is a
log line from a real machine that is not the developer's.

## Licence

[Apache 2.0](LICENSE) — free for anyone to use, modify and redistribute. See
**[PATENTS.md](PATENTS.md)** for the patent posture: this is published so that
it stays free, and so nobody can lock it up.

Not affiliated with, endorsed by, or connected to Lucasfilm, Disney, Bit
Reactor, or Respawn. "Star Wars" is a trademark of Lucasfilm Ltd.
