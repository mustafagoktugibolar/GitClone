namespace GitClone.Interfaces;

public interface IBranchService
{
    void EnsureCreated();
    void WriteHead(string branchName);
    (string fullPath, string branchName) ReadHead();
    void CreateBranch(string branchName);
    void DeleteBranch(string branchName);
    void RenameBranch(string branchName, string newBranchName);
    void ListBranches();
}