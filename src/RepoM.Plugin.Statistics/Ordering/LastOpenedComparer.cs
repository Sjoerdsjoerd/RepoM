namespace RepoM.Plugin.Statistics.Ordering;

using System;
using System.Collections.Generic;
using System.Linq;
using RepoM.Core.Plugin.Repository;
using RepoM.Core.Plugin.RepositoryOrdering;

internal class LastOpenedComparer : IRepositoryComparer
{
    private readonly StatisticsService _service;
    private readonly int _weight;
    
    public LastOpenedComparer(StatisticsService service, int weight)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _weight = weight;
    }

    public int Compare(IRepository? x, IRepository? y)
    {
        if (_weight == 0)
        {
            return 0;
        }

        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (ReferenceEquals(null, y))
        {
            return _weight;
        }

        if (ReferenceEquals(null, x))
        {
            return -1 * _weight;
        }

        DateTime lastX = GetLast(x);
        DateTime lastY = GetLast(y);

        if (lastX == lastY)
        {
            return 0;
        }

        if (lastX < lastY)
        {
            return _weight;
        }

        return -1 * _weight;
    }

    private DateTime GetLast(IRepository repository)
    {
        IReadOnlyList<DateTime> items = _service.GetRecordings(repository);

        return items.Count == 0
            ? DateTime.MinValue
            : items.MaxBy(x => x);
    }
}