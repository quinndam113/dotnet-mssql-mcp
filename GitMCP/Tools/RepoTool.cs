using LibGit2Sharp;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace GitMCP.Tools;
[McpServerToolType]
public static class RepoTool
{
    [McpServerTool, Description("""
        Get git repository details for the specified path to repository.
        """)]
    public static string GetGitRepositoryInfo(string repoPath)
    {
        try
        {
            var repo = new Repository(repoPath);

            return $"Git repo from path: {repo.Info.WorkingDirectory}";
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get branches for the specified path to repository.
        """)]
    public static string GetBranches(string repoPath)
    {
        try
        {
            var repo = new Repository(repoPath);

            var branches = repo.Branches.Select(b => b.FriendlyName).ToList();
            return string.Join(", ", branches);
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get current branch for the specified path to repository.
        """)]
    public static string GetCurrentBranch(string repoPath)
    {
        try
        {
            var repo = new Repository(repoPath);
            var currentBranch = repo.Head.FriendlyName;
            return $"Current branch: {currentBranch}";
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get diff between the current branch and the specified branch for the specified path to repository.
        """)]
    public static string GetDiff(string repoPath, string targetBranchName)
    {
        try
        {
            var repo = new Repository(repoPath);
            var currentBranch = repo.Head;
            var targetBranch = repo.Branches[targetBranchName];
            if (targetBranch == null)
            {
                return $"Branch '{targetBranchName}' not found.";
            }
            var diff = repo.Diff.Compare<TreeChanges>(currentBranch.Tip.Tree, targetBranch.Tip.Tree);
            return $"Diff between '{currentBranch.FriendlyName}' and '{targetBranch.FriendlyName}': {diff.Count} changes.";
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get current file changes for the specified path to repository.
        """)]
    public static string GetCurrentFileChanges(string repoPath)
    {
        try
        {
            var repo = new Repository(repoPath);
            var status = repo.RetrieveStatus();
            var changes = status.Added.Concat(status.Modified).Concat(status.Removed).ToList();

            var result = new StringBuilder();
            foreach (var change in changes)
            {
                result.AppendLine($"{change.State}: {change.FilePath}");
            }
            return result.ToString();
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get current file change content for the specified path to repository and file path.
        """)]
    public static string GetCurrentFileChangeContent(string repoPath, string filePath)
    {
        try
        {
            var repo = new Repository(repoPath);
            var status = repo.RetrieveStatus(filePath);
            if (status == null)
            {
                return "File is not changed or does not exist in the repository.";
            }

            var blob = repo.Lookup<Blob>(repo.Head.Tip[filePath].Target.Sha);
            return blob.GetContentText();
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get list of commits for the specified path to repository and branch name. Show the commit SHA, message, author, and date.
        """)]
    public static string GetCommits(string repoPath, string branchName)
    {
        try
        {
            var repo = new Repository(repoPath);
            var branch = repo.Branches[branchName];
            if (branch == null)
            {
                return $"Branch '{branchName}' not found.";
            }
            var commits = repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = branch }).ToList();
            var result = new StringBuilder();
            foreach (var commit in commits)
            {
                result.AppendLine($"{commit.Sha}: {commit.MessageShort} by {commit.Author.Name} on {commit.Author.When}");
            }
            return result.ToString();
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Cherry-pick a commit from the specified branch to the current branch for the specified path to repository.
        Can specify commit datetime to use for the cherry-picked commit or use the original commit datetime.
        """)]
    public static string CherryPickCommit(string repoPath, string commitSha, DateTimeOffset? commitDateTime = null)
    {
        try
        {
            var repo = new Repository(repoPath);
            var commit = repo.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                return $"Commit '{commitSha}' not found.";
            }

            Configuration config = repo.Config;
            string userName = config.Get<string>("user.name").Value;
            string userEmail = config.Get<string>("user.email").Value;
            var committer = new Signature(userName, userEmail, commitDateTime ?? DateTimeOffset.Now);

            // Step 4: Perform the cherry-pick
            CherryPickOptions options = new CherryPickOptions();
            CherryPickResult result = repo.CherryPick(commit, committer);

            var msg = new StringBuilder();
            // Step 5: Check the result
            switch (result.Status)
            {
                case CherryPickStatus.CherryPicked:
                    msg.AppendLine("Cherry-pick completed successfully.");
                    break;
                case CherryPickStatus.Conflicts:
                    msg.AppendLine("Cherry-pick resulted in conflicts. Resolve them manually.");
                    foreach (var conflict in repo.Index.Conflicts)
                    {
                        msg.AppendLine($"Conflict in file: {conflict.Ours.Path}");
                    }
                    break;
                default:
                    msg.AppendLine($"Cherry-pick status: {result.Status}");
                    break;
            }

            return msg.ToString();
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Commit changes to the specified path to repository with the specified commit message.
        """)]
    public static string CommitChanges(string repoPath, string commitMessage)
    {
        try
        {
            using (var repo = new Repository(repoPath))
            {
                // Stage all changes
                Commands.Stage(repo, "*");
                // Create the commit
                var author = repo.Config.BuildSignature(DateTimeOffset.Now);
                var committer = author;
                var commit = repo.Commit(commitMessage, author, committer);
                return $"Changes committed with SHA: {commit.Sha}";
            }
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }

    }

    [McpServerTool, Description("""
        Push changes to the specified path to repository with the current branch name.
        To remote branch, same as current branch name.
        """)]
    public static string PushChanges(string repoPath)
    {
        try
        {
            using (var repo = new Repository(repoPath))
            {
                var remote = repo.Network.Remotes["origin"];
                if (remote == null)
                {
                    return "Remote 'origin' not found.";
                }
                var currentBranch = repo.Head;
                var pushOptions = new PushOptions();
                repo.Network.Push(remote, currentBranch.CanonicalName, pushOptions);
                return $"Changes pushed to remote '{remote.Name}' on branch '{currentBranch.FriendlyName}'.";
            }
        }
        catch (RepositoryNotFoundException)
        {
            return "Repository not found.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
