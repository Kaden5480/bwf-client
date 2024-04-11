-- Where downloaded libraries should be stored
local lib_dir = "libs"

-- NuGet's API endpoint for downloading packages
local nuget_api = "https://www.nuget.org/api/v2/package/"

-- Displays the current progress of downloads
local function progress(url, total, current)
    local ratio = current / total
    ratio = math.min(math.max(ratio, 0), 1);
    local percent = math.floor(ratio * 100);
    io.write("\r" .. url .. " (" .. percent .. "%/100%)")
end

-- Downloads libraries using NuGet's API if required
local function download_libs(libs)
    os.mkdir(lib_dir)

    for name, version in pairs(libs) do
        local url = nuget_api .. name .. "/" .. version
        local dir_path = lib_dir .. "/" .. name
        local zip_path = dir_path .. ".zip"

        -- Skip if already downloaded
        if os.isfile(zip_path) == true then
            goto continue
        end

        -- Otherwise, download the library
        local result, status = http.download(url, zip_path, {
            progress = function(total, current)
                progress(url, total, current)
            end
        })

        print()

        if result ~= "OK" then
            print("Failed downloading: " .. url
                .. " reason: (" .. status .. ")" .. result)
            os.exit(1)
        end

        -- Extract the library
        print("Extracting " .. zip_path .. "...")
        zip.extract(zip_path, dir_path)

        ::continue::
    end
end

return {
    lib_dir = lib_dir,
    download_libs = download_libs,
}
