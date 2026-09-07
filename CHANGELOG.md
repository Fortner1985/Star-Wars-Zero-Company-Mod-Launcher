# Changelog

Format: [Keep a Changelog](https://keepachangelog.com/). Versioning: [SemVer](https://semver.org/).

## [0.2.0] — 2026-09-07

First public release.

### Added
- ZeroCam #4 (terrain clip-through) and #6 (floaty exploration controls).
  **Both untested in a live game** — see the status table in the README.
- Shared spring-arm classification in `main.lua` (`classify_arm`,
  `is_gameplay_family`, `each_spring_arm`), so the gameplay-vs-cinematic scoping
  rule lives in one place and cannot drift between fix modules.
- `docs/CAMERA_ARCHITECTURE.md` — the camera system as actually mapped from a
  UE4SS object dump, replacing guesswork.
- Apache 2.0 licence, NOTICE, PATENTS.md, contribution guide, issue templates.

### Changed
- `discovery_mode` now defaults to off. The object dump answered everything the
  discovery modules were written to find.
- Stub fixes #1/#2/#3/#5 ship toggled **off**, so the log reports what is
  actually applied instead of six "loaded" lines for two working fixes.

### Removed
- `discovery.lua` is not shipped. Superseded by the object dump.

### Fixed
- Corrected an analysis error before it became a false lead: camroid actors were
  briefly believed to be duplicating (36 Explore, 35 Tactical). Instance indices
  showed one of each. There is no actor leak.

### Notes
- Squad Six is **not** redistributed here. It is a third-party mod with no
  licence file. Only our patches to it ship, in `patches/squad-six/`.
