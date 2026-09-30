# IISAppCmd

A .NET Framework console application which modifies an IIS configuration file.

It copies an `applicationHost.config` to a working location and writes an
application pool, the site that runs in it and, when asked for one, a native
global module with the custom section of the IIS agent into the copy, using the
same command-line syntax as the `iisexpressstarter` reference tool. It can also
set the server-wide caching, request filtering and directory browsing sections,
as `appcmd set config` would. The original is never touched.

## Requirements

The .NET Framework 4.8.1 runtime, on Windows. Nothing else: the archive carries
the executable and every assembly it needs.

## Install

Each release ships in two forms. Both hold the same files.

**A zip attached to the GitHub release** — no credentials needed, which makes it
the one to fetch from an application at runtime:

```
https://github.com/sassi67/IISAppCmd/releases/download/v1.0.0/IISAppCmd-1.0.0.zip
```

It unzips flat to `IISAppCmd.exe` and its dependencies. A
`IISAppCmd-1.0.0.zip.sha256` is attached beside it to verify the download.

**A NuGet package on GitHub Packages**, for a .NET consumer or a build that
already reads that feed. It is a distribution container rather than something you
reference: the executable is under `tools/net481/`, and the package declares no
dependency to restore. GitHub Packages requires authentication on every download,
including from this public repository, so a fetch needs a token with
`read:packages`:

```
GET https://nuget.pkg.github.com/sassi67/download/iisappcmd/1.0.0/iisappcmd.1.0.0.nupkg
Authorization: Bearer <token>
```

The id and the version are lower-cased in that URL.

## Usage

```
IISAppCmd -ap <json> [-app <json>]... [-vd <json>]... [-gm <json>] [-cc <json>] [-ca <json>] [-rf <json>] [-db <json>] [-b <32|64>] [-t <tfm>] [-p <port>] [-s <path>] [-c <path>]
```

| Option | | Meaning |
| --- | --- | --- |
| `-ap` | `--application` | Application the site serves, as `{"name": "...", "path": "..."}`. **Required.** Its path is served from the root of the site. |
| `-app` | | Additional application of the same site, as `{"path": "...", "physicalPath": "...", "applicationPool": "..."}`. Unlike `-ap`'s, `path` is the application's URL path, not its physical path. `applicationPool` is optional. May be repeated. |
| `-vd` | | Additional virtual directory of an application added by `-ap` or `-app`, as `{"parentPath": "...", "path": "...", "physicalPath": "..."}`, named by the `parentPath` of the application it attaches to. May be repeated. |
| `-gm` | `--globalmodule` | Native module to register, as `{"name": "...", "image": "...", "preCondition": "..."}`. Only then is one written. |
| `-cc` | `--customconfig` | Custom section that module gets, as `{"appPool": "...", "options": "..."}`. Needs `--globalmodule`. |
| `-ca` | `--caching` | Output caching, as `{"enabled": true, "enableKernelCache": false}`. |
| `-rf` | `--requestfiltering` | Request filtering, as `{"removeServerHeader": true}`. |
| `-db` | `--directorybrowse` | Directory browsing, as `{"enabled": false}`. |
| `-b` | `--bitness` | Bitness of the worker process, `32` or `64`. Default `64`. |
| `-t` | `--tfm` | Framework the application targets. Default `netcoreapp3.1`. |
| `-p` | `--port` | Port the site listens on, 1-65535. Default `5001`. |
| `-s` | `--source` | The `applicationHost.config` to copy, usually the one of an installed IIS Express. |
| `-c` | `--config` | Where the working copy is written. Default `%TEMP%\iisconfig\applicationhost-<id>.config`. |
| `-h` | `--help` | Print the full help. |

Options may be given in short or long form, with a space or an `=` between the
option and its value.

### `--source` and `--config`

These two are the input and the output of the same copy, which the names do not
make obvious:

- **`--source`** is the file the run **reads**. It is never modified.
- **`--config`** is the file the run **writes**. It is the deliverable.

```
--source                        --config
IISExpress's                    a fresh, tailored
applicationHost.config   ──►    applicationHost.config
(read, untouched)               (written, then edited)
```

The run copies the source to the destination and then applies every edit — the
application pool, the site, the global module, the custom section — to the copy.
The source only ever serves as a template.

`--config` is the handoff to IIS Express, which IISAppCmd never starts itself:

