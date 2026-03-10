namespace GitClone.Application.Help;

public sealed class HelpUseCase : IUseCase<HelpRequest, HelpResult>
{
    public Task<HelpResult> ExecuteAsync(HelpRequest request)
    {
        var lines = new[]
        {
            "Usage: ilos <command>",
            "Commands:",
            "  init: Create an empty Ilos repository",
            "  status: Show tracked/untracked files",
            "  add: Stage a file or all files",
            "  commit: Create a commit from staged files",
            "  log: Show commit history",
            "  switch: Switch or create branches",
            "  diff: Show working tree or staged changes",
            "  restore: Restore files from index or HEAD",
            "  reset: Move HEAD and optionally update index/worktree",
            "  branch: Create/list/rename/delete branches",
            "  clone: Clone a remote repository",
            "  config: Manage local/global configuration",
            "  --help: Show help",
            "  --version: Show ilos version"
        };

        return Task.FromResult(new HelpResult(lines));
    }
}
