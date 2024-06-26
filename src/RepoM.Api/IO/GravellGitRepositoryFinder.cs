namespace RepoM.Api.IO;

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using RepoM.Core.Plugin.RepositoryFinder;

// http://stackoverflow.com/questions/2106877/is-there-a-faster-way-than-this-to-find-all-the-files-in-a-directory-and-all-sub
internal class GravellGitRepositoryFinder : IGitRepositoryFinder
{
    private readonly IPathSkipper _pathSkipper;
    private readonly IFileSystem _fileSystem;

    public GravellGitRepositoryFinder(IPathSkipper pathSkipper, IFileSystem fileSystem)
    {
        _pathSkipper = pathSkipper ?? throw new ArgumentNullException(nameof(pathSkipper));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public List<string> Find(string root, Action<string> onFoundAction)
    {
        return FindInternal(root, "HEAD", onFoundAction).ToList();
    }

    private IEnumerable<string> FindInternal(string root, string searchPattern, Action<string> onFound)
    {
        var pending = new Queue<string>();
        pending.Enqueue(root);
        while (pending.Count > 0)
        {
            root = pending.Dequeue();

            if (_pathSkipper.ShouldSkip(root))
            {
                continue;
            }

            string[] tmp;
            try
            {
                tmp = _fileSystem.Directory.GetFiles(root, searchPattern);
            }
            catch (Exception)
            {
                continue;
            }

            for (var i = 0; i < tmp.Length; i++)
            {
                onFound?.Invoke(tmp[i]);
                yield return tmp[i];
            }

            tmp = _fileSystem.Directory.GetDirectories(root);
            for (var i = 0; i < tmp.Length; i++)
            {
                pending.Enqueue(tmp[i]);
            }
        }
    }
}