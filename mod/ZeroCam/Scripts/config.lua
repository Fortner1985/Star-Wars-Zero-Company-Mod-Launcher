-- ZeroCam Configuration
-- Edit these values to enable/disable individual fixes and tune behavior.
-- After changing, restart the game for settings to take effect.

local Config = {
    version = "0.2.0",

    -- Logging: "debug", "info", "warn", "error"
    log_level = "info",

    -- When true, only runs discovery logging without applying any fixes.
    -- Turned OFF 2026-09-07: the UE4SS object dump answered what discovery
    -- was written to go and find, so there is nothing left for it to learn.
    -- discovery.lua is kept for reference but no longer runs.
    discovery_mode = false,

    -- Individual fix toggles (only apply when discovery_mode = false)
    fixes = {
        -- #1/#2/#3/#5 are still stubs: they need runtime event timing that a
        -- static object dump cannot supply. Left OFF so the log reflects
        -- what is actually applied rather than six "loaded" lines for two
        -- working fixes.
        action_camera    = false,  -- Fix #1: STUB
        blind_spot       = false,  -- Fix #2: STUB
        enemy_turn_lock  = false,  -- Fix #3: STUB
        clip_through     = true,   -- Fix #4: Prevent terrain clip-through  [ACTIVE]
        pitch_black      = false,  -- Fix #5: STUB
        floaty_controls  = true,   -- Fix #6: Reduce floaty third-person camera  [ACTIVE]
        cut_plane        = true,   -- Fix #7: Cut-plane opacity  [STAGE 1: READ-ONLY]
        cut_plane        = true,   -- Fix #7: Object-hiding cut plane  [PROBE ONLY]
    },

    -- === Fix #3: Enemy Turn Lock ===
    -- "free"  = re-enable player camera input during enemy turn
    -- "auto"  = auto-reset camera to overhead view at start of enemy turn
    enemy_turn_camera_mode = "free",

    -- === Fix #5: Pitch-Black Watchdog ===
    -- How often (ms) to check if the camera has fallen into the void
    pitch_black_check_interval = 500,
    -- Z-coordinate threshold; if camera drops below this relative to the
    -- lowest terrain point found, trigger recovery
    pitch_black_min_height_offset = -500.0,

    -- === Fix #7: Object-hiding cut plane ===
    -- The blue plane shown when the camera drops below a floor. Intended game
    -- behaviour, not a bug -- this only makes it tunable.
    --
    -- 1.0 = fully solid, 0.0 = invisible. Lower is more see-through.
    -- NOT YET APPLIED: fix #7 is read-only until the log tells us the real
    -- material parameter name. Setting this now does nothing.
    cut_plane_opacity = 0.35,

    -- === Spring arm writes (fixes #4 and #6) ===
    -- Milliseconds to wait before writing each arm a second time, in case the
    -- Blueprint construction script overwrites our first write.
    --
    -- 0 = OFF, and 0 is correct. An unguarded version of this retry is the
    -- prime suspect for a fatal error on 2026-09-07: it wrote to spring arms
    -- 250ms after construction, and combat destroys cinematic arms constantly,
    -- so some of those writes landed on freed memory. The live run also showed
    -- the retry was unnecessary -- values stick at construction time.
    --
    -- If you enable it, the code validates the object first. Do not remove
    -- that check.
    reapply_delay_ms = 0,

    -- === Fix #4: Terrain Clip-Through ===
    -- Applied to Collision_Boom / B_BoomA / B_BoomB on the Tactical_Pro_* and
    -- Explore_Pro_* camroids only. Ability_* and Cinematic_* rigs are left
    -- alone on purpose: those shots pass through geometry by design.
    clip_through = {
        -- Spring arm collision probe radius. Larger catches more geometry at
        -- the cost of the camera sitting further off walls.
        probe_size = 24.0,
        -- Collision channel for the probe. nil = leave the game's own choice
        -- alone, which is the safe default; set a number only if clipping
        -- persists and you know the channel you want.
        probe_channel = nil,
    },

    -- === Fix #7: Object-Hiding Cut Plane ===
    -- The blue plane drawn when the camera drops below a floor. This is
    -- intended game behaviour, not a clipping bug -- it was mistaken for one.
    --
    -- With this table empty the fix WRITES NOTHING. It only reads the live
    -- settings object and logs its values, which the object dump could not
    -- give us. Fill in a key below to override that value.
    --
    -- Opacity is not settable from here yet: none of these is an opacity
    -- float. It almost certainly lives inside the cut plane material as a
    -- named scalar parameter, and the probe log is how we find its name.
    --
    -- Available keys (all numbers), commented out until we know the originals:
    --   DefaultCutHeightOffset       how far below the floor the cut sits
    --   OpacityFadeCurveLength       length of the opacity fade
    --   DefaultOpaqueTransitionTime  time to reach full opacity
    --   EdgeFadeCurveLength          softness at the plane edges
    --   IntroCurveLength / OutroCurveLength
    cut_plane = {
        -- DefaultOpaqueTransitionTime = 0.05,
        -- EdgeFadeCurveLength = 0.0,
    },

    -- === Fix #6: Floaty Controls ===
    -- Camera lag speed (higher = snappier, 0 = instant)
    -- The game's default is unknown until discovery; these are starting points.
    exploration_lag_speed = 20.0,
    exploration_rotation_lag_speed = 15.0,
    -- Set true to completely disable camera lag (most responsive)
    exploration_disable_lag = false,
    -- How far the camera may trail its target before the lag is clamped
    exploration_lag_max_distance = 50.0,

    -- === Discovery (inert: discovery_mode is off) ===
    -- Maximum objects to log per class during discovery (avoids log spam)
    discovery_max_per_class = 20,
    -- Classes to enumerate during discovery
    discovery_classes = {
        "CameraComponent",
        "CameraActor",
        "CineCameraActor",
        "CineCameraComponent",
        "SpringArmComponent",
        "PlayerCameraManager",
        "PlayerController",
        "PostProcessVolume",
        "PostProcessComponent",
    },
    -- Functions to hook during discovery (camera transitions)
    discovery_hook_targets = {
        -- Standard UE camera manager functions
        "/Script/Engine.PlayerCameraManager:SetViewTarget",
        "/Script/Engine.PlayerCameraManager:StopCameraFade",
        "/Script/Engine.PlayerCameraManager:StartCameraFade",
        -- Input toggling
        "/Script/Engine.PlayerController:EnableInput",
        "/Script/Engine.PlayerController:DisableInput",
        "/Script/Engine.PlayerController:SetInputMode",
    },
}

return Config
