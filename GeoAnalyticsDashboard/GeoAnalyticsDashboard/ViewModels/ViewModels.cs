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
    /// <summary>
    /// Latest-year country snapshot list used by the map and selection panel.
    /// </summary>
    public ObservableCollection<CountryAdoptionSnapshot> Countries { get; } = new();

    /// <summary>
    /// Top countries (by battery EV share) within the selected continent for the latest year.
    /// Drives the continent-specific pie and selection explode logic.
    /// </summary>
    public ObservableCollection<TopCountryShare> TopCountries { get; } = new();

    /// <summary>
    /// Time series of Battery EV share for the selected country.
    /// </summary>
    public ObservableCollection<YearlySharePoint> BatterySeries { get; } = new();

    /// <summary>
    /// Time series of Plug-in EV share for the selected country.
    /// </summary>
    public ObservableCollection<YearlySharePoint> PlugInSeries { get; } = new();

    /// <summary>
    /// Aggregated latest-year battery EV share by continent (average), used by the continent pie.
    /// </summary>
    public ObservableCollection<ContinentShare> ContinentShares { get; } = new();

    /// <summary>
    /// Powertrain mix slices (Battery, Plug-in, Other) for the selected country.
    /// Values are percentages rounded to two decimals.
    /// </summary>
    public ObservableCollection<PowertrainMixSlice> SelectedMix { get; } = new();

    /// <summary>
    /// Custom color palette used by charts to ensure consistent branding.
    /// </summary>
    public List<Brush> CustomBrushes { get; set; }

    // Tracks most recent year in the dataset to drive selections/filters
    private int _latestYear;
    private int _detailsIndex;

    /// <summary>
    /// Index for details view segment/tab selection in the UI.
    /// </summary>
    public int DetailsIndex { get => _detailsIndex; set => SetProperty(ref _detailsIndex, value); }

    // Explode continent indices for pies
    private int _continentExplodeIndex = -1;

    /// <summary>
    /// Slice index to explode in the continent pie; -1 means no explosion.
    /// </summary>
    public int ContinentExplodeIndex { get => _continentExplodeIndex; set => SetProperty(ref _continentExplodeIndex, value); }

    // Explode country indices for pies
    private int _countryExplodeIndex = -1;

    /// <summary>
    /// Slice index to explode in the top-countries pie; -1 means no explosion.
    /// </summary>
    public int CountryExplodeIndex { get => _countryExplodeIndex; set => SetProperty(ref _countryExplodeIndex, value); }

    private bool _isInsightsVisible = true;
    private bool _isTrendVisible = false;
    private bool _isContinentVisible = false;

    /// <summary>
    /// True when the Insights panel is visible.
    /// </summary>
    public bool IsInsightsVisible
    {
        get => _isInsightsVisible;
        set { if (_isInsightsVisible != value) { _isInsightsVisible = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// True when the Trend (time-series) view is visible.
    /// </summary>
    public bool IsTrendVisible
    {
        get => _isTrendVisible;
        set { if (_isTrendVisible != value) { _isTrendVisible = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// True when the Continent (aggregate) view is visible.
    /// </summary>
    public bool IsContinentVisible
    {
        get => _isContinentVisible;
        set { if (_isContinentVisible != value) { _isContinentVisible = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Command to activate the Insights view.
    /// </summary>
    public ICommand ShowInsightsCommand { get; }

    /// <summary>
    /// Command to activate the Trend view.
    /// </summary>
    public ICommand ShowTrendCommand { get; }

    /// <summary>
    /// Command to activate the Continent view.
    /// </summary>
    public ICommand ShowContinentCommand { get; }

    // Selected insights
    private string _selectedCountryName = "Select a country";

    /// <summary>
    /// Name of the currently selected country in the latest-year snapshot list.
    /// </summary>
    public string SelectedCountryName { get => _selectedCountryName; set => SetProperty(ref _selectedCountryName, value); }

    private double _selectedBatteryShare;

    /// <summary>
    /// Latest-year Battery EV share for the selected country.
    /// </summary>
    public double SelectedBatteryShare { get => _selectedBatteryShare; set => SetProperty(ref _selectedBatteryShare, value); }

    private double _selectedPlugInShare;

    /// <summary>
    /// Latest-year Plug-in EV share for the selected country.
    /// </summary>
    public double SelectedPlugInShare { get => _selectedPlugInShare; set => SetProperty(ref _selectedPlugInShare, value); }

    private double _selectedGrowth;

    /// <summary>
    /// Year-over-year growth of Battery EV share based on the two most recent years.
    /// </summary>
    public double SelectedGrowth { get => _selectedGrowth; set => SetProperty(ref _selectedGrowth, value); }

    private string _selectedRecommendation = "Click a country to view EV insights.";

    /// <summary>
    /// Contextual recommendation derived from EV share levels and growth for the selected country.
    /// </summary>
    public string SelectedRecommendation { get => _selectedRecommendation; set => SetProperty(ref _selectedRecommendation, value); }

    // Selected continent for the current selection and tooltip text for the country pie info icon
    private string _selectedContinent = string.Empty;
    private string _countryPieInfoTooltip = "Top countries by EV battery share.";

    /// <summary>
    /// Selected continent name for the currently selected country.
    /// </summary>
    public string SelectedContinent { get => _selectedContinent; set => SetProperty(ref _selectedContinent, value); }

    /// <summary>
    /// Tooltip text shown on the info icon near Country-by-Battery-Share pie title.
    /// </summary>
    public string CountryPieInfoTooltip { get => _countryPieInfoTooltip; set => SetProperty(ref _countryPieInfoTooltip, value); }

    // Internal raw data
    private readonly List<EvAdoptionRecord> _allData = new();

    /// <summary>
    /// Initializes the view model, sets a custom color palette, begins CSV loading,
    /// and wires commands for toggling Insights/Trend/Continent views.
    /// </summary>
    public MainPageViewModel()
    {
        CustomBrushes = new List<Brush>();
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#064e3b")));
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#166534")));
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#22c55e")));
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#86efac")));
        CustomBrushes.Add(new SolidColorBrush(Color.FromArgb("#dcfce7")));

        _ = LoadCsvData("share-car-sales-battery-plugin.csv");

        ShowInsightsCommand = new Command(() =>
        {
            IsInsightsVisible = true;
            IsTrendVisible = false;
            IsContinentVisible = false;
        });

        ShowTrendCommand = new Command(() =>
        {
            IsInsightsVisible = false;
            IsTrendVisible = true;
            IsContinentVisible = false;
        });

        ShowContinentCommand = new Command(() =>
        {
            IsInsightsVisible = false;
            IsTrendVisible = false;
            IsContinentVisible = true;
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
            bool isSeeding = item.BatteryShare == 0 && item.PlugInShare == 0;

            Countries.Add(new CountryAdoptionSnapshot
            {
                Name = item.Country,
                TooltipText = isSeeding
                                ? $"{item.Country} - EV in early stage"
                                : item.Country,
                BatteryShare = item.BatteryShare,
                PlugInShare = item.PlugInShare,
            });
        }

        TopCountries.Clear();
        CountryExplodeIndex = -1;

        ContinentShares.Clear();
        var byContinent = latestData
            .Where(d => !string.IsNullOrWhiteSpace(d.Continent)
                        && (d.BatteryShare > 0 || d.PlugInShare > 0))
            .GroupBy(d => d.Continent!)
            .Select(g => new ContinentShare
            {
                Continent = g.Key,
                Value = g.Average(x => x.BatteryShare)
            })
            .Where(c => c.Value > 0)
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
            ContinentExplodeIndex = 0;
        }
        else
        {
            ContinentExplodeIndex = -1;
        }

        if (Countries.Count > 0)
        {
            var firstWithData = Countries.FirstOrDefault(c => c.BatteryShare > 0 || c.PlugInShare > 0);
            if (firstWithData != null)
            {
                ApplySelection(firstWithData);
            }
        }
    }

    // Determines whether a country can be selected (has any EV share data)
    private static bool IsSelectable(CountryAdoptionSnapshot cs) => cs != null && (cs.BatteryShare > 0 || cs.PlugInShare > 0);

    /// <summary>
    /// Applies the selected country. Updates insight fields, rebuilds Battery/Plug-in trend series,
    /// computes YoY growth from the most recent two years, regenerates powertrain mix, and updates
    /// the actionable recommendation text.
    /// </summary>
    /// <param name="cs">Country snapshot representing latest-year values for a country.</param>
    public void ApplySelection(CountryAdoptionSnapshot cs)
    {
        if (cs == null) return;
        if (!IsSelectable(cs))
        {
            SelectedRecommendation = "No EV data for this country.";
            return;
        }

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

        // Update selected continent and the tooltip text for the country pie info icon
        SelectedContinent = selectedContinent ?? string.Empty;
        CountryPieInfoTooltip = string.IsNullOrWhiteSpace(SelectedContinent)
            ? "Top countries by EV battery share."
            : $"Top countries in {SelectedContinent} by EV battery share.";

        if (!string.IsNullOrWhiteSpace(selectedContinent))
        {
            var sameContinentLatest = _allData
                .Where(d => d.Year == _latestYear && string.Equals(d.Continent, selectedContinent, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.BatteryShare)
                .ToList();

            TopCountries.Clear();
            foreach (var top in sameContinentLatest.Take(5))
            {
                TopCountries.Add(new TopCountryShare { Country = top.Country, Value = top.BatteryShare });
            }

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

    /// <summary>
    /// Builds the powertrain mix slices for the SelectedMix pie based on the latest values.
    /// Computes the residual "Other" as 100 - (Battery + Plug-in).
    /// </summary>
    private void BuildSelectedMix(double battery, double plugIn)
    {
        SelectedMix.Clear();
        var other = Math.Max(0, 100 - (battery + plugIn));
        SelectedMix.Add(new PowertrainMixSlice { Name = "Battery", Value = RoundPct(battery) });
        SelectedMix.Add(new PowertrainMixSlice { Name = "Plug-in", Value = RoundPct(plugIn) });
        SelectedMix.Add(new PowertrainMixSlice { Name = "Other", Value = RoundPct(other) });
    }

    /// <summary>
    /// Rounds a percentage to two decimals.
    /// </summary>
    private static double RoundPct(double v) => Math.Round(v, 2);

    /// <summary>
    /// Produces a simple recommendation message based on absolute EV share and short-term growth.
    /// </summary>
    private string BuildRecommendation(double battery, double plugIn, double growth)
    {
        if (battery >= 60) return "EV market is mature: Focus on infrastructure and fast charging.";
        if (battery >= 30 && growth >= 5) return "Strong growth: Accelerate EV incentives and marketing.";
        if (battery >= 15) return "Moderate adoption: Expand dealer networks and awareness campaigns.";
        if (battery >= 5) return "Early adoption: Pilot programs and partnerships recommended.";
        return "Seed stage: Awareness and education are key.";
    }

    /// <summary>
    /// Parses a numeric value from the CSV using invariant culture.
    /// </summary>
    private static double Parse(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    /// <summary>
    /// Splits a CSV line into fields. Note: the dataset is simple and not quoted.
    /// </summary>
    private static string[] SplitCsv(string line) => line.Split(',');

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// Raises the PropertyChanged event for the provided property name.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Helper to set a backing field and raise PropertyChanged only when the value changes.
    /// </summary>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(name);
        return true;
    }
}