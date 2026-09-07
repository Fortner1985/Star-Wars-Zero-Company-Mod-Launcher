-- ZeroCam Fix #3: Enemy-Turn Camera Lock
-- Frees the camera during the enemy turn so the player isn't stuck staring
-- at a wall while opponents move.
--
-- Supports two modes (set in config.lua):
--   "free" — re-enables player camera input during enemy turn
--   "auto" — auto-resets camera to overhead view at each enemy action
--
-- STATUS: STUB — requires discovery data to identify turn transition hooks
-- and input enable/disable mechanisms.

local EnemyTurn = {}

function EnemyTurn.init(state, config)
    state.log("info", "enemy_turn: Initializing (stub — awaiting discovery data)")
    state.log("info", "enemy_turn: Mode = " .. tostring(config.enemy_turn_camera_mode))

    -- TODO after discovery:
    -- 1. From discovery_log.txt, identify:
    --    - The turn change function (TacticalGameState turn transition)
    --    - The function that disables player input during enemy turn
    --    - The camera reset/position function
    -- 2. Hook the turn transition:
    --    - Detect when it becomes the enemy's turn
    --    - In "free" mode: re-enable camera input (hook DisableInput, block it
    --      for camera controls only, or re-call EnableInput after the game
    --      disables it)
    --    - In "auto" mode: at each enemy action, reset camera to a top-down
    --      position centered on the action
    --
    -- Expected pattern for "free" mode:
    --   RegisterHook("<TurnChangeFunction>", function(context)
    --       state.is_player_turn = false
    --   end, function(context)
    --       -- Post-hook: if enemy turn, re-enable camera input
    --       if not state.is_player_turn then
    --           local pc = FindFirstOf("PlayerController")
    --           if pc then pc:EnableInput(pc) end
    --       end
    --   end)

    state.log("info", "enemy_turn: Stub loaded — no fixes applied yet")
end

return EnemyTurn