```
iisexpress.exe /config:<the --config path> /site:Site_<id>
```

**Pass `--config` explicitly.** Without it the tool invents
`%TEMP%\iisconfig\applicationhost-<id>.config`, with a fresh random id per run,
and announces it only on stdout as `Working configuration: <path>`. Naming the
path yourself means you already know it instead of having to scrape it.

**`--config` must not already exist.** An existing destination is never
overwritten; the run fails with exit code 3 instead. Use a fresh path per run, or
delete it first — this is the failure a retry is most likely to hit. The same
rule means `--source` and `--config` pointing at one file fails safely rather
than truncating your IIS Express installation.

**`--source` is not optional in a deployed copy.** A local build falls back to a
bundled `Resources\applicationHost.config`, but that file is a test fixture and
the released archive does not carry it. Point `--source` at the
`applicationHost.config` of the IIS Express you installed.

So a deployed call names both, one read and one written per run:

```
-s C:\iisexpress\AppServer\applicationHost.config
-c C:\work\applicationhost-<your run id>.config
```

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success. |
| `1` | Usage error: an unknown option, a missing `--application`, a malformed path or JSON value. The help text is printed to stderr. |
| `3` | Configuration error: the source configuration is missing, the destination already exists, a `-vd`'s `parentPath` names no application added by `-ap`/`-app`, or the IIS configuration system refused a write. |

### The custom section

`--customconfig` writes a `<dynatrace><config appPool options /></dynatrace>`
element inside the module's entry in `<modules>`. The IIS configuration system
refuses to write an element it has no schema for, so the tool takes one of two
routes: if `IISAgentConfigSchema.xml` is installed in
`%windir%\system32\inetsrv\config\schema`, the section is written through
`Microsoft.Web.Administration` with everything else; otherwise the tool edits the
XML directly after the commit. Either way the result is the same, but nothing may
open the file through `Microsoft.Web.Administration` after the second route.

### Server-wide settings

`--caching`, `--requestfiltering` and `--directorybrowse` each do what one
`appcmd set config` without a path does: they set their section at the root of
the file, so it applies to every site.

| Option | `appcmd` equivalent |
| --- | --- |
| `--caching` | `appcmd set config /section:system.webServer/caching /enabled:<bool> /enableKernelCache:<bool>` |
| `--requestfiltering` | `appcmd set config /section:system.webServer/security/requestFiltering /removeServerHeader:<bool>` |
| `--directorybrowse` | `appcmd set config /section:directoryBrowse /enabled:<bool>` |

Every member is optional, but at least one has to be given, as a JSON `true` or
`false`; an attribute left out keeps the value the file already has. A member the
section does not have is a usage error rather than being ignored, so a misspelt
attribute cannot pass for one that was set. `removeServerHeader` needs the schema
of IIS 10 or later; on an older one the run fails with exit code 3.

## Calling it from another application

The tool is a child process: pass the options, wait, and branch on the exit code.

The one real hazard is quoting. Windows hands a process a single command-line
string, so the quotes inside a JSON value have to survive that flattening. From
Java, pass the arguments as separate vector elements and escape the inner quotes
— do not build one string yourself:

```java
File exe = new File(toolDir, "IISAppCmd.exe");

Process p = new ProcessBuilder(
        exe.getAbsolutePath(),
        "-s", iisExpressHome + "\\AppServer\\applicationHost.config",
        "-ap", "{\"name\":\"Scratch\",\"path\":\"C:\\\\apps\\\\scratch\"}",
        "-p", "5001",
        "-c", new File(workDir, "applicationhost-" + runId + ".config").getAbsolutePath())
    .redirectErrorStream(true)
    .start();

int exitCode = p.waitFor();   // 0 ok, 1 usage error, 3 configuration error
```

A forward slash works inside the JSON `path`, which avoids one level of escaping:
`{"name":"Scratch","path":"C:/apps/scratch"}`.

## Building from source

Requires .NET Framework 4.8.1 and either the .NET SDK or MSBuild.

```bash
dotnet build IISAppCmd.slnx
dotnet test IISAppCmd/test/IISAppCmd.Tests.csproj
dotnet pack IISAppCmd/IISAppCmd.csproj -c Release -o artifacts -p:Version=1.0.0
```

## License

Apache License 2.0. See [LICENSE](LICENSE).
