local gmake = require("./premake/systems/gmake")
local vs = require("./premake/systems/vs")

workspace "BagWithFriends"
    configurations {
        "Debug",
        "Release",
    }

project "BagWithFriends"
    kind            "SharedLib"
    language        "C#"
    dotnetframework "4.7.2"

    -- Add the --game-dir command line option
    newoption {
        trigger     = "game-dir",
        value       = "DIR",
        description = "The path to the Peaks of Yore game files",
        default     = "C:/Program Files (x86)/Steam/steamapps/common/Peaks of Yore",
    }

    local managed_dir = _OPTIONS["game-dir"] .. "/Peaks of Yore_Data/Managed"

    -- Libraries this project depends on
    libs = {
        ["LavaGang.MelonLoader"] = "0.6.1",
        ["Lib.Harmony"]          = "2.3.3",
        ["WebSocketSharp"]       = "1.0.3-rc11",
    }

    link_with = {
        managed_dir .. "/Assembly-CSharp.dll",
        managed_dir .. "/Assembly-CSharp-firstpass.dll",
        managed_dir .. "/com.rlabrecque.steamworks.net.dll",
        managed_dir .. "/GalaxyCSharp.dll",
        managed_dir .. "/UnityEngine.dll",
        managed_dir .. "/UnityEngine.AssetBundleModule.dll",
        managed_dir .. "/UnityEngine.CoreModule.dll",
        managed_dir .. "/UnityEngine.ImageConversionModule.dll",
        managed_dir .. "/UnityEngine.IMGUIModule.dll",
        managed_dir .. "/UnityEngine.InputLegacyModule.dll",
        managed_dir .. "/UnityEngine.PhysicsModule.dll",
        managed_dir .. "/UnityEngine.TextRenderingModule.dll",
        managed_dir .. "/UnityEngine.UI.dll",
        managed_dir .. "/UnityEngine.UIElementsModule.dll",
        managed_dir .. "/UnityEngine.UIModule.dll",
        "System.dll",
        "System.Drawing.dll",
        "System.Management.dll",
        "System.Web.dll",
    }

    files {
        "src/**.cs",
    }

    -- TODO: Handle unsupported build systems (maybe?)
    if _ACTION == "gmake" then
        gmake.run()
    else
        vs.run()
    end

    filter { "configurations:Debug" }
        targetdir "bin/debug"
        symbols   "On"


    filter { "configurations:Release" }
        targetdir "bin/release"
        optimize  "On"
