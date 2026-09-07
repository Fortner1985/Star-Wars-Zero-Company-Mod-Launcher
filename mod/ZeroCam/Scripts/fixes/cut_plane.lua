-- ZeroCam Fix #7: Object-Hiding Cut Plane Opacity
--
-- The blue plane that appears when the tactical camera drops below a floor is
-- BP_ObjectHidingCutPlane_C -- a deliberate part of the game's object-hiding
-- system, not a camera bug and not something ZeroCam broke. This fix makes its
-- opacity tunable.
--
-- STATUS: STAGE 1 -- RECONNAISSANCE ONLY. Writes nothing.
--
-- Why not just set it. The object dump lists property *names*, never their
-- *values*. It shows that BitReactorObjectHidingSettings has:
--
--     ApplyCutPlaneParameterName   NameProperty
--     MaterialDataAsset            SoftObjectProperty
--     DepthStencilBelowPlaneMaterialAsset / ...AbovePlaneMaterialAsset
--     OpacityFadeCurveLength / DefaultOpaqueTransitionTime / EdgeFadeCurveLength
--     DefaultCutHeightOffset / DefaultCutPlane / DefaultCutPlaneVector
--
-- but not what ApplyCutPlaneParameterName actually contains, and not which
-- material carries the opacity scalar. Guessing a parameter name would produce
-- a write that silently does nothing while the log claims success. This mod has
-- already done that once (the applied=0 reporting bug) and it wasted a real
-- person's time, so: read the live values, print them, then write stage 2
-- against facts.
--
-- Stage 2, once the log tells us the parameter name, will set the scalar on a
-- dynamic material instance and expose `cut_plane_opacity` in config.lua.
-- 1.0 = fully solid, 0.0 = invisible. Lower is more see-through.

local CutPlane = {}

local SETTINGS = "BitReactorObjectHidingSettings"

-- Read-only. Every access is wrapped: a settings singleton may not exist until
-- a mission is loaded, and a missing property must never take the game down.
local function peek(object, name)
    local ok, value = pcall(function() return object:GetPropertyValue(name) end)
    if not ok then return "<error>" end
    if value == nil then return "<nil>" end
    local ok2, text = pcall(tostring, value)
    return ok2 and text or "<untostringable>"
end

local FIELDS = {
    "ApplyCutPlaneParameterName",
    "IntroCurveLengthParameterName",
    "MaterialDataAsset",
    "DepthStencilBelowPlaneMaterialAsset",
    "DepthStencilAbovePlaneMaterialAsset",
    "DepthStencilClearMaterialAsset",
    "TranslucentFlashMaterialAsset",
    "EndCapMaterialAsset",
    "OpacityFadeCurveLength",
    "DefaultOpaqueTransitionTime",
    "EdgeFadeCurveLength",
    "IntroCurveLength",
    "OutroCurveLength",
    "DefaultCutHeightOffset",
}

function CutPlane.init(state, config)
    local reported = false

    local function report()
        if reported then return end

        local ok, settings = pcall(FindFirstOf, SETTINGS)
        if not ok or settings == nil then return end   -- no mission yet
        reported = true

        state.log("info", "cut_plane: === live " .. SETTINGS .. " ===")
        for _, field in ipairs(FIELDS) do
            state.log("info", "cut_plane:   " .. field .. " = " .. peek(settings, field))
        end

        -- The plane actors themselves. Their components are where a dynamic
        -- material instance would have to be created in stage 2.
        local ok2, planes = pcall(FindAllOf, "BP_ObjectHidingCutPlane_C")
        if ok2 and type(planes) == "table" then
            local count = 0
            for _ in pairs(planes) do count = count + 1 end
            state.log("info", "cut_plane:   live cut plane actors = " .. tostring(count))
        else
            state.log("info", "cut_plane:   live cut plane actors = none yet")
        end

        state.log("info", "cut_plane: === end === (stage 1: nothing was written)")
    end

    -- The settings singleton does not exist on the menu. Rather than poll,
    -- piggyback on spring arms: they arrive exactly when a mission spins up,
    -- which is also when this object becomes available. Costs nothing extra
    -- and adds no timer of its own.
    state.each_spring_arm(function()
        if not reported and type(ExecuteWithDelay) == "function" then
            pcall(ExecuteWithDelay, 4000, report)
        end
        return false          -- never counts as applied; this fix writes nothing
    end, "cut_plane_probe")

    state.log("info", "cut_plane: stage 1 (read-only) armed")
end

return CutPlane
