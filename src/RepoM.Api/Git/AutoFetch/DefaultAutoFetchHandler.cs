namespace RepoM.Api.Git.AutoFetch;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RepoM.Api.Common;

public class DefaultAutoFetchHandler : IAutoFetchHandler
{
    private bool _active;
    private AutoFetchMode? _mode;
    private readonly Timer _timer;
    private readonly Dictionary<AutoFetchMode, AutoFetchProfile> _profiles;
    private int _lastFetchRepository = -1;

    public DefaultAutoFetchHandler(
        IAppSettingsService appSettingsService,
        IRepositoryInformationAggregator repositoryInformationAggregator,
        IRepositoryWriter repositoryWriter)
    {
        AppSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
        RepositoryInformationAggregator = repositoryInformationAggregator ?? throw new ArgumentNullException(nameof(repositoryInformationAggregator));
        RepositoryWriter = repositoryWriter ?? throw new ArgumentNullException(nameof(repositoryWriter));
        AppSettingsService.RegisterInvalidationHandler(() => Mode = AppSettingsService.AutoFetchMode);

        _profiles = new Dictionary<AutoFetchMode, AutoFetchProfile>
            {
                { AutoFetchMode.Off, new AutoFetchProfile { PauseBetweenFetches = TimeSpan.MaxValue, } },
                { AutoFetchMode.Discretely, new AutoFetchProfile { PauseBetweenFetches = TimeSpan.FromMinutes(5), } },
                { AutoFetchMode.Adequate, new AutoFetchProfile { PauseBetweenFetches = TimeSpan.FromMinutes(1), } },
                { AutoFetchMode.Aggressive, new AutoFetchProfile { PauseBetweenFetches = TimeSpan.FromSeconds(2), } },
            };

        _timer = new Timer(FetchNext, null, Timeout.Infinite, Timeout.Infinite);
    }

    private void UpdateBehavior()
    {
        if (!_mode.HasValue)
        {
            return;
        }

        UpdateBehavior(_mode.Value);
    }

    private void UpdateBehavior(AutoFetchMode mode)
    {
        AutoFetchProfile profile = _profiles[mode];

        var milliseconds = (int)profile.PauseBetweenFetches.TotalMilliseconds;
        if (profile.PauseBetweenFetches == TimeSpan.MaxValue)
        {
            milliseconds = Timeout.Infinite;
        }

        _timer.Change(milliseconds, Timeout.Infinite);
    }

    private void FetchNext(object? timerState)
    {
        var hasAny = RepositoryInformationAggregator.Repositories?.Any() ?? false;
        if (!hasAny)
        {
            return;
        }

        // sort the repository list alphabetically each time because  ...
        // 1. it's most comprehensive for the user
        // 2. makes sure that no repository is jumped over because the list
        //    of repositories is constantly changed and not sorted in any way in memory.
        //    So we cannot guarantee that each repository is fetched on each iteration if we do not sort.
        var repositories = RepositoryInformationAggregator.Repositories?
                                                          .OrderBy(r => r.Name)
                                                          .ToArray() ?? Array.Empty<RepositoryViewModel>();

        // temporarily disable the timer to prevent parallel fetch executions
        UpdateBehavior(AutoFetchMode.Off);

        _lastFetchRepository++;

        if (repositories.Length <= _lastFetchRepository)
        {
            _lastFetchRepository = 0;
        }

        RepositoryViewModel repositoryViewModel = repositories[_lastFetchRepository];

        Console.WriteLine($"Auto-fetching {repositoryViewModel.Name} (index {_lastFetchRepository} of {repositories.Length})");

        repositoryViewModel.IsSynchronizing = true;
        try
        {
            RepositoryWriter.Fetch(repositoryViewModel.Repository);
        }
        catch
        {
            // nothing to see here
        }
        finally
        {
            // re-enable the timer to get to the next fetch
            UpdateBehavior();

            repositoryViewModel.IsSynchronizing = false;
        }
    }

    public bool Active
    {
        get => _active;
        set
        {
            _active = value;

            if (value && _mode == null)
            {
                Mode = AppSettingsService.AutoFetchMode;
            }

            UpdateBehavior();
        }
    }

    public AutoFetchMode Mode
    {
        get => _mode ?? AutoFetchMode.Off;
        set
        {
            if (value == _mode)
            {
                return;
            }

            _mode = value;
            UpdateBehavior();
        }
    }

    public IAppSettingsService AppSettingsService { get; }

    public IRepositoryInformationAggregator RepositoryInformationAggregator { get; }

    public IRepositoryWriter RepositoryWriter { get; }
}