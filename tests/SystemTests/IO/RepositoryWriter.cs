namespace SystemTests.IO;

using System;
using System.IO.Abstractions;
using System.Linq;
using LibGit2Sharp;

public class RepositoryWriter
{
    private readonly IFileSystem _fileSystem;

    public RepositoryWriter(string path, IFileSystem fileSystem)
    {
        Path = path;
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public void InitBare()
    {
        _ = Repository.Init(Path, isBare: true);
    }

    public void Clone(string sourcePath)
    {
        Repository.Clone(sourcePath, Path);
    }

    public void Branch(string name)
    {
        using var repo = new Repository(Path);
        repo.CreateBranch(name);
    }

    public void CreateFile(string nameWithExtension, string content)
    {
        _fileSystem.File.WriteAllText(System.IO.Path.Combine(Path, nameWithExtension), content);
    }

    public void Stage(string nameWithExtension)
    {
        using var repo = new Repository(Path);
        Commands.Stage(repo, System.IO.Path.Combine(Path, nameWithExtension));
    }

    public void Commit(string message)
    {
        using var repo = new Repository(Path);
        repo.Commit(message, Signature, Signature);
    }

    public string Fetch()
    {
        using var repo = new Repository(Path);
        var logMessage = string.Empty;

        Remote remote = repo.Network.Remotes.Single();
        var refs = remote.FetchRefSpecs.Select(r => r.Specification).ToArray();
        Commands.Fetch(repo, remote.Name, refs, new FetchOptions(), logMessage);

        return logMessage;
    }

    public void Pull()
    {
        using var repo = new Repository(Path);
        Remote remote = repo.Network.Remotes.Single();
        Commands.Pull(repo, Signature, new PullOptions());
    }

    public void Merge(string branchName)
    {
        using var repo = new Repository(Path);
        repo.Merge(repo.Branches[branchName], Signature);
    }

    public void MergeWithTracked()
    {
        using var repo = new Repository(Path);
        repo.Merge(repo.Head.TrackedBranch, Signature);
    }

    public int Rebase(string ontoBranchName)
    {
        using var repo = new Repository(Path);
        Branch branch = repo.Head;
        Branch target = repo.Branches[ontoBranchName];

        // ATTENTION:
        // param "onto" should be null when just rebasing to the given upstream:
        // https://libgit2.github.com/libgit2/#HEAD/group/rebase/git_rebase_init
        Branch? onto = null;

        RebaseResult result = repo.Rebase.Start(branch, target, onto, Identity, new RebaseOptions());
        return (int)result.TotalStepCount;
    }

    public void Push()
    {
        using var repo = new Repository(Path);
        Remote remote = repo.Network.Remotes.Single();
        repo.Network.Push(remote, repo.Head.CanonicalName, new PushOptions());
    }

    internal void Checkout(string branch)
    {
        using var repo = new Repository(Path);
        Commands.Checkout(repo, branch);
    }

    public string Path { get; }

    private static Identity Identity => new("John Doe", "johndoe@anywhe.re");

    private static Signature Signature => new(Identity, DateTimeOffset.Now);

    public string CurrentBranch
    {
        get
        {
            using var repo = new Repository(Path);
            return repo.Head.FriendlyName;
        }
    }

    public string HeadTip
    {
        get
        {
            using var repo = new Repository(Path);
            return repo.Head.Tip.Sha;
        }
    }
}