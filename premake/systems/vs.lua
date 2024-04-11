local function run()
    local nuget_libs = {}

    for name, version in pairs(libs) do
        table.insert(nuget_libs, name .. ":" .. version)
    end

    nuget(nuget_libs)
    links(link_with)
end

return {
    run = run,
}
