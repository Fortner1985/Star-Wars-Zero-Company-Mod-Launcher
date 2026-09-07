-- ═══════════════════════════════════════════════════════════════════════════
-- ZeroCam — Star Wars Zero Company Camera Fix Mod
-- Version 0.1.0
--
-- Fixes all six documented camera and perspective issues in the PC port.
-- https://github.com/TODO/ZeroCam
--
-- Requires: UE4SS v3.0.1+
-- Game:     Star Wars Zero Company (Steam App ID: 2075800)
-- ═══════════════════════════════════════════════════════════════════════════

local MOD_NAME = "ZeroCam"
local VERSION = "0.1.0"

-- ─── resolve mod directory ──────────────────────────────────────────────────
-- UE4SS runs Lua from the mod's Scripts/ folder. We need the parent (ZeroCam/)
-- for log files and config.

local function get_mod_dir()
    -- Try to figure out our path. UE4SS sets the working directory to
    -- <game>/Binaries/Win64/Mods/<ModName>/Scripts/
    -- We want <game>/Binaries/Win64/Mods/<ModName>/
    local info = debug.getinfo(1, "S")
    if info and info.source then
        local source = info.source:gsub("^@", "")
        local dir = source:match("(.+)[/\\]Scripts[/\\]")
        if dir then return dir end
        -- Fallback: strip the filename
        dir = source:match("(.+)[/\\]")
        if dir then return dir end
    end
    return "."
end

-- ─── load config ────────────────────────────────────────────────────────────

local function load_config()
    -- config.lua sits next to main.lua in Scripts/
    local ok, config = pcall(require, "config")
    if ok and type(config) == "table" then
        return config
    end

    -- Fallback defaults if config.lua is missing or broken
    print("[" .. MOD_NAME .. "] WARNING: Could not load config.lua, using defaults")
    return {
        version = VERSION,
        log_level = "info",
        discovery_mode = true,
        fixes = {
            action_camera = true,
            blind_spot = true,
            enemy_turn_lock = true,
            clip_through = true,
            pitch_black = true,
            floaty_controls = true,
        },
        discovery_classes = {
            "CameraComponent", "CameraActor", "SpringArmComponent",
            "PlayerCameraManager", "PlayerController",
        },
        discovery_hook_targets = {},
        discovery_max_per_class = 20,
        pitch_black_check_interval = 500,
        pitch_black_min_height_offset = -500.0,
        exploration_lag_speed = 20.0,
        exploration_rotation_lag_speed = 15.0,
        exploration_disable_lag = false,
        enemy_turn_camera_mode = "free",
    }
end

-- ─── logging ────────────────────────────────────────────────────────────────

local LOG_LEVELS = { debug = 1, info = 2, warn = 3, error = 4 }

local function make_logger(config)
    local threshold = LOG_LEVELS[config.log_level] or 2

    return function(level, message)
        local level_num = LOG_LEVELS[level] or 2
        if level_num >= threshold then
            print(string.format("[%s][%s] %s", MOD_NAME, level:upper(), message))
        end
    end
end

-- ─── spring arm helpers ─────────────────────────────────────────────────────
-- Added 2026-09-07 from UE4SS_ObjectDump. The dump showed 109 live
-- SpringArmComponents, every one of them named by role and owned by a named
-- camroid, e.g.
--
--   ...PersistentLevel.BR_Camroid_Explore_Pro_B_C_0.Collision_Boom
--
-- Fixes #4 and #6 both need to act on some of those and pointedly not on
-- others, so the family/role split lives here rather than being duplicated
-- (and drifting) in two modules.
--
-- Families: Tactical | Explore | Ability | Cinematic | Zero | unknown
-- Only Tactical and Explore are gameplay cameras. Ability and Cinematic
-- rigs are scripted shots and must be left alone -- forcing collision or
-- stripping lag on those breaks intended framing.

local function arm_full_name(arm)
    local ok, name = pcall(function() return arm:GetFullName() end)
    if ok and type(name) == "string" then return name end
    return nil
end

local FAMILY_PATTERNS = {
    { "BR_Camroid_Tactical_",  "Tactical"  },
    { "BR_Camroid_Explore_",   "Explore"   },
    { "BR_Camroid_Ability_",   "Ability"   },
    { "BR_Camroid_Cinematic_", "Cinematic" },
    { "BR_Camroid_Zero",       "Zero"      },
}

local GAMEPLAY_FAMILIES = { Tactical = true, Explore = true }

