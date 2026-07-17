# GitClone

GitClone is a Git-like CLI tool written in `.NET`.
The CLI command name is packaged as `ilos`.

The CLI is built on [`System.CommandLine`](https://learn.microsoft.com/dotnet/standard/commandline/), so
standard behavior works everywhere: `--help`/`-h`/`-?` on every command and subcommand, `--version`,
typo suggestions ("did you mean..."), and non-zero exit codes on bad input.

## Features

- Repository lifecycle: `init`, `status`, `add`, `rm`, `mv`, `commit`, `log`
- History inspection and rewriting: `diff`, `reset`, `restore`, `cherry-pick`, `rebase`
- Branching: `branch`, `switch` (alias `checkout`), `tag`, `merge`
- Work-in-progress shelving: `stash` (`push`/`pop`/`apply`/`list`/`drop`)
- Remotes: `remote`, `fetch`, `push`, `pull`, `clone`
- Configuration: `config`

See [Supported Commands](#supported-commands) below for exact usage, or run `ilos --help` /
`ilos <command> --help`.

### Known limitations

- **Remotes are local filesystem paths, not network endpoints.** `remote`/`fetch`/`push`/`pull` copy
  commits and blobs between two `.ilos` repositories on disk (the same way real git supports
  file-based remotes) - there is no git wire protocol implementation, so an `http(s)://` remote
  doesn't work with these commands.
- **`clone` behaves differently depending on the source:**
  - A local path clones with **full history and all branches**, and automatically adds the source
    as the `origin` remote.
  - An `http(s)://` GitHub URL downloads a **ZIP snapshot of one branch** (via GitHub's archive
    endpoint) - no commit history, no other branches. This is a hard ceiling without implementing
    git's actual transfer protocol.
- **`rebase` has no `--continue`/`--abort`.** It's all-or-nothing: the full sequence of commits is
  replayed in memory first, and nothing touches disk unless every commit replays without conflict.
  A conflicting rebase aborts cleanly with zero side effects; resolve the conflict with `merge`
  instead.
- **Not git-compatible on disk.** Objects are plain files (blobs) and JSON (commits), not
  zlib-compressed/hashed the way real git objects are. An `ilos` repository cannot be opened by
  real `git`, and vice versa.
- **No detached HEAD, hooks, submodules, sparse checkout, worktrees, reflog, `bisect`, `blame`, or
  commit/tag signing.**
- `diff` reports name-status only (`A`/`M`/`D`/`??`), not line-level unified diffs.
- `.ilosignore` supports only exact filenames, `*.ext`, and `dir/` patterns - no `**` globs or `!`
  negation.

## Solution Structure

```text
GitClone/
|- GitClone.sln
|- GitClone.Cli/               # CLI entry point (System.CommandLine) and command builders
|  |- Program.cs
|  |- ConsoleTextWriter.cs
|  |- Commands/                # One <Command>Command.cs per CLI command - builds System.CommandLine.Command
|  `- Rendering/                # One <Command>Renderer.cs per command - prints a Result to the console
|- GitClone.Application/       # UseCase layer (business rules) - one folder per command
|  |- Add/  Branch/  CherryPick/  Clone/  Commit/  Config/  Diff/  Fetch/  Init/  Log/
|  |- Merge/  Mv/  Pull/  Push/  Rebase/  Remote/  Reset/  Restore/  Rm/  Stash/  Status/
|  |- Switch/  Tag/
|  `- Shared/                  # RepositoryState (index/refs/commits I/O), CommitMerging,
|                               # ConflictResolution - the shared primitives merge/rebase/
|                               # cherry-pick/stash-apply/pull all build on
|- GitClone.Core/              # Abstractions, interfaces, and core models
|- GitClone.Infrastructure/    # Filesystem/runtime/service implementations
|- tests/GitClone.Tests/       # CLI integration tests + architecture convention tests
`- tools/GitClone.DebugHarness/
```

## Command Implementation Pattern

The codebase follows a `commands/BranchCommand` style:

```text
GitClone.Cli/Commands/BranchCommand.cs   -- declares the System.CommandLine.Command (args/options/help)
  -> GitClone.Application/Branch/BranchUseCase.cs   -- business logic, builds a Result
  -> GitClone.Cli/Rendering/BranchRenderer.cs        -- prints the Result
```

General flow:

1. `Command.Build()` declares the command's arguments/options (System.CommandLine gives `--help`,
   validation, and exit codes for free) and builds a `Request` from the parsed input.
2. `UseCase` runs business logic against the repository and returns a `Result`.
3. `Renderer` prints the result to the terminal.

Commands that can produce a merge conflict (`merge`, `pull`, `cherry-pick`, `stash apply`/`pop`)
share the three-way merge algorithm in `GitClone.Application/Shared/CommitMerging.cs` and the
conflict-marker-writing logic in `ConflictResolution.cs`, rather than each reimplementing it.

## Supported Commands

Run `ilos --help` for the authoritative, up-to-date list, or `ilos <command> --help` for a
command's exact arguments and options. As of this writing:

| Command | Purpose |
|---|---|
| `init` | Create an empty repository |
| `status` | Show the working tree status |
| `add <pathspec>` | Stage file contents for the next commit |
| `rm <path> [--cached]` | Remove a file from the working tree and the index |
| `mv <source> <destination>` | Move or rename a tracked file |
| `commit -m <message>` | Record staged changes |
| `cherry-pick <commit>` | Apply the changes from an existing commit onto the current branch |
| `log [-n <count>]` | Show commit logs |
| `diff [--cached]` | Show changes between commits, or the working tree and the index |
| `merge <branch>` | Merge a branch into the current branch |
| `rebase <branch>` | Reapply the current branch's commits on top of another branch |
| `reset [--soft\|--mixed\|--hard] [<revision>]` | Reset HEAD (and optionally the index/working tree) |
| `restore <path> [--staged]` | Restore working tree files (or unstage) |
| `stash [push\|pop\|apply\|list\|drop]` | Shelve and reapply uncommitted changes |
| `switch <branch> [-b]` / `checkout` | Switch branches (optionally creating one) |
| `branch [<name>] [-d\|-m]` | List, create, delete, or rename branches |
| `tag [<name>] [-d]` | List, create, or delete tags |
| `clone <url>` | Clone a repository (see [limitations](#known-limitations)) |
| `config` | Manage local/global configuration |
| `remote [add\|remove\|list]` | Manage remotes (local filesystem paths) |
| `fetch <remote> [--branch]` | Download commits and blobs from a remote |
| `push <remote> <branch>` | Update a remote branch with local commits |
| `pull <remote> <branch>` | Fetch from a remote and merge into the current branch |

Every command also accepts `-h`/`-?`/`--help`; the root command accepts `-v`/`--version`.

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
  - `GitClone.Cli/Commands/<Command>Command.cs` - a `Build()` method returning a
    `System.CommandLine.Command` with its arguments/options declared
  - `GitClone.Application/<Command>/<Command>UseCase.cs` (+ `Request`/`Result`)
  - `GitClone.Cli/Rendering/<Command>Renderer.cs`
  - Register all three in `GitClone.Cli/Program.cs` (`AddCommands`, `AddApplication`, and
    `BuildRootCommand`)
- If the command can produce a merge conflict, reuse `GitClone.Application/Shared/CommitMerging.cs`
  and `ConflictResolution.cs` rather than reimplementing three-way merge/conflict-marker logic.
- Update tests under `tests/GitClone.Tests`.
