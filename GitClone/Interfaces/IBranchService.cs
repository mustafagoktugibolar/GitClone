namespace GitClone.Interfaces;

public interface IBranchService
{
    Task EnsureCreated();
    Task WriteHead(string branchName);
    Task<(string fullPath, string branchName)> ReadHead();
    Task CreateBranch(string branchName);
    Task DeleteBranch(string branchName);
    Task RenameBranch(string branchName, string newBranchName);
    Task ListBranches();
}