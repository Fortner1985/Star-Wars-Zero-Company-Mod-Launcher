# Install guide

## What you need

- **Star Wars Zero Company** on Steam (App ID `2075800`)
- **Windows x64**
- **UE4SS `v3.0.1-1125-g527a483b` (experimental-latest) or newer**

You do **not** need .NET installed. The launcher ships self-contained.

## Why the UE4SS version matters more than anything else

The stock UE4SS v3.0.1 release (Feb 2024) **cannot load this game.** Its pattern
scan fails to resolve `EngineVersion`, `GUObjectArray`, `FName::FName`,
`StaticConstructObject_Internal` and `FText::FText`, retries about 56 times, and
dies with `Fatal Error: PS scan timed out`.

No Lua VM is created, so **no mod runs at all** — and because the failure is in
the loader, no mod produces an error. Every mod you installed appears to simply
do nothing.

If you take one thing from this guide: when a mod "does nothing", check the
loader before you blame the mod. The launcher's health panel does this for you.

## Steps

1. **Install UE4SS** (experimental-latest) into
   `steamapps/common/Star Wars Zero Company/SWZeroCompany/Binaries/Win64/`.
   Recent builds use an `ue4ss/` subfolder; the injector `dwmapi.dll` stays at
   the top level.

2. **Add the UE 5.6 signature file.** Place `StaticConstructObject.lua` in
   `Win64/ue4ss/UE4SS_Signatures/`. Without it the scan cannot finish. This is
   an upstream UE 5.6 issue (RE-UE4SS #1197, #1204, #1289, #1311), not specific
   to this game.

3. **Install ZeroCam.** Copy `mod/ZeroCam/` into `Win64/ue4ss/Mods/`, then add
   this line to `Win64/ue4ss/Mods/mods.txt`:
   ```
   ZeroCam : 1
   ```

4. **Run `ZeroSuite.exe`.** It finds your game across every Steam library drive.
   If it cannot, browse to the folder manually.

5. **Check the health panel before playing.** It should report that the loader
   started and that `ZeroCam` is running.

## Expected layout

```
SWZeroCompany/Binaries/Win64/
  dwmapi.dll
  ue4ss/
    UE4SS.dll
    UE4SS-settings.ini
    UE4SS.log
    UE4SS_Signatures/
      StaticConstructObject.lua      <- required
    Mods/
      mods.txt                       <- ZeroCam : 1
      ZeroCam/
        Scripts/
```

## Verifying the fixes actually applied

After a launch with a mission loaded, search `UE4SS.log` for `ZeroCam`:

```
[ZeroCam][INFO] clip_through: applied=6 skipped=103 probe_size=24.0 channel=nil
[ZeroCam][INFO] floaty_controls: applied=4 skipped=105 mode=snappier lag=20.0/15.0
```

`applied=0` means nothing took effect — please open an issue with the line.

## Uninstall

Set `ZeroCam : 0` in `mods.txt`, or delete the `ZeroCam` folder. The launcher
keeps a `.zsbak` copy of every file it edits.

## Troubleshooting

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md).
