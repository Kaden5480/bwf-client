{
    game-dir ? "$HOME/.local/share/Steam/steamapps/common/Peaks of Yore",
    build-system ? "gmake",
    config ? "release",
} : let
    pkgs = import <nixpkgs> {};
in
    pkgs.mkShell {
        buildInputs = with pkgs; [
            gnumake
            mono
            premake5
        ];

        shellHook = ''
            if [ "${game-dir}" ]; then
                premake5 "${build-system}" --game-dir="${game-dir}"
            else
                premake5 "${build-system}"
            fi

            printf "${build-system}\n"

            if [ "${build-system}" = "gmake" ]; then
                make config=${config}
            fi

            exit
        '';
    }
