# Greathorn.CLI

## Assumptions

Your depots layout needs to follow Epic's standard layout, as the tools are expecting a similar thing. The bootstrap executables can go anywhere but in our case we use `\\Greathorn\Binaries\Bootstrap\`, this then allows the downloaded source code to end up in `\\Greathorn\Source\Programs\Greathorn.CLI\`, and then the built tools end up in `\\Greathorn\Binaries\DotNet`.

## Reequirements

### Git
Git needs to be accessible from your command prompt; some Git clients do not add Git to the `PATH` so it may be easier  to just use the installers found at: https://git-scm.com/download/.

### .NET SDK 8.0
The bootstrapped build requires the installation of the .NET 8.0 SDK, which can be found at: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
