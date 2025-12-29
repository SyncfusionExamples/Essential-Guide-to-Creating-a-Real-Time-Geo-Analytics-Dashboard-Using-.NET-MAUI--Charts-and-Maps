namespace GeoAnalyticsDashboard
{
    /// <summary>
    /// Raw EV adoption record parsed from the CSV file. Represents a country's metrics for a given year.
    /// </summary>
    public class EvAdoptionRecord
    {
        /// <summary>Country display name (e.g., "Norway").</summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>Two/three letter code used in shape-data mapping.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Calendar year of the observation.</summary>
        public int Year { get; set; }

        /// <summary>Plug-in hybrid market share (% of car sales).</summary>
        public double PlugInShare { get; set; }

        /// <summary>Battery electric vehicle market share (% of car sales).</summary>
        public double BatteryShare { get; set; }

        /// <summary>Continent name for aggregation (e.g., Europe, Asia).</summary>
        public string? Continent { get; set; }
    }

    /// <summary>
    /// Latest-year snapshot per country, used by the map and detail card selections.
    /// </summary>
    public class CountryAdoptionSnapshot
    {
        /// <summary>Country display name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Battery EV share (%) for the latest year.</summary>
        public double BatteryShare { get; set; }

        /// <summary>Plug-in hybrid share (%) for the latest year.</summary>
        public double PlugInShare { get; set; }

        public string TooltipText { get; set; }
    }

    /// <summary>
    /// Model backing the Top 5 countries pie chart.
    /// </summary>
    public class TopCountryShare
    {
        /// <summary>Country display name.</summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>Value used for ranking/charting (Battery share %).</summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// Aggregated share per continent for the continent-level pie chart.
    /// </summary>
    public class ContinentShare
    {
        /// <summary>Continent name.</summary>
        public string Continent { get; set; } = string.Empty;

        /// <summary>Aggregated value (e.g., average Battery share %) used for charting.</summary>
        public double Value { get; set; }

        /// <summary>Share of this continent relative to the sum of all continent values (0-100).</summary>
        public double Percentage { get; set; }

        /// <summary>Formatted label like "Europe 12.39 %" for pie data label.</summary>
        public string DisplayLabel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Point in the yearly trend series for charts.
    /// </summary>
    public class YearlySharePoint
    {
        /// <summary>Year label shown on the X axis.</summary>
        public double Year { get; set; }

        /// <summary>Share value (%) for the series.</summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// Slice used in the radial bar chart to show powertrain mix for the selected country.
    /// </summary>
    public class PowertrainMixSlice
    {
        /// <summary>Slice label (Battery, Plug-in, Other).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Slice value in percent.</summary>
        public double Value { get; set; }
    }
}