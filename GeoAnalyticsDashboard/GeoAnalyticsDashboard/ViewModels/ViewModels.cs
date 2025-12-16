using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GeoAnalyticsDashboard;

/// <summary>
/// View model for the desktop geo analytics dashboard. Loads EV adoption data from CSV, exposes
/// observable series and collections consumed by Syncfusion Maps/Charts, tracks selection state,
/// computes YoY growth and powertrain mix, and provides commands to toggle insights/trend views.
/// </summary>
public class MainPageViewModel : INotifyPropertyChanged
{
    public ObservableCollection<CountryAdoptionSnapshot> Countries { get; } = new();
    public ObservableCollection<TopCountryShare> TopCountries { get; } = new();
    public ObservableCollection<YearlySharePoint> BatterySeries { get; } = new();
    public ObservableCollection<YearlySharePoint> PlugInSeries { get; } = new();

    public ObservableCollection<PowertrainMixSlice> SelectedMix { get; } = new();

    public List<Brush> CustomBrushes { get; set; }

    private int _detailsIndex;
    public int DetailsIndex { get => _detailsIndex; set => SetProperty(ref _detailsIndex, value); }


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
        // Custom color palette for the charts
        CustomBrushes = new List<Brush>();
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#1e3a8a")));
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#1d4ed8")));
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#3b82f6")));
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#93c5fd")));
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#dbeafe")));

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

/// <summary>
/// Loads and parses the EV adoption CSV from the app package, builds latest-year country snapshot
/// collections for the map and top-5 pie, and initializes the first selection for details/trends.
/// </summary>
/// <param name="fileNameInRaw">CSV file name as packaged in the application.</param>
public async Task LoadCsvData(string fileNameInRaw)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync(fileNameInRaw);
        using var reader = new StreamReader(stream);

        var header = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(header)) return;

        // Skip header and parse remaining lines
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
                BatteryShare = Parse(parts[4])
            });
        }

        var latestYear = _allData.Max(d => d.Year);
        var latestData = _allData.Where(d => d.Year == latestYear)
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
        foreach (var top in latestData.Take(5))
        {
            TopCountries.Add(new TopCountryShare { Country = top.Country, Value = top.BatteryShare });
        }

        if (Countries.Count > 0)
        {
            ApplySelection(Countries[0]);
        }

    }

/// <summary>
/// Applies the selected country. Updates insight fields, rebuilds Battery/Plug-in trend series,
/// computes YoY growth from the most recent two years, regenerates powertrain mix, and updates
/// the actionable recommendation text.
/// </summary>
/// <param name="cs">Country snapshot representing latest-year values for a country.</param>
public void ApplySelection(CountryAdoptionSnapshot cs)
    {
        if (cs == null) return;

        SelectedCountryName = cs.Name;
        SelectedBatteryShare = cs.BatteryShare;
        SelectedPlugInShare = cs.PlugInShare;

        // Build trend series
        var countryData = _allData.Where(d => d.Country == cs.Name)
                                  .OrderBy(d => d.Year)
                                  .ToList();

        BatterySeries.Clear();
        PlugInSeries.Clear();

        for (int i = 0; i < countryData.Count; i++)
        {
            BatterySeries.Add(new YearlySharePoint { Year = countryData[i].Year.ToString(), Value = countryData[i].BatteryShare });
            PlugInSeries.Add(new YearlySharePoint { Year = countryData[i].Year.ToString(), Value = countryData[i].PlugInShare });
        }

        // YoY growth based on battery share (latest - previous)
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

        // Recommendation
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

    // CSV split (supports quoted commas if ever present)
    private static string[] SplitCsv(string line)
    {
        return line.Split(',');
    }

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