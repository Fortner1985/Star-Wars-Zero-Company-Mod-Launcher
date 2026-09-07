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
