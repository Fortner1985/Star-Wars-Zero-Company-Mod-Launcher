-- ═══════════════════════════════════════════════════════════════════════════
-- ZeroSuite Bridge
--
-- Runs UE4SS's own dumpers on request, so the launcher can trigger them
-- instead of relying on a keybind.
--
-- Why this exists: Ctrl+J produced nothing. UE4SS's console and GUI are both
-- disabled in this install (ConsoleEnabled = 0, GuiConsoleEnabled = 0), and
-- the log shows no ObjectDumper activity, so the keystroke never arrived.
-- Rather than guess at focus and modifier handling, the launcher writes a
-- one-word request file and this mod performs the dump.
--
-- It polls rather than acting at startup on purpose: the dumpers only record
-- what is currently loaded, so a menu-time dump misses every combat class.
-- Polling lets the request be made while a mission is running.
-- ═══════════════════════════════════════════════════════════════════════════

local POLL_MS = 3000

-- Scripts/ -> ZeroSuiteBridge/ -> Mods/ -> ue4ss/
local function ue4ss_dir()
    local source = debug.getinfo(1, "S").source:gsub("^@", "")
    local scripts = source:match("(.+)[/\\][^/\\]+$")
    local mod = scripts and scripts:match("(.+)[/\\][^/\\]+$")
    local mods = mod and mod:match("(.+)[/\\][^/\\]+$")
    return mods and mods:match("(.+)[/\\][^/\\]+$")
end

local root = ue4ss_dir()
if root == nil then
    print("[ZeroSuiteBridge] could not resolve ue4ss directory\n")
    return
end

local request_path = root .. "\\zerosuite-request.txt"
local result_path  = root .. "\\zerosuite-result.txt"

local function write_result(text)
    local file = io.open(result_path, "w")
    if file then file:write(text .. "\n"); file:close() end
    print("[ZeroSuiteBridge] " .. text .. "\n")
end

local function read_request()
    local file = io.open(request_path, "r")
    if not file then return nil end
    local body = file:read("*a") or ""
    file:close()
    os.remove(request_path)
    return body:gsub("%s+", ""):lower()
end

-- Each dumper is called through pcall and its absence reported rather than
-- assumed: these globals are provided by UE4SS and have moved between builds.
local ACTIONS = {
    objects = { name = "DumpAllObjects", fn = function() return DumpAllObjects() end },
    usmap   = { name = "DumpUSMAP",      fn = function() return DumpUSMAP() end },
    sdk     = { name = "GenerateSDK",    fn = function() return GenerateSDK() end },
    actors  = { name = "DumpAllActors",  fn = function() return DumpAllActors() end },
}

local function run(command)
    local action = ACTIONS[command]
    if action == nil then
        write_result("unknown request '" .. tostring(command) .. "'")
        return
    end
    if _G[action.name] == nil then
        write_result("FAILED " .. command .. ": " .. action.name .. " is not available in this UE4SS build")
        return
    end
    write_result("running " .. action.name .. " -- the game will freeze until it finishes")
    local ok, err = pcall(action.fn)
    if ok then
        write_result("DONE " .. command .. " via " .. action.name)
    else
        write_result("FAILED " .. command .. ": " .. tostring(err))
    end
end

print("[ZeroSuiteBridge] watching " .. request_path .. "\n")
write_result("ready")

LoopAsync(POLL_MS, function()
    local command = read_request()
    if command ~= nil and command ~= "" then
        pcall(run, command)
    end
    return false  -- never stop
end)
