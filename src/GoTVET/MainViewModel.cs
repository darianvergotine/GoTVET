using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace GoTVET;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly CatalogService _catalogService;
    private readonly DownloadService _downloadService;
    private readonly List<PastPaper> _allPapers = [];
    private string _searchText = "";
    private string _selectedProgramme = "All programmes";
    private string _selectedLevel = "All levels";
    private string _selectedField = "All fields";
    private string _selectedYear = "All years";
    private string _selectedSession = "All sessions";
    private string _selectedType = "All types";
    private string _status = "Ready";
    private string _catalogSource = "";
    private bool _isBusy;
    private double _downloadProgress;
    private PastPaper? _selectedPaper;
    private CancellationTokenSource? _refreshCts;

    public MainViewModel(CatalogService catalogService, DownloadService downloadService)
    {
        _catalogService = catalogService;
        _downloadService = downloadService;
        PapersView = CollectionViewSource.GetDefaultView(Papers);
        PapersView.Filter = FilterPaper;

        RefreshCommand = new RelayCommand(_ => RefreshAsync());
        DownloadCommand = new RelayCommand<PastPaper>(DownloadAsync, paper => paper is not null && !IsBusy);
        DownloadSelectedCommand = new RelayCommand(_ => DownloadAsync(SelectedPaper), _ => SelectedPaper is not null && !IsBusy);
        OpenSourceCommand = new RelayCommand<PastPaper>(paper =>
        {
            if (paper is not null)
            {
                DownloadService.OpenUrl(string.IsNullOrWhiteSpace(paper.BrowseUrl)
                    ? _catalogService.WebsiteUrl
                    : paper.BrowseUrl);
            }

            return Task.CompletedTask;
        });
        OpenFolderCommand = new RelayCommand(_ =>
        {
            _downloadService.OpenDownloadFolder();
            return Task.CompletedTask;
        });
        OpenWebsiteCommand = new RelayCommand(_ =>
        {
            DownloadService.OpenUrl(_catalogService.WebsiteUrl);
            return Task.CompletedTask;
        });
        OpenDownloadsCommand = new RelayCommand<PastPaper>(paper =>
        {
            if (!string.IsNullOrWhiteSpace(paper?.FileName))
            {
                var path = Path.Combine(_downloadService.DownloadFolder, paper.FileName);
                if (File.Exists(path))
                {
                    DownloadService.OpenFile(path);
                }
            }

            return Task.CompletedTask;
        });

        LoadLocalCatalog();
        _ = RefreshAsync();
    }

    public ObservableCollection<PastPaper> Papers { get; } = [];
    public ObservableCollection<string> Programmes { get; } = [];
    public ObservableCollection<string> Levels { get; } = [];
    public ObservableCollection<string> Fields { get; } = [];
    public ObservableCollection<string> Years { get; } = [];
    public ObservableCollection<string> Sessions { get; } = [];
    public ObservableCollection<string> Types { get; } = [];
    public ObservableCollection<DownloadJob> RecentDownloads { get; } = [];
    public ICollectionView PapersView { get; }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand DownloadSelectedCommand { get; }
    public RelayCommand<PastPaper> DownloadCommand { get; }
    public RelayCommand<PastPaper> OpenSourceCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand OpenWebsiteCommand { get; }
    public RelayCommand<PastPaper> OpenDownloadsCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
            {
                PapersView.Refresh();
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    public string SelectedProgramme
    {
        get => _selectedProgramme;
        set
        {
            if (Set(ref _selectedProgramme, value))
            {
                RebuildLevels();
                PapersView.Refresh();
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    public string SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (Set(ref _selectedLevel, value))
            {
                PapersView.Refresh();
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    public string SelectedField
    {
        get => _selectedField;
        set
        {
            if (Set(ref _selectedField, value))
            {
                PapersView.Refresh();
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    public string SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (Set(ref _selectedYear, value))
            {
                PapersView.Refresh();
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    public string SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (Set(ref _selectedSession, value))
            {
                PapersView.Refresh();
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    public string SelectedType
    {
        get => _selectedType;
        set
        {
            if (Set(ref _selectedType, value))
            {
                PapersView.Refresh();
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    public PastPaper? SelectedPaper
    {
        get => _selectedPaper;
        set
        {
            if (Set(ref _selectedPaper, value))
            {
                DownloadSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string CatalogSource
    {
        get => _catalogSource;
        set => Set(ref _catalogSource, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (Set(ref _isBusy, value))
            {
                DownloadSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => Set(ref _downloadProgress, value);
    }

    public string ResultSummary => $"{CountVisible():N0} papers shown · {Papers.Count:N0} in catalogue";

    public string DownloadFolder => _downloadService.DownloadFolder;

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        IsBusy = true;
        DownloadProgress = 0;
        try
        {
            var progress = new Progress<string>(message => Status = message);
            var snapshot = await _catalogService.RefreshAsync(progress, _refreshCts.Token).ConfigureAwait(true);
            ApplySnapshot(snapshot);
            Status = snapshot.Papers.Count == 0
                ? "The GoTVET library is empty. Check your internet connection and refresh again."
                : $"Catalogue updated from {snapshot.Source}.";
        }
        catch (OperationCanceledException)
        {
            Status = "Refresh cancelled.";
        }
        catch (Exception exception)
        {
            Status = "Could not reach the GoTVET website. Check your internet connection and try Refresh library. " + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DownloadAsync(PastPaper? paper)
    {
        if (paper is null)
        {
            return;
        }

        IsBusy = true;
        DownloadProgress = 0;
        var job = new DownloadJob { Paper = paper, Status = "Downloading" };
        RecentDownloads.Insert(0, job);
        try
        {
            Status = $"Downloading {paper.Title}…";
            var progress = new Progress<double>(value =>
            {
                DownloadProgress = value;
                job.Progress = value;
            });
            var path = await _downloadService.DownloadAsync(paper, progress, CancellationToken.None).ConfigureAwait(true);
            job.Status = "Saved";
            job.LocalPath = path;
            job.Progress = 1;
            Status = $"Saved to {path}";
            DownloadService.OpenFile(path);
        }
        catch (Exception exception)
        {
            job.Status = "Needs browser";
            job.Error = exception.Message;
            Status = exception.Message;
            DownloadService.OpenUrl(string.IsNullOrWhiteSpace(paper.BrowseUrl)
                ? _catalogService.WebsiteUrl
                : paper.BrowseUrl);
        }
        finally
        {
            IsBusy = false;
            DownloadProgress = 0;
        }
    }

    private void LoadLocalCatalog()
    {
        ApplySnapshot(_catalogService.LoadLocal());
        Status = "Search a subject, then download. Papers are stored on the GoTVET website.";
    }

    private void ApplySnapshot(CatalogSnapshot snapshot)
    {
        _allPapers.Clear();
        _allPapers.AddRange(snapshot.Papers);
        Papers.Clear();
        foreach (var paper in snapshot.Papers)
        {
            Papers.Add(paper);
        }

        CatalogSource = snapshot.Source + " · updated " + snapshot.UpdatedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm");
        RebuildFilters();
        PapersView.Refresh();
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(DownloadFolder));
    }

    private void RebuildFilters()
    {
        Replace(Programmes, "All programmes", _allPapers.Select(paper => paper.Programme));
        Replace(Fields, "All fields", _allPapers.Select(paper => paper.Field));
        Replace(Years, "All years", _allPapers.Select(paper => paper.Year > 0 ? paper.Year.ToString() : "")
            .Where(year => year.Length > 0)
            .OrderByDescending(year => year));
        Replace(Sessions, "All sessions", _allPapers.Select(paper => paper.Session));
        Replace(Types, "All types", _allPapers.Select(paper => paper.DocumentType));
        RebuildLevels();
    }

    private void RebuildLevels()
    {
        var query = _allPapers.AsEnumerable();
        if (SelectedProgramme is not "All programmes")
        {
            query = query.Where(paper => paper.Programme == SelectedProgramme);
        }

        Replace(Levels, "All levels", query.Select(paper => paper.Level));
        if (!Levels.Contains(SelectedLevel))
        {
            _selectedLevel = "All levels";
            OnPropertyChanged(nameof(SelectedLevel));
        }
    }

    private static void Replace(ObservableCollection<string> target, string allLabel, IEnumerable<string> values)
    {
        var items = new[] { allLabel }.Concat(
            values.Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private bool FilterPaper(object obj)
    {
        if (obj is not PastPaper paper)
        {
            return false;
        }

        if (SelectedProgramme is not "All programmes" &&
            !string.Equals(paper.Programme, SelectedProgramme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SelectedLevel is not "All levels" &&
            !string.Equals(paper.Level, SelectedLevel, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SelectedField is not "All fields" &&
            !string.Equals(paper.Field, SelectedField, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SelectedYear is not "All years" && paper.Year.ToString() != SelectedYear)
        {
            return false;
        }

        if (SelectedSession is not "All sessions" &&
            !string.Equals(paper.Session, SelectedSession, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SelectedType is not "All types" &&
            !string.Equals(paper.DocumentType, SelectedType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return Contains(paper.Subject) || Contains(paper.Title) || Contains(paper.FileName) ||
               Contains(paper.Level) || Contains(paper.Programme);
    }

    private bool Contains(string value) =>
        value.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);

    private int CountVisible()
    {
        var count = 0;
        foreach (var _ in PapersView)
        {
            count++;
        }

        return count;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
