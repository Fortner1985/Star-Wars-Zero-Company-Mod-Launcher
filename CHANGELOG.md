# Changelog

Format: [Keep a Changelog](https://keepachangelog.com/). Versioning: [SemVer](https://semver.org/).

## [0.2.0] — 2026-09-07

First public release.

### Added
- ZeroCam #4 (terrain clip-through) and #6 (floaty exploration controls).
  Confirmed **applying** in a live mission (`clip_through: applied=3`,
  `floaty_controls: applied=7`) with 300+ cinematic arms correctly skipped.
  Whether they fix the underlying camera problems is **not yet confirmed**.
- Deferred summary reporting. The first version tallied at init — on the menu,
  before any camroid exists — so it printed `applied=0` unconditionally and
  could never report success.
- Shared spring-arm classification in `main.lua` (`classify_arm`,
  `is_gameplay_family`, `each_spring_arm`), so the gameplay-vs-cinematic scoping
  rule lives in one place and cannot drift between fix modules.
- ZeroCam #7 (object-hiding cut-plane opacity), **stage 1: read-only**. Reports
  the live `BitReactorObjectHidingSettings` values so stage 2 can be written
  against the real material parameter name instead of a guess. Piggybacks on the
  existing spring-arm notification rather than adding a timer.
- `docs/STATUS.md` — handoff document: state, next actions, crash post-mortem,
  corrections, and the traps a newcomer will hit.
- `docs/CAMERA_ARCHITECTURE.md` — the camera system as actually mapped from a
  UE4SS object dump, replacing guesswork.
- Apache 2.0 licence, NOTICE, PATENTS.md, contribution guide, issue templates.

### Documentation
- README now covers the launcher's display and performance fixes (VSync,
  quality auto-detect) and its Squad Six support. The first version documented
  the mod and silently omitted half of what the launcher actually does.

### Changed
- Corrected a stale warning in the launcher UI. It read "Not yet implemented -
  every fix loads as a stub and applies nothing", which stopped being true once
  #4 and #6 started applying. A tool that misreports its own state is the exact
  failure this project exists to fix.
- `discovery_mode` now defaults to off. The object dump answered everything the
  discovery modules were written to find.
- Stub fixes #1/#2/#3/#5 ship toggled **off**, so the log reports what is
  actually applied instead of six "loaded" lines for two working fixes.

### Removed
- `discovery.lua` is not shipped. Superseded by the object dump.

### Security / stability
- Removed an unguarded deferred write to spring arms. It wrote to each arm
  250 ms after construction with no validity check; combat destroys cinematic
  arms constantly, making it a use-after-free and the prime suspect for a fatal
  error during the first live run. The retry is now off by default
  (`reapply_delay_ms = 0`) and validates the object when enabled.

### Fixed
- Corrected an analysis error before it became a false lead: camroid actors were
  briefly believed to be duplicating (36 Explore, 35 Tactical). Instance indices
  showed one of each. There is no actor leak.

### Notes
- Squad Six is **not** redistributed here. It is a third-party mod with no
  licence file. Only our patches to it ship, in `patches/squad-six/`.
