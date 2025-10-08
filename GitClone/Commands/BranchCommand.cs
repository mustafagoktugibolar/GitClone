using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands;

public class BranchCommand(IBranchService branchService) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("branch", StringComparison.OrdinalIgnoreCase);
    }

    public void Handle(string[] args)
    {
        switch (args.Length)
        {
            case 2:
                if (args[1].Equals("-h", StringComparison.OrdinalIgnoreCase) || args[1].Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    ShowUsage();
                    break;
                }
                var newBranch = args[1];
                branchService.CreateBranch(newBranch);
                break;

            case 3 when args[1].Equals("-d", StringComparison.OrdinalIgnoreCase) || args[1].Equals("--delete", StringComparison.OrdinalIgnoreCase):
                var delBranch = args[2];
                branchService.DeleteBranch(delBranch);
                break;

            case 4 when args[1] == "-m":
                var oldName = args[2];
                var newName = args[3];
                branchService.RenameBranch(oldName, newName);
                break;

            default:
                ShowUsage();
                break;
        }
    }
    private void ShowUsage()
    {
        throw new NotImplementedException();
    }
}
    