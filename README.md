# Greathorn.CLI

## Assumptions

- Your depot's layout needs to follow Epic's standard layout where as the Unreal Engine source code is at the root of the depot (for example `\\<DEPOT>\Engine`).
- You will need to setup your own P4 ignores around the specific paths that are introduced by our tooling. 
  - For example, `\Greathorn\Source\Programs\Greathorn.CLI\` and `\Greathorn\Binaries\DotNet\` are ignored in our depot.

## Requirements

### Git
Git needs to be accessible from your command prompt; some Git clients do not add Git to the `PATH` so it may be easier  to just use the installers found at: https://git-scm.com/download/.

### .NET SDK 8.0
The bootstrapped build requires the installation of the .NET 8.0 SDK, which can be found at: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

## Programs

### Bootstrap

A minimal application used to get our CLI tooling in place. The location of its executables doesn't have an impact; however we keep ours in `\Greathorn\Binaries\Bootstrap\`. 

> The workspace root is is searched upward from its location, looking for key UE files `Setup.bat` and `GenerateProjectFiles.bat`.

- Clones and **overwrites** the source from https://github.com/GreathornGames/Greathorn.CLI into `\Greathorn\Source\Programs\Greathorn.CLI\`.
- All projects are built in `Release` configuration and output to `\Greathorn\Binaries\DotNet\`.

#### Arguments

The application operates normally without any provided arguments, however you can customize some of the behaviour with the following:

| Argument | Description |
| :-- | :-- |
| `no-build` | Do not build projects. |
| `no-workspace` | Do not run `WorkspaceSetup` after building projects. |
| `quiet` | Run in quiet mode, not asking for user input. |

### GG

### ShellSetup

### WorkspaceSetup
