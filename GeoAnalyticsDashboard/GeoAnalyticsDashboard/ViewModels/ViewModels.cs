
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GeoAnalyticsDashboard;

/// <summary>
/// View model for the desktop/mobile geo analytics dashboard. Loads EV adoption data from CSV,
/// exposes observable series and collections consumed by Syncfusion Maps/Charts, tracks selection state,
/// computes YoY growth and powertrain mix, and provides commands to toggle insights/trend views.
/// </summary>
public class MainPageViewModel : INotifyPropertyChanged
{
    public ObservableCollection<CountryAdoptionSnapshot> Countries { get; } = new();
    public ObservableCollection<TopCountryShare> TopCountries { get; } = new();
    public ObservableCollection<YearlySharePoint> BatterySeries { get; } = new();
    public ObservableCollection<YearlySharePoint> PlugInSeries { get; } = new();
    // Pie chart sources
    public ObservableCollection<ContinentShare> ContinentShares { get; } = new();
    public ObservableCollection<PowertrainMixSlice> SelectedMix { get; } = new();

    // INITIAL GREEN CHART/CIRCULAR PALETTE
    public List<Brush> CustomBrushes { get; set; }

    private int _latestYear;
    private int _detailsIndex;
    public int DetailsIndex { get => _detailsIndex; set => SetProperty(ref _detailsIndex, value); }

    // Explode indices for pies
    private int _continentExplodeIndex = -1;
    public int ContinentExplodeIndex { get => _continentExplodeIndex; set => SetProperty(ref _continentExplodeIndex, value); }
    private int _countryExplodeIndex = -1;
    public int CountryExplodeIndex { get => _countryExplodeIndex; set => SetProperty(ref _countryExplodeIndex, value); }

    private bool _isInsightsVisible = true;
    private bool _isTrendVisible = false;
    public bool IsInsightsVisible
    {
        get => _isInsightsVisible;
        set { if (_isInsightsVisible != value) { _isInsightsVisible = value; OnPropertyChanged(); } }
    }
    public bool IsTrendVisible
    {
        get => _isTrendVisible;
        set { if (_isTrendVisible != value) { _isTrendVisible = value; OnPropertyChanged(); } }
    }

    public ICommand ShowInsightsCommand { get; }
    public ICommand ShowTrendCommand { get; }

    // Selected insights
    private string _selectedCountryName = "Select a country";
    public string SelectedCountryName { get => _selectedCountryName; set => SetProperty(ref _selectedCountryName, value); }
    private double _selectedBatteryShare;
    public double SelectedBatteryShare { get => _selectedBatteryShare; set => SetProperty(ref _selectedBatteryShare, value); }
    private double _selectedPlugInShare;
    public double SelectedPlugInShare { get => _selectedPlugInShare; set => SetProperty(ref _selectedPlugInShare, value); }
    private double _selectedGrowth;
    public double SelectedGrowth { get => _selectedGrowth; set => SetProperty(ref _selectedGrowth, value); }
    private string _selectedRecommendation = "Click a country to view EV insights.";
    public string SelectedRecommendation { get => _selectedRecommendation; set => SetProperty(ref _selectedRecommendation, value); }

    // Internal raw data
    private readonly List<EvAdoptionRecord> _allData = new();

    public MainPageViewModel()
    {
        // Replace old blue palette with the initial green palette
        CustomBrushes = new List<Brush>
        {
            new SolidColorBrush(Color.FromArgb("#064e3b")), // Deep green
            new SolidColorBrush(Color.FromArgb("#166534")), // Primary green
            new SolidColorBrush(Color.FromArgb("#22c55e")), // Bright green
            new SolidColorBrush(Color.FromArgb("#86efac")), // Light green
            new SolidColorBrush(Color.FromArgb("#dcfce7"))  // Soft mint
        };

        _ = LoadCsvData("share-car-sales-battery-plugin.csv");

        ShowInsightsCommand = new Command(() =>
        {
            IsInsightsVisible = true;
            IsTrendVisible = false;
        });

        ShowTrendCommand = new Command(() =>
        {
            IsInsightsVisible = false;
            IsTrendVisible = true;
        });
    }

