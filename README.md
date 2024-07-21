# Socially Distant
Socially Distant is an up-coming hacking game following the story of a global ransomware attack disrupting a society forced into cyberspace by the spread of a deadly biological threat to humanity.

## Ritchie's Toolbox
This game is made using Ritchie's Toolbox, a defacto custom game engine built on top of MonoGame. We do not directly use the MonoGame Content Pipeline, instead we use AutoPipeline and some custom MSBuild targets to build the game's content. Most of the time, you shouldn't notice - but if you are adding content to the game, you should never directly interact with or commit the `Content.mgcb` file as it'll be wiped and overwritten by AutoPipeline.

## Building from source
Building Socially Distant from source is moderately simple.

### Pre-requisites
On all platforms, you should have the .NET 8 SDK and Steam installed. You should also install the `dotnet-mgcb` .NET tool globally.

> :warning: **Shaders on non-Windows platforms**
>
> If you aren't building the game on Windows, you **will** run into build errors during shader compilation. This is because MonoGame currently uses the Direct3D shader compiler for initial shader compilation before transpiling to the target platform. Linux users will need to install Wine and run the `mgfxc-wine-setup.sh` script in the root of the repository, this will set up a Wine prefix with the necessary setup for shader compilation. For macOS users running Apple Silicon, you're on your own.

### How to build
1. Clone this repo, or fork and clone that.
2. Open the solution file at `src/sociallydistant.sln` in your .NET IDE
3. Build and run the `SociallyDistant` project.

Depending on your IDE/setup, you may need to run:

```bash
dotnet tool restore
```

in the repo root, and

```bash
dotnet restore
```

in the `src` directory. The first command installs any missing local .NET tools, and the second restores NuGet packages.

## We're accepting contributions!
Feel free to submit merge requests to the game. If merged, they will be shipped in the next Steam release of Socially Distant. For more info, see the `CONTRIBUTING.md` and `LICENSE` files in the repo root.

> :asterisk: **Note
>
>By contributing to the game, you agree to the Developer Certificate of Origin. You acknowledge that you own or otherwise have the permission to submit your contribution under the same license as the game's source code. To acknowledge the DCO, all commits must be signed off.

## Project structure

### Source code
All the important code lives under `src/`. In there, you will find:

 - `SociallyDistant`: This is the main game application.
 - `SociallyDistant.Framework`: This is the Socially Distant runtime, a library that the game itself and all mods must reference.
 - `AcidicGUI`: This is the game's UI system, and the small part of Ritchie's Toolbox that actually exists despite not properly being named. This is subject to be moved to its own repo in the future.

### Third-party stuff
Some MonoGame extensions, such as IMGUI, are compiled from source as part of the game build. These are all found under the `vendor` folder.

### Game assets
All game assets are (and must be) placed in `src/SociallyDistant/Content`. This directory is managed by AutoPipeline and some custom MSBuild tasks.