-- Returns family, role. Role is the component's own name (Collision_Boom,
-- B_BoomA, B_Shake, ...). Either may be nil if the name will not parse,
-- and callers are expected to treat nil as "not mine".
local function classify_arm(arm)
    local full = arm_full_name(arm)
    if not full then return nil, nil end

    local family
    for _, entry in ipairs(FAMILY_PATTERNS) do
        if full:find(entry[1], 1, true) then
            family = entry[2]
            break
        end
    end

    local role = full:match("%.([A-Za-z0-9_]+)$")
    return family, role
end

local function is_gameplay_family(family)
    return family ~= nil and GAMEPLAY_FAMILIES[family] == true
end

-- Runs `callback` over every spring arm that exists now, and over every one
-- created later. `callback` returns true if it applied something, false or
-- nil if it skipped.
--
-- Deliberately NOT a polling loop: an unbounded rescan is a documented past
-- mistake in this codebase (see discovery.lua's header) and a per-frame sweep
-- of 109 components is a framerate bug waiting to be filed as a camera bug.
--
-- Two things learned from the first live run (2026-09-07), both of which the
-- first version got wrong:
--
-- 1. REPORTING. UE4SS starts mods at game startup, on the menu, where no
--    camroid exists. FindAllOf therefore returns nothing and the old
--    init-time summary printed "applied=0 skipped=0" every single time, no
--    matter what happened afterwards. It was structurally incapable of
--    reporting success. The summary is now deferred and reprinted as arms
--    actually arrive, so the log says what really happened in a mission.
--
-- 2. CONSTRUCTION ORDER, and the crash it caused. NotifyOnNewObject fires
--    when an object is constructed, possibly before the Blueprint
--    construction script writes its own values. The first attempt at
--    handling that captured the arm and wrote to it again 250ms later,
--    unconditionally. That is a use-after-free: cinematic arms are created
--    and destroyed constantly during combat (a single mission skipped 300+
--    of them), and writing to a destroyed UObject is an access violation.
--    The game took a fatal error five seconds after the last such write.
--
--    The retry is therefore OFF by default (`reapply_delay_ms = 0`), and
--    when enabled it validates the object first. The live run also showed
--    it was not needed: values landed at construction time and the applied
--    counts held steady (clip_through=3, floaty_controls=7) rather than
--    decaying, which is what an overwrite would have looked like.
--
--    Rule for anyone re-enabling this: a deferred write to a UObject must
--    check IsValid immediately before writing, every time. A pcall does not
--    save you here -- a write to freed memory takes the process down before
--    Lua can catch anything.
local function make_each_spring_arm(state)
    return function(callback, label)
        local stamped = {}
        local applied, skipped = 0, 0
        local reported = -1

        -- Deferred summary. Reprints only when the numbers actually moved, so
        -- a mission produces a couple of useful lines rather than a stream.
        local function report()
            if applied + skipped == reported then return end
            reported = applied + skipped
            state.log("info", string.format(
                "%s: applied=%d skipped=%d", tostring(label), applied, skipped))
        end

        local function run(arm, is_retry)
            local ok, did = pcall(callback, arm)
            if not ok then
                state.log("warn", tostring(label) .. ": callback failed -- " .. tostring(did))
                return
            end
            if is_retry then return end          -- retry must not double-count
            if did then applied = applied + 1 else skipped = skipped + 1 end
        end

        local function once(arm)
            local key = arm_full_name(arm)
            if key and stamped[key] then return end
            if key then stamped[key] = true end

            run(arm, false)

            -- Optional reapply after the Blueprint construction script runs.
            -- Off unless config asks for it; see the crash note above.
            local delay = tonumber(state.config.reapply_delay_ms) or 0
            if delay > 0 and type(ExecuteWithDelay) == "function" then
                pcall(ExecuteWithDelay, delay, function()
                    -- The object may have been destroyed in the meantime.
                    -- This guard is the difference between a retry and a crash.
                    local ok, alive = pcall(function() return arm:IsValid() end)
                    if ok and alive then
                        pcall(run, arm, true)
                    end
                end)
            end

            if type(ExecuteWithDelay) == "function" then
                pcall(ExecuteWithDelay, 3000, report)
            end
        end

        local ok, arms = pcall(FindAllOf, "SpringArmComponent")
        if ok and type(arms) == "table" then
            for _, arm in pairs(arms) do once(arm) end
        end

        -- Not a warning any more. At startup this is the normal, expected
        -- state: the camroids do not exist until a mission loads. Calling it
        -- a warning sent a real person hunting a bug that was not there.
        if applied + skipped == 0 then
            state.log("info", string.format(
                "%s: armed, waiting for a mission (no spring arms exist yet)",
                tostring(label)))
        else
            report()
        end

        pcall(NotifyOnNewObject, "/Script/Engine.SpringArmComponent", once)
    end
end

-- ─── shared state ───────────────────────────────────────────────────────────
-- Passed to every module so they can share data without globals.

local function create_state(config)
    local state = {
        mod_name = MOD_NAME,
        mod_dir = get_mod_dir(),
        version = VERSION,
        config = config,
        log = make_logger(config),

        -- spring arm helpers (see above)
        classify_arm = classify_arm,
        is_gameplay_family = is_gameplay_family,

        -- Populated by discovery or by fixes at runtime
        camera_manager = nil,         -- PlayerCameraManager instance
        player_controller = nil,      -- PlayerController instance
        tactical_state = nil,         -- TacticalGameState instance
        spring_arms = {},             -- discovered SpringArmComponent instances
        camera_components = {},       -- discovered CameraComponent instances
        is_player_turn = true,        -- current turn ownership
        last_good_camera_pos = nil,   -- for pitch-black recovery
        hooks_registered = {},        -- track what we've hooked
    }

    -- needs `state` itself for logging, so it is attached after construction
    state.each_spring_arm = make_each_spring_arm(state)

    return state
end

-- ─── module loader ──────────────────────────────────────────────────────────

local FIX_MODULES = {
    { name = "action_camera",   file = "fixes.action_camera"   },
    { name = "blind_spot",      file = "fixes.blind_spot"      },
    { name = "enemy_turn_lock", file = "fixes.enemy_turn"      },
    { name = "clip_through",    file = "fixes.clip_through"    },
    { name = "pitch_black",     file = "fixes.pitch_black"     },
    { name = "floaty_controls", file = "fixes.floaty_controls" },
}

local function load_fixes(state, config)
    local loaded = 0
    local failed = 0

    for _, mod in ipairs(FIX_MODULES) do
        local enabled = config.fixes[mod.name]
        if enabled then
            local require_ok, module = pcall(require, mod.file)
            if require_ok and type(module) == "table" and module.init then
                local init_ok, err = pcall(module.init, state, config)
                if init_ok then
                    loaded = loaded + 1
                    state.log("info", "Fix loaded: " .. mod.name)
                else
                    failed = failed + 1
                    state.log("error", "Fix INIT FAILED: " .. mod.name .. " — " .. tostring(err))
                end
            else
                failed = failed + 1
                state.log("error", "Fix REQUIRE FAILED: " .. mod.name .. " — " .. tostring(module))
            end
        else
            state.log("info", "Fix skipped (disabled): " .. mod.name)
        end
    end

    return loaded, failed
end

-- ─── entry point ────────────────────────────────────────────────────────────

local function main()
    local config = load_config()
    local state = create_state(config)

    state.log("info", "════════════════════════════════════════════════")
    state.log("info", MOD_NAME .. " v" .. VERSION .. " starting")
    state.log("info", "Mod directory: " .. state.mod_dir)
    state.log("info", "Discovery mode: " .. tostring(config.discovery_mode))
    state.log("info", "════════════════════════════════════════════════")

    if config.discovery_mode then
        -- Discovery mode: only run the discovery module, no fixes
        state.log("info", "Running in DISCOVERY MODE — no fixes will be applied")
        state.log("info", "Play normally: enter combat, trigger cameras, end turns, explore")

        local disc_ok, discovery = pcall(require, "discovery")
        if disc_ok and type(discovery) == "table" and discovery.init then
            local init_ok, err = pcall(discovery.init, state, config)
            if init_ok then
                state.log("info", "Discovery module initialized successfully")
            else
                state.log("error", "Discovery init failed: " .. tostring(err))
            end
        else
            state.log("error", "Could not load discovery module: " .. tostring(discovery))
        end
    else
        -- Fix mode: load and initialize all enabled fix modules
        state.log("info", "Running in FIX MODE — applying enabled camera fixes")

        local loaded, failed = load_fixes(state, config)
        state.log("info", string.format(
            "Fix loading complete: %d loaded, %d failed, %d disabled",
            loaded, failed,
            6 - loaded - failed
        ))

        if failed > 0 then
            state.log("warn", "Some fixes failed to load — check the log above for details")
        end
    end

    state.log("info", MOD_NAME .. " initialization complete")
end

-- Run with protected call so a crash in init doesn't kill UE4SS
local ok, err = pcall(main)
if not ok then
    print("[" .. MOD_NAME .. "][FATAL] Startup crashed: " .. tostring(err))
end
