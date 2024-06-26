namespace RepoM.Plugin.EverythingFileSearch.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using RepoM.Core.Plugin.RepositoryFinder;

internal class EverythingGitRepositoryFinder : IGitRepositoryFinder
{
    private readonly IPathSkipper _pathSkipper;

    public EverythingGitRepositoryFinder(IPathSkipper pathSkipper)
    {
        _pathSkipper = pathSkipper ?? throw new ArgumentNullException(nameof(pathSkipper));
    }

    public List<string> Find(string root, Action<string>? onFoundAction)
    {
        const string SEARCH = "file:\"HEAD\" endwith:\"HEAD\" startwith:\"HEAD\"";

        var result = Everything64Api.Search($"\"{root}\" {SEARCH}")
                                    .Where(item => !string.IsNullOrWhiteSpace(item))
                                    .Where(item => !_pathSkipper.ShouldSkip(item))
                                    .ToList();

        if (onFoundAction == null)
        {
            return result;
        }

        foreach (var item in result)
        {
            onFoundAction.Invoke(item);
        }

        return result;
    }
}