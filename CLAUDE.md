# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

IISAppCmd is a .NET Framework console application that modifies an IIS configuration file. The project is currently a bare scaffold (`Program.cs` has an empty `Main`) — no IIS-specific logic has been implemented yet.

## Build

Requires .NET Framework 4.8.1 and MSBuild (Visual Studio 2022 or Build Tools).

```bash
msbuild IISAppCmd.slnx
```

Or open `IISAppCmd.slnx` in Visual Studio.

Build a specific configuration:

```bash
msbuild IISAppCmd.slnx /p:Configuration=Release
```

Output goes to `IISAppCmd/bin/Debug/` or `IISAppCmd/bin/Release/`.

## Run

```bash
IISAppCmd\bin\Debug\IISAppCmd.exe
```

## Architecture

- Single project (`IISAppCmd/IISAppCmd.csproj`), `Exe` output, root namespace `IISAppCmd`.
- Solution uses the new slnx format (`IISAppCmd.slnx`) rather than a legacy `.sln`.
- Targets .NET Framework v4.8.1 (see `App.config` and the `.csproj`).
- No test project exists yet.
