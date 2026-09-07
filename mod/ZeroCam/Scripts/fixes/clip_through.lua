-- ZeroCam Fix #4: Terrain Clip-Through
-- Forces collision probing on the spring arms that actually position the
-- tactical and exploration cameras, so panning stops pushing the view
-- through walls and terrain.
--
-- STATUS: IMPLEMENTED (from UE4SS_ObjectDump, 2026-09-07)
--
-- The dump settled the open question in the v1 stub: the game DOES use
-- /Script/Engine.SpringArmComponent -- 109 live instances -- so the
-- "write our own position correction" fallback branch is dead code and has
-- been removed. Real property names, straight off the class:
--
--     bDoCollisionTest : BoolProperty
--     ProbeSize        : FloatProperty
--     ProbeChannel     : ByteProperty
--     TargetArmLength  : FloatProperty
--
-- Scope matters here. Every camroid owns several arms, and only some of
-- them belong to us:
--
--     Collision_Boom   the arm whose entire job is collision  -> ours
--     B_BoomA/B_BoomB  the main positioning booms             -> ours
--     B_Aim/B_Shake/B_AnimOffset/B_UserOffset  pure offsets   -> leave
--     CinematicFollowCamAttach  per-character cine attach     -> leave
--
-- And only on the gameplay camroid families (Tactical_Pro_*, Explore_Pro_*).
-- The Ability_* and Cinematic_Cloud_* rigs are scripted shots that pass
-- through geometry deliberately; forcing collision on those would jerk
-- every ability camera off its intended framing. That is a regression, not
-- a fix, so they are excluded by family.

local ClipThrough = {}

local ARMS = { Collision_Boom = true, B_BoomA = true, B_BoomB = true }

function ClipThrough.init(state, config)
    local cfg = config.clip_through or {}
    local probe_size = cfg.probe_size or 24.0
    local probe_channel = cfg.probe_channel   -- nil = leave the game's choice alone

    local applied, skipped = 0, 0

    local function apply(arm)
        local family, role = state.classify_arm(arm)
        if not state.is_gameplay_family(family) or not ARMS[role] then
            skipped = skipped + 1
            return
        end

        local ok, err = pcall(function()
            arm.bDoCollisionTest = true
            arm.ProbeSize = probe_size
            if probe_channel ~= nil then
                arm.ProbeChannel = probe_channel
            end
        end)

        if ok then
            applied = applied + 1
            state.log("debug", string.format(
                "clip_through: %s.%s probe=%.1f", tostring(family), tostring(role), probe_size))
        else
            state.log("warn", "clip_through: write failed on " ..
                tostring(family) .. "." .. tostring(role) .. " -- " .. tostring(err))
        end
    end

    state.each_spring_arm(apply)

    state.log("info", string.format(
        "clip_through: applied=%d skipped=%d probe_size=%.1f channel=%s",
        applied, skipped, probe_size, tostring(probe_channel)))
end

return ClipThrough
