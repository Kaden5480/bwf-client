local utils = require("./premake/utils")

local function run()
    local extra_libs = {
        ["NETStandard.Library"] = "2.0.3",
        ["System.Memory"]       = "4.5.5",
        ["System.Text.Json"]    = "8.0.1",
    }

    for name, version in pairs(extra_libs) do
        libs[name] = version
    end

    utils.download_libs(libs)

    local extra_links = {
        utils.lib_dir .. "/LavaGang.MelonLoader/lib/net35/MelonLoader.dll",
        utils.lib_dir .. "/Lib.Harmony/lib/net48/0Harmony.dll",
        utils.lib_dir .. "/NETStandard.Library/build/netstandard2.0/ref/mscorlib.dll",
        utils.lib_dir .. "/NETStandard.Library/build/netstandard2.0/ref/netstandard.dll",
        utils.lib_dir .. "/System.Memory/lib/net461/System.Memory.dll",
        utils.lib_dir .. "/System.Text.Json/lib/net462/System.Text.Json.dll",
        utils.lib_dir .. "/WebSocketSharp/lib/websocket-sharp.dll",
    }

    for _, link in pairs(extra_links) do
        table.insert(link_with, link)
    end

    links(link_with)
end

return {
    run = run,
}