    public async Task LoadCsvData(string fileNameInRaw)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync(fileNameInRaw);
        using var reader = new StreamReader(stream);
        var header = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(header)) return;

        string? line;
        _allData.Clear();
        while ((line = await reader.ReadLineAsync()) != null)
        {
            var parts = SplitCsv(line);
            if (parts.Length < 5) continue;
            _allData.Add(new EvAdoptionRecord
            {
                Country = parts[0],
                Code = parts[1],
                Year = int.Parse(parts[2]),
                PlugInShare = Parse(parts[3]),
                BatteryShare = Parse(parts[4]),
                Continent = parts.Length > 5 ? parts[5] : null
            });
        }

        _latestYear = _allData.Max(d => d.Year);
        var latestData = _allData.Where(d => d.Year == _latestYear)
                                 .OrderByDescending(d => d.BatteryShare)
                                 .ToList();

        Countries.Clear();
        foreach (var item in latestData)
        {
            Countries.Add(new CountryAdoptionSnapshot
            {
                Name = item.Country,
                BatteryShare = item.BatteryShare,
                PlugInShare = item.PlugInShare
            });
        }

        TopCountries.Clear();
        CountryExplodeIndex = -1;

        // Continent aggregation
        ContinentShares.Clear();
        var byContinent = latestData
            .Where(d => !string.IsNullOrWhiteSpace(d.Continent))
            .GroupBy(d => d.Continent!)
            .Select(g => new ContinentShare
            {
                Continent = g.Key,
                Value = g.Average(x => x.BatteryShare)
            })
            .OrderByDescending(c => c.Value)
            .ToList();

        var total = byContinent.Sum(c => c.Value);
        if (total > 0)
        {
            foreach (var c in byContinent)
            {
                c.Percentage = Math.Round(c.Value / total * 100, 2);
                c.DisplayLabel = $"{c.Continent} {c.Percentage} %";
                ContinentShares.Add(c);
            }
            ContinentExplodeIndex = 0; // largest continent by default
        }
        else
        {
            ContinentExplodeIndex = -1;
        }

        if (Countries.Count > 0)
        {
            ApplySelection(Countries[0]);
        }
    }

    public void ApplySelection(CountryAdoptionSnapshot cs)
    {
        if (cs == null) return;

        SelectedCountryName = cs.Name;
        SelectedBatteryShare = cs.BatteryShare;
        SelectedPlugInShare = cs.PlugInShare;

        var selectedContinent = _allData
            .Where(d => d.Country == cs.Name && d.Year == _latestYear)
            .Select(d => d.Continent)
            .FirstOrDefault()
            ?? _allData.Where(d => d.Country == cs.Name && d.Continent != null)
                       .OrderByDescending(d => d.Year)
                       .Select(d => d.Continent)
                       .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(selectedContinent))
        {
            var sameContinentLatest = _allData
                .Where(d => d.Year == _latestYear && string.Equals(d.Continent, selectedContinent, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.BatteryShare)
                .ToList();

            TopCountries.Clear();
            foreach (var top in sameContinentLatest.Take(5))
                TopCountries.Add(new TopCountryShare { Country = top.Country, Value = top.BatteryShare });

            var selIdx = TopCountries.Select((t, i) => new { t, i }).FirstOrDefault(x => x.t.Country == cs.Name)?.i ?? -1;
            CountryExplodeIndex = selIdx >= 0 ? selIdx : (TopCountries.Count > 0 ? 0 : -1);

            var contIdx = ContinentShares.Select((c, i) => new { c, i })
                .FirstOrDefault(x => string.Equals(x.c.Continent, selectedContinent, StringComparison.OrdinalIgnoreCase))?.i ?? -1;
            ContinentExplodeIndex = contIdx;
        }
        else
        {
            var idx = TopCountries.Select((t, i) => new { t, i }).FirstOrDefault(x => x.t.Country == cs.Name)?.i ?? -1;
            CountryExplodeIndex = idx;
        }

        var countryData = _allData.Where(d => d.Country == cs.Name)
                                  .OrderBy(d => d.Year)
                                  .ToList();

        BatterySeries.Clear();
        PlugInSeries.Clear();

        for (int i = 0; i < countryData.Count; i++)
        {
            BatterySeries.Add(new YearlySharePoint { Year = countryData[i].Year, Value = countryData[i].BatteryShare });
            PlugInSeries.Add(new YearlySharePoint { Year = countryData[i].Year, Value = countryData[i].PlugInShare });
        }

        if (countryData.Count > 1)
        {
            var last = countryData[^1];
            var prev = countryData[^2];
            SelectedGrowth = last.BatteryShare - prev.BatteryShare;
        }
        else
        {
            SelectedGrowth = 0;
        }

        BuildSelectedMix(SelectedBatteryShare, SelectedPlugInShare);
        SelectedRecommendation = BuildRecommendation(SelectedBatteryShare, SelectedPlugInShare, SelectedGrowth);
    }

    private void BuildSelectedMix(double battery, double plugIn)
    {
        SelectedMix.Clear();
        var other = Math.Max(0, 100 - (battery + plugIn));
        SelectedMix.Add(new PowertrainMixSlice { Name = "Battery", Value = RoundPct(battery) });
        SelectedMix.Add(new PowertrainMixSlice { Name = "Plug-in", Value = RoundPct(plugIn) });
        SelectedMix.Add(new PowertrainMixSlice { Name = "Other", Value = RoundPct(other) });
    }

    private static double RoundPct(double v) => Math.Round(v, 2);
    private string BuildRecommendation(double battery, double plugIn, double growth)
    {
        if (battery >= 60) return "EV market is mature: Focus on infrastructure and fast charging.";
        if (battery >= 30 && growth >= 5) return "Strong growth: Accelerate EV incentives and marketing.";
        if (battery >= 15) return "Moderate adoption: Expand dealer networks and awareness campaigns.";
        if (battery >= 5) return "Early adoption: Pilot programs and partnerships recommended.";
        return "Seed stage: Awareness and education are key.";
    }

    private static double Parse(string s) => double.Parse(s, CultureInfo.InvariantCulture);
    private static string[] SplitCsv(string line) => line.Split(',');

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(name);
        return true;
    }
}

