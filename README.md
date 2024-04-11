# bwf-client
![Code size](https://img.shields.io/github/languages/code-size/Kaden5480/bwf-client?color=5c85d6)
![Open issues](https://img.shields.io/github/issues/Kaden5480/bwf-client?color=d65c5c)
![License](https://img.shields.io/github/license/Kaden5480/bwf-client?color=a35cd6)

A fork of the client mod for
[Bag With Friends (BWF)](https://gitlab.com/bag-with-friends/)
, a multiplayer mod for
[Peaks of Yore](https://store.steampowered.com/app/2236070/).

# Overview
- [Installing](#installing)
    - [Windows](#windows-installation)
    - [Linux](#linux-installation)

# Installing
## Windows installation
### Prerequisites
- Install Microsoft Visual C++ 2015-2022 Redistributable from
[this link](https://aka.ms/vs/17/release/vc_redist.x64.exe)
or by running `winget install Microsoft.VCRedist.2015+.x64` in cmd/powershell/terminal.
- Install Microsoft .NET Desktop Runtime 6 from
[this link](https://download.visualstudio.microsoft.com/download/pr/d0849e66-227d-40f7-8f7b-c3f7dfe51f43/37f8a04ab7ff94db7f20d3c598dc4d74/windowsdesktop-runtime-6.0.29-win-x64.exe)
or by running `winget install Microsoft.DotNet.DesktopRuntime.6` in cmd/powershell/terminal.

### MelonLoader
- Download the latest nightly MelonLoader build
[here](https://nightly.link/LavaGang/MelonLoader/workflows/build/alpha-development/MelonLoader.Windows.x64.CI.Release.zip).
- Extract the contents of the downloaded zip file into your game directory, for example:<br>
  `C:\Program Files (x86)\Steam\steamapps\common\Peaks of Yore`
- Run Peaks of Yore and then quit the game.
- If MelonLoader was installed correctly, you should notice new directories
  in your game directory (such as Mods).

### BWF
- Download the latest BWF release [here](https://github.com/Kaden5480/bwf-client/releases).
- Extract the contents of the downloaded zip file into the Mods directory which MelonLoader created.
- Run Peaks of Yore and BWF should be ready to go.

## Linux installation
### System prerequisites
- Install [protontricks](https://pkgs.org/download/protontricks).

### Prefix configuration
- Open protontricks.
- Select "Peaks of Yore".
- Select "Select the default wineprefix" and press "OK".
- Select "Run winecfg" and press "OK".
- Change "Windows Version" to "Windows 10" and press "Apply".
- Switch to the "Libraries" tab.
- Where it says "New override for library:", choose "version", press "Add", then press "OK".

### Installing prefix components
- Open protontricks.
- Select "Peaks of Yore".
- Select "Select the default wineprefix" and press "OK".
- Select "Install Windows DLL or component" and press "OK".
- Select the packages "dotnetdesktop5" and "vcrun2019" and press "OK".
- You may get errors that say checksums didn't match, you can ignore these. When
  you are asked to "Continue anyway", choose "Yes".

### MelonLoader
- Download the latest nightly MelonLoader build
[here](https://nightly.link/LavaGang/MelonLoader/workflows/build/alpha-development/MelonLoader.Windows.x64.CI.Release).
- Extract the contents of the downloaded zip file into your game directory, for example:<br>
  `~/.local/share/Steam/steamapps/common/Peaks of Yore/`
- Run Peaks of Yore and then quit the game.
- If MelonLoader was installed correctly, you should notice new directories
  in your game directory (such as Mods).

### BWF
- Download the latest BWF release
[here](https://github.com/Kaden5480/bwf-client/releases).
- Extract the contents of the downloaded zip file into the Mods directory which MelonLoader created.
- Run Peaks of Yore and BWF should be ready to go.
