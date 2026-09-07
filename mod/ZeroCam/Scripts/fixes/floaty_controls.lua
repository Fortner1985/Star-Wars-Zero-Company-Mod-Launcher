-- ZeroCam Fix #6: Floaty Third-Person Controls
-- Cuts the interpolation lag on the exploration camera's spring arms so the
-- view tracks input instead of drifting after it.
--
-- STATUS: IMPLEMENTED (from UE4SS_ObjectDump, 2026-09-07)
--
-- The v1 stub hedged on whether exploration used a SpringArmComponent at
-- all. It does. BR_Camroid_Explore_Pro_B_C owns six of them, and the lag
-- properties the stub guessed at are real, spelled exactly as guessed:
--
--     bEnableCameraLag         : BoolProperty
--     bEnableCameraRotationLag : BoolProperty
--     CameraLagSpeed           : FloatProperty
--     CameraRotationLagSpeed   : FloatProperty
--     CameraLagMaxDistance     : FloatProperty
--
-- Scope is narrower than fix #4. Floatiness is a feel complaint about the
-- camera the player is steering, so this touches the Explore_Pro_* family
-- only -- the tactical camera runs on rails and its smoothing is intended.
-- Within that family it skips B_Shake and B_AnimOffset: those arms exist to
-- carry deliberate motion, and de-lagging them makes handheld sway stutter
-- rather than making anything more responsive.
--
-- Two modes, per config:
--   exploration_disable_lag = true   lag off entirely, most responsive
--   exploration_disable_lag = false  lag kept, speeds raised (snappier)
--
-- Lag speed is inverse-feel: HIGHER is snappier. The config defaults of
-- 20.0/15.0 are well above a typical UE default of ~10, which is the
-- intended direction. Values are not clamped here -- the game may reject
-- absurd ones on its own, and second-guessing the user's tuning is not
-- this module's job.

local FloatyControls = {}

local SKIP = { B_Shake = true, B_AnimOffset = true }

function FloatyControls.init(state, config)
    local disable    = config.exploration_disable_lag
    local lag_speed  = config.exploration_lag_speed or 20.0
    local rot_speed  = config.exploration_rotation_lag_speed or 15.0
    local max_dist   = config.exploration_lag_max_distance or 50.0

    local applied, skipped = 0, 0

    local function apply(arm)
        local family, role = state.classify_arm(arm)
        if family ~= "Explore" or SKIP[role] then
            skipped = skipped + 1
            return
        end

        local ok, err = pcall(function()
            if disable then
                arm.bEnableCameraLag = false
                arm.bEnableCameraRotationLag = false
            else
                arm.bEnableCameraLag = true
                arm.bEnableCameraRotationLag = true
                arm.CameraLagSpeed = lag_speed
                arm.CameraRotationLagSpeed = rot_speed
            end
            arm.CameraLagMaxDistance = max_dist
        end)

        if ok then
            applied = applied + 1
            state.log("debug", "floaty_controls: Explore." .. tostring(role))
        else
            state.log("warn", "floaty_controls: write failed on Explore." ..
                tostring(role) .. " -- " .. tostring(err))
        end
    end

    state.each_spring_arm(apply)

    state.log("info", string.format(
        "floaty_controls: applied=%d skipped=%d mode=%s lag=%.1f/%.1f",
        applied, skipped, disable and "lag_off" or "snappier", lag_speed, rot_speed))
end

return FloatyControls
