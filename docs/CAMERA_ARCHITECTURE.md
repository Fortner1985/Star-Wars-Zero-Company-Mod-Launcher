# Camera architecture

Mapped from a UE4SS object dump (160 MB) taken from inside a live combat
mission on 2026-09-07. Everything below is read off that dump. Where something
is inferred rather than observed, it says so.

This document replaces guesswork. An earlier discovery pass probed invented
class names (`TacticalCameraManager`, `ActionCameraComponent`) and reported
"not found" fifteen times out of seventeen. None of those classes exist.

## The camera system is Blueprint camroids

Internal codename **Bruno**. The live pieces:

```
BP_BrunoPlayerCameraManager_C     the camera manager
BP_BrunoPlayerController_C        the player controller
BP_SplineOfAction_C               the rail the tactical camera runs on
BR_Camroid_*                      the camera rigs
```

### Camroid families

| Family | Purpose | Safe to modify? |
|---|---|---|
| `BR_Camroid_Tactical_Pro_*` | Tactical combat camera | ✅ gameplay |
| `BR_Camroid_Explore_Pro_*` | Exploration camera | ✅ gameplay |
| `BR_Camroid_Ability_*` | Per-ability shots (AimingPro, Commander, Reaction_Fire, Breakdown1) | ❌ scripted |
| `BR_Camroid_Cinematic_Cloud_*` | Cutscene rigs (Track_Cowboy, Track_Full, Bounds, DynamicAction) | ❌ scripted |
| `BR_Camroid_Zero` | — | ❌ unexamined |

**The scoping rule, and why it matters more than any individual fix:** the
`Ability_*` and `Cinematic_*` rigs pass through geometry and carry motion
*deliberately*. Forcing collision on them, or stripping their lag, breaks the
framing of every ability cutscene in the game. That is a regression that will be
reported as "the mod broke my game", and it is easy to cause by accident because
these rigs use exactly the same components as the gameplay ones.

The rule is enforced once, in `main.lua` (`classify_arm`, `is_gameplay_family`),
so it cannot drift between fix modules.

## Spring arms

The game uses stock `/Script/Engine.SpringArmComponent` — **109 live instances**.
Each camroid owns several, named by role:

| Arm | Role | Used by |
|---|---|---|
| `Collision_Boom` | Collision probing | Fix #4 |
| `B_BoomA`, `B_BoomB` | Main positioning booms | Fix #4 |
| `B_Aim` | Aim offset | — |
| `B_UserOffset` | Player-driven offset | Fix #6 |
| `B_AnimOffset` | Animation-driven offset | skipped by #6 (carries intended motion) |
| `B_Shake` | Camera shake | skipped by #6 (same reason) |
| `CinematicFollowCamAttach` | Per-character cinematic attach | never touched |

Properties, verbatim from the class:

```
bDoCollisionTest         BoolProperty     ProbeSize                FloatProperty
ProbeChannel             ByteProperty     TargetArmLength          FloatProperty
bEnableCameraLag         BoolProperty     CameraLagSpeed           FloatProperty
bEnableCameraRotationLag BoolProperty     CameraRotationLagSpeed   FloatProperty
CameraLagMaxDistance     FloatProperty    bUsePawnControlRotation  BoolProperty
```

The dump lists property *definitions*, not their runtime *values*. We know what
is settable; we do not know what the game sets it to. That distinction is why
both shipped fixes are "untested" rather than "verified".

## Cutscene / sequence system — open investigation

```
313    distinct LS_* level sequences resident
14413  objects living inside MovieScene instances
127    CineCameraComponents that are sequence spawnables
27     CineCameraComponents live in PersistentLevel
```

Every ability owns a camera sequence: `LS_Rocketstrike_Cam`,
`LS_AG_LightsaberStrike_F`, `LS_Cloud_2hRifle_Crouching_Target_To_Source` (590
objects on its own).

**Ruled out: an actor leak.** Camroid actors exist once each
(`_C_0`, plus `_C_1` for `Reaction_Fire`). An earlier count suggesting 36 Explore
and 35 Tactical camroids was counting *references*, not distinct objects.

**Still open.** Two candidates, needing different fixes:

1. Spawnable instantiation hitching at shot start — a game-thread cost
2. Cinematic depth of field through the shot — `CineCameraComponent` focus
   tracking runs a per-frame trace; a GPU cost

`stat unit` and `stat gpu` in the console during an ability cutscene would
separate them. `ConsoleEnablerMod` binds the console to tilde and F10.

## Squad size, for the record

Not a camera issue, but found in the same dump and useful to anyone modding
deployment limits:

```
/Script/BitReactorGame.BitReactorMissionData:MaxSquadSize   offset 0x24C
/Script/BitReactorGame.BitReactorMissionData:MinSquadSize   offset 0x250
/Script/Bruno.BrunoMissionViewModel:MinSquadSize            offset 0x2E0
/Script/Bruno.BrunoMissionViewModel:MaxSquadSize            offset 0x2E4
```

Plain `IntProperty` — no enum, no bitfield. The dump contains only the class and
its CDO, no live per-mission instances, so the value comes from a loaded data
asset; the reliable runtime handle is the `BrunoMissionViewModel` instance.

The `MaxPartySize` hits on `Engine.GameSession` and `CoreOnline.JoinabilitySettings`,
and `Party.SocialSettings:DefaultMaxPartySize`, are multiplayer session plumbing.
Ignore them.
