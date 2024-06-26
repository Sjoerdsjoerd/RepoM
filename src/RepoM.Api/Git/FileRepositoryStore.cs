namespace RepoM.Api.Git;

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

public abstract class FileRepositoryStore : IRepositoryStore
{
    private readonly IFileSystem _fileSystem;

    protected FileRepositoryStore(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    protected abstract string GetFileName();

    public IEnumerable<string> Get()
    {
        var file = GetFileName();
        return Get(file);
    }

    public void Set(IEnumerable<string> paths)
    {
        var file = GetFileName();
        var path = _fileSystem.Directory.GetParent(file)?.FullName;

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!_fileSystem.Directory.Exists(path))
        {
            _fileSystem.Directory.CreateDirectory(path);
        }

        try
        {
            _fileSystem.File.WriteAllLines(GetFileName(), paths.ToArray());
        }
        catch (Exception)
        {
            // swallow for now.
        }
    }

    private string[] Get(string file)
    {
        if (!_fileSystem.File.Exists(file))
        {
            return Array.Empty<string>();
        }

        try
        {
            return _fileSystem.File.ReadAllLines(file);
        }
        catch (Exception)
        {
            // swallow for now.
        }

        return Array.Empty<string>();
    }
}