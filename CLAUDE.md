# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

IISAppCmd is a .NET Framework console application that modifies an IIS configuration file. It copies the bundled `Resources/applicationHost.config` to a working location and writes an application pool, the site that runs in it and, when asked for one, a native global module with the custom section of the IIS agent into the copy through `Microsoft.Web.Administration`, using the same command-line syntax as the `iisexpressstarter` reference tool.

## Build

Requires .NET Framework 4.8.1 and either the .NET SDK or MSBuild (Visual Studio 2022 or Build Tools).

```bash
dotnet build IISAppCmd.slnx
```

Or `msbuild IISAppCmd.slnx`, or open `IISAppCmd.slnx` in Visual Studio.

Build a specific configuration:

```bash
dotnet build IISAppCmd.slnx -c Release
```

Output goes to `IISAppCmd/bin/Debug/` or `IISAppCmd/bin/Release/`.

## Test

```bash
dotnet test IISAppCmd/test/IISAppCmd.Tests.csproj
```

The builder tests write through `Microsoft.Web.Administration` into a private copy of the bundled config and check the resulting XML; they need IIS's configuration system on the machine but never start IIS Express.

## Run

```bash
IISAppCmd\bin\Debug\IISAppCmd.exe -ap <json> [-gm <json>] [-cc <json>] [-b <32|64>] [-t <tfm>] [-p <port>] [-c <path>]
```

`-ap` is the only required option; it carries the application inline as `{"name": "...", "path": "..."}`, the same shape `iisexpressstarter` takes, and its path is served from the root of the site. `-gm` carries a native module the same way, as `{"name": "...", "image": "...", "preCondition": "..."}`, and only then is one written. `-cc` carries the custom section that module gets, as `{"appPool": "...", "options": "..."}`, and needs `-gm`. `-h` prints the full help. Without `-c` the copy is written to `%TEMP%\iisconfig\applicationhost-<id>.config`.

## Architecture

- Main project `IISAppCmd/IISAppCmd.csproj` (SDK-style, `Exe`, `net481`, root namespace `IISAppCmd`) and test project `IISAppCmd/test/IISAppCmd.Tests.csproj` (NUnit 4).
- Solution uses the new slnx format (`IISAppCmd.slnx`) rather than a legacy `.sln`.
- `src/CommandLine/`: option model and parser (`-ap`, `-gm`, `-cc`, `-b`, `-t`, `-p`, `-c`, `-h`), mirroring `iisexpressstarter`'s syntax.
- `src/Config/ApplicationHostConfig.cs`: locates the bundled config and creates the working copy; never overwrites an existing file.
- `src/IIS/`: data structures mirroring IIS schema elements (`ApplicationPool`, `Site`, `GlobalModule`, `CustomConfig`, …), the factories that fill them from the command line (`ApplicationPoolFactory`, `SiteFactory`, `GlobalModuleFactory`, `CustomConfigFactory`) and the builders that write them through a `ServerManager` opened on the copy (`ApplicationPoolBuilder`, `SiteBuilder`, `GlobalModuleBuilder`; the caller commits). `SiteBuilder` is also given the pool the run created, and every application of the site that names no pool runs in it. `GlobalModuleBuilder` declares the module in `<globalModules>` and enables it in `<modules>`, which is what makes IIS load it. `ConfigurationWriter` holds the attribute conversions the builders need.
- The custom section of `Resources/IISAgentConfigSchema.xml` (`<dynatrace><config appPool options /></dynatrace>` inside the module's entry in `<modules>`) is the one thing not written through `Microsoft.Web.Administration`: the IIS configuration system on the machine has no schema for it and refuses both to write and to read such an element. `GlobalModuleBuilder.BuildCustomConfig()` therefore edits the XML directly and has to run *after* `CommitChanges()`, and nothing may open the file through a `ServerManager` afterwards.
- `src/Program.cs`: parse, copy, build the pool, build the site, build the global module when one was asked for, single `CommitChanges()`, then the custom section. Exit codes follow the reference tool: 0 success, 1 usage error, 3 configuration error.
