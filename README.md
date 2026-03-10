# GitClone

GitClone is a lightweight Git-like CLI tool written in `.NET`.
The CLI command name is packaged as `ilos`.

## Features

- Create a repository (`init`)
- Check status (`status`)
- Stage files (`add`)
- Create commits (`commit`)
- View history (`log`)
- Branch operations (`branch`, `switch`)
- Compare changes (`diff`)
- Restore/reset changes (`restore`, `reset`)
- Clone remote repositories (`clone`)
- Manage configuration (`config`)

## Solution Structure

```text
GitClone/
|- GitClone.sln
|- GitClone.Cli/               # CLI entry point and command handlers
|  |- Program.cs
|  |- Commands/
|  |  |- AddCommand.cs
|  |  |- BranchCommand.cs
|  |  |- CloneCommand.cs
|  |  |- CommitCommand.cs
|  |  |- ConfigCommand.cs
|  |  |- DiffCommand.cs
|  |  |- HelpCommand.cs
|  |  |- InitCommand.cs
|  |  |- LogCommand.cs
|  |  |- ResetCommand.cs
|  |  |- RestoreCommand.cs
|  |  |- StatusCommand.cs
|  |  |- SwitchCommand.cs
|  |  `- VersionCommand.cs
|  `- Rendering/               # Output renderers for commands
|- GitClone.Application/       # UseCase layer (business rules)
|  |- Add/
|  |- Branch/
|  |- Clone/
|  |- Commit/
|  |- Config/
|  |- Diff/
|  |- Help/
|  |- Init/
|  |- Log/
|  |- Reset/
|  |- Restore/
|  |- Status/
|  |- Switch/
|  `- Version/
|- GitClone.Core/              # Abstractions, interfaces, and core models
|- GitClone.Infrastructure/    # Filesystem/runtime/service implementations
|- tests/GitClone.Tests/       # Unit and architecture tests
`- tools/GitClone.DebugHarness/
```

## Command Implementation Pattern

The codebase follows a `commands/BranchCommand` style:

```text
GitClone.Cli/Commands/BranchCommand.cs
  -> GitClone.Application/Branch/BranchUseCase.cs
  -> GitClone.Cli/Rendering/BranchRenderer.cs
```

General flow:

1. `Command` receives arguments and builds a `Request`.
2. `UseCase` runs business logic and returns a `Result`.
3. `Renderer` prints the result to the terminal.

This same pattern is used by other commands (`StatusCommand`, `CommitCommand`, `SwitchCommand`, etc.).

## Supported Commands

According to `ilos --help` output:

- `init`
- `status`
- `add`
- `commit`
- `log`
- `switch`
- `diff`
- `restore`
- `reset`
- `branch`
- `clone`
- `config`
- `--help`
- `--version`

## Development

### Requirements

- `.NET SDK` (targets `net10.0`)

### Build and Test

```bash
dotnet restore GitClone.sln
dotnet build GitClone.sln
dotnet test GitClone.sln
```

### Run CLI Locally

```bash
dotnet run --project GitClone.Cli -- --help
dotnet run --project GitClone.Cli -- init
dotnet run --project GitClone.Cli -- status
```

## Packaging

The `GitClone.Cli` project can be packed as a global tool (`PackageId: ilos`).

```bash
dotnet pack GitClone.Cli -c Release
```

The generated package is written to `GitClone.Cli/nupkg/` by default.

## Contributing

- When adding a new command, follow the existing pattern:
  - `GitClone.Cli/Commands/<Command>Command.cs`
  - `GitClone.Application/<Command>/<Command>UseCase.cs`
  - `GitClone.Cli/Rendering/<Command>Renderer.cs`
- Update tests under `tests/GitClone.Tests`.
