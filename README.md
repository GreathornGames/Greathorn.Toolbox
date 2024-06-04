# Greathorn.Toolbox

## Assumptions

- Your depot's layout needs to follow Epic's standard layout where as the Unreal Engine source code is at the root of the depot (for example `\\<DEPOT>\Engine`).
- You will need to setup your own P4 ignores around the specific paths that are introduced by our tooling. 
  - For example, `\Greathorn\Source\Programs\Greathorn.Toolbox\` and `\Greathorn\Binaries\DotNet\` are ignored in our depot.

## Requirements

### Git
Git needs to be accessible from your command prompt; some Git clients do not add Git to the `PATH` so it may be easier  to just use the installers found at: https://git-scm.com/download/.

### .NET SDK 8.0
The bootstrapped build requires the installation of the .NET 8.0 SDK, which can be found at: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

## Programs

### Bootstrap

A minimal application used to get our CLI tooling in place. The location of its executables doesn't have an impact; however we keep ours in `\Greathorn\Binaries\Bootstrap\`.

> The workspace root is is searched upward from its location, looking for key UE files `Setup.bat` and `GenerateProjectFiles.bat`.

- Clones and **overwrites** the source from https://github.com/GreathornGames/Greathorn.Toolbox into `\Greathorn\Source\Programs\Greathorn.Toolbox\`.
- All projects are built in `Release` configuration and output to `\Greathorn\Binaries\DotNet\`.

#### Arguments

The application operates normally without any provided arguments, however you can customize some of the behaviour with the following:

| Argument | Description |
| :-- | :-- |
| `no-build` | Do not build projects. |
| `no-clone` | Do not clone/update the downloaded source code. |
| `no-workspace` | Do not run `WorkspaceSetup` after building projects. |
| `no-pause` | Do not pause and wait for any key on exit. |
| `quiet` | Run in quiet mode, not asking for user input, etc. |

### CleanSlate

A tool for resetting the state of specific areas of the project.

#### Arguments

Any number of arguments can be added as part of the instructions of what to clean.

| Argument | Description |
| :-- | :-- |
| `project` | Delete project `Intermediate` and `Binaries` folders. |
| `project-plugins` | Delete the `Intermediate` and `Binaries` folders of all plugins in projects. |

### GG

A command synthesizer application which searches for `*.gg.json` files in `\Greathorn\Programs\GG` and recursively under `\Projects\` to build an action list.

The launched commands are ran non-elevated with numerous instance environment variables set.

### KeepAlive

An application designed to keep a launched application running, much like a service. Useful for game server clients, but more specifically in our case, Horde Agents.

By keeping the running application in user space, it has access to graphical resources and is considered *interactive* by Horde, and therefore can be used for Gauntlet automated testing.

The default configuration will launch the locally installed Horde Agent; it is important to disable the service version (the tray application is also not needed). A path can be provided by argument to a json configuration file which implements `KeepAliveConfig`.

### WorkspaceSetup

This application sets up a users workspace just how we want it. 

- Enviornment path
- P4Config / P4V custom tools
- Updates and builds Greathorn.Toolbox
- Execution flags on unix systems
- Add security exclusions for the workspace folder
- UE's prerequisites and version selector

> This application must be ran at an elevated level; a UAC request will be triggered by the bundled manifest in the executable.

#### Arguments

The application operates normally without any provided arguments, however you can customize some of the behaviour with the following:

| Argument | Description |
| :-- | :-- |
| `no-build` | Skip the build check. |
| `no-source` | Skip the source code update check. |
| `no-pause` | Do not pause and wait for any key on exit. |
| `quiet` | Run in quiet mode, not asking for user input, etc. |

## Development

> It is important to keep your IDE building in `DEBUG` mode when actively developing as both `Bootstrap` and `Workspace` have destructive actions which will wipe out any changes to the source code if ran in `RELEASE` or `WORKSPACE` mode.