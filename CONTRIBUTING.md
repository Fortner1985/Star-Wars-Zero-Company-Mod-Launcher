# Contributing

This project needs help, and the most valuable contribution is probably not code.

## The single most useful thing you can do

Run it and paste a log line.

Two camera fixes have never run on any machine but the developer's. After a
launch, `SWZeroCompany/Binaries/Win64/ue4ss/UE4SS.log` will contain:

```
[ZeroCam][INFO] clip_through: applied=6 skipped=103 probe_size=24.0 channel=nil
[ZeroCam][INFO] floaty_controls: applied=4 skipped=105 mode=snappier lag=20.0/15.0
```

Open an issue with those lines and your GPU/CPU. If `applied=0`, that is a bug
report and a valuable one — it likely means the game overwrites our values on
mission load, which we suspect but have not proven.

## Up for grabs

Four camera fixes are stubs. Each ships with its reasoning written out in the
file, including what specifically blocks it. None are blocked on more object
dumping — they are blocked on **runtime event timing**, which means someone has
to watch the game do the thing.

| Fix | Blocked on |
|---|---|
| #1 Action camera | The trigger function on `BP_BrunoPlayerCameraManager_C`, and the settings toggle it should read. Both are in the object dump; this is an extraction job. |
| #2 Blind spot | Reinforcement event timing and in-flight blend parameters. Hardest of the four. |
| #3 Enemy-turn lock | Which function fires at turn change, and what disables input. |
| #5 Pitch-black | A watchdog over live camera Z. Inherently runtime. |
| #7 Cut-plane opacity | Stage 1 is in and read-only. Needs one log line (`cut_plane:` in UE4SS.log) naming the material scalar, then stage 2 is short. |
| Cutscene FPS | Cause not established. See below. |

### The FPS investigation

Cutscenes drop frames. We know the shape of the system but not the cause:

- 313 `LS_*` level sequences resident, ~14,400 objects inside MovieScene
  instances, 127 sequence-spawnable cine cameras
- **No actor leak** — camroids exist once each; this was checked and ruled out

Two candidate causes needing different fixes: spawnable instantiation hitching
at shot start (game thread), or cinematic depth-of-field through the shot (GPU).
`stat unit` and `stat gpu` in the console during an ability cutscene would
distinguish them. That measurement is worth more than any patch right now.

## House rules

**A deferred write to a UObject must check `IsValid` immediately before
writing, every time.** A `pcall` will not save you: a write to freed memory
takes the process down before Lua can catch anything. This rule exists because
breaking it crashed the game (see docs/STATUS.md).

**Never claim something works that you have not watched work.** The status table
in the README is the project's most valuable asset. If you add a fix, mark it
untested until someone other than you has run it.

- Every UE4SS call goes through `pcall`. A camera mod must never take down the
  game.
- **Do not poll.** Use `NotifyOnNewObject`, not a `LoopAsync` sweep. A recurring
  scan of 109 spring arms is a framerate regression that will be reported as a
  camera bug. This mistake has already been made once in this codebase.
- Respect the family scoping in `main.lua` (`classify_arm`,
  `is_gameplay_family`). `Ability_*` and `Cinematic_*` rigs are off limits —
  they pass through geometry on purpose.
- Keep the classifier in one place so the rule cannot drift between modules.
- Say what you tested and on which game build.

## Building the launcher

Needs the .NET 9 SDK.

```
cd launcher
bash bin/release      # -> dist/ZeroSuite.exe
```

Publishes self-contained `win-x64`, so end users install no runtime. It is
deliberately **not** `PublishSingleFile`: WPF's font cache throws `Invalid URI`
inside a single-file bundle. The reason is recorded in the csproj; please do not
"fix" it without reading that comment.

## Licence of contributions

Apache 2.0, including its patent grant. See [PATENTS.md](PATENTS.md).
