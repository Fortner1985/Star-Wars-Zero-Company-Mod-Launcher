-- ZeroCam Fix #5: "Pitch-Black" UI Bug
-- Detects when the camera falls into the void (below terrain) causing the
-- entire map to disappear, and recovers by snapping the camera back to the
-- last known good position.
--
-- STATUS: STUB — requires discovery data to identify how to read/write
-- camera world position in this game's camera system.

local PitchBlack = {}

function PitchBlack.init(state, config)
    state.log("info", "pitch_black: Initializing (stub — awaiting discovery data)")

    -- TODO after discovery:
    -- 1. Determine how to read the active camera's world position
    --    (PlayerCameraManager -> GetCameraLocation, or the active
    --    CameraComponent's world transform)
    -- 2. Establish a baseline terrain Z from the level's geometry
    -- 3. Run a watchdog loop that:
    --    a. Reads current camera Z
    --    b. Compares against min_height_offset below terrain
    --    c. If below threshold, snaps camera to last_good_camera_pos
    --    d. Also checks for extreme post-process overrides (near-zero
    --       exposure) that could cause blackout without position change
    --
    -- Expected pattern:
    --   state.last_good_camera_pos = { x = 0, y = 0, z = 1000 }
    --
    --   LoopAsync(config.pitch_black_check_interval, function()
    --       local cam = FindFirstOf("PlayerCameraManager")
    --       if not cam then return false end
    --
    --       local loc = cam:GetPropertyValue("...")  -- camera location
    --       local z = loc.Z or loc.z
    --
    --       if z and z > config.pitch_black_min_height_offset then
    --           -- Camera is fine, save as last known good
    --           state.last_good_camera_pos = { x = loc.X, y = loc.Y, z = z }
    --       elseif state.last_good_camera_pos then
    --           -- Camera fell into void — recover
    --           state.log("warn", "pitch_black: Camera fell below threshold, recovering")
    --           -- Teleport camera back to last good position
    --       end
    --
    --       return false  -- keep looping
    --   end)

    state.log("info", "pitch_black: Stub loaded — no fixes applied yet")
end

return PitchBlack
