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
- [Building from source](#building-from-source)
    - [Windows](#windows-build)
    - [Linux](#linux-build)
    - [Nix](#linux-build-with-nix)

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
### Prerequisites
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
[here](https://nightly.link/LavaGang/MelonLoader/workflows/build/alpha-development/MelonLoader.Windows.x64.CI.Release.zip).
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

# Building from source
## Windows build
### Prerequisites
- Install Git (standalone installer) from
[this link](https://git-scm.com/download/win)
or by running `winget install Git.Git` im cmd/powershell/terminal.
- Install premake5 from
[this link](https://premake.github.io/download)
or by running `winget install Premake.Premake.5.Beta` in cmd/powershell/terminal.
- Install Visual Studio 2022 from
[this link](https://visualstudio.microsoft.com/vs/)
or by running `winget install Microsoft.VisualStudio.2022.Community` in cmd/powershell/terminal.

### Configuration
Clone this repository.
```sh
git clone https://github.com/Kaden5480/bwf-client.git
```
Enter the newly created directory and generate the project files for Visual Studio.
```sh
cd bwf-client/
premake5 vs2022
```
You may also specify the location of your game files.
```sh
premake5 vs2022 --game-dir="E:\Alternate\Path\To\Peaks of Yore"
```

### Building with Visual Studio
After following the configuration step, you should now have
a BagWithFriends.csproj and BagWithFriends.sln file.

- Open the BagWithFriends.sln file in Visual Studio.
- If you are missing components from Visual Studio, you will see a box which says
  "Based on your solution, you might need to install extra components...".
  If this is the case, press the "Install" button and proceed
  through the installation steps.
- Along the top of Visual Studio you will notice a dropdown box which will say either "Debug" or "Release",
  if you don't plan on developing the mod, you can choose the "Release" option.
- Select Build -> Build BagWithFriends (or press ctrl+b).
- The code should be compiled and you will be able to find the output in `bin/release/`
  or `bin/debug`, depending on which config you selected.

## Linux build
### Prerequisites
- Install [git](https://pkgs.org/download/git)
- Install [premake5](https://pkgs.org/download/premake5)
- Install [make](https://pkgs.org/download/make)
- Install [mono](https://pkgs.org/download/mono)

### Configuration
Clone this repository.
```sh
git clone https://github.com/Kaden5480/bwf-client.git
```

Enter the newly created directory and generate the project files for Make.
This step also downloads necessary libraries from
[NuGet](https://www.nuget.org/).
```sh
cd bwf-client/
premake5 gmake
```

You may also specify the location of your game files.
```sh
premake5 gmake --game-dir="/alternate/path/to/Peaks of Yore/"
```

### Building with Make
After following the configuration step, you should now have
a Makefile and BagWithFriends.make file.

Build the release version of the mod.
```sh
make config=release
```

Or, if you would like the debug version, you can run either of these commands.
```sh
make
make config=debug
```

The code should be compiled and you will be able to find the output in `bin/release/`
or `bin/debug`, depending on which config you selected.

## Linux build with Nix
### Prerequisites
- Install [git](https://pkgs.org/download/git)
- Install [nix](https://pkgs.org/download/nix)

### Configuration
Clone this repository.
```sh
git clone https://github.com/Kaden5480/bwf-client.git
```

Enter the newly created directory and build the release version of the mod.
Necessary libraries will be downloaded from
[NuGet](https://www.nuget.org/).
```sh
cd bwf-client/
nix-shell
```

You can also specify a custom game directory.
```sh
nix-shell --argstr game-dir "/alternate/path/to/Peaks of Yore/"
```

You can also select the debug build.
```sh
nix-shell --argstr config debug
```

Or a mix of both
```sh
nix-shell --argstr config debug \
    --argstr game-dir "/aternate/path/to/Peaks of Yore"
```

The code should be compiled and you will be able to find the output in `bin/release/`
or `bin/debug`, depending on which config you selected.
