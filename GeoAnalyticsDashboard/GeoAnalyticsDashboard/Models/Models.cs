
namespace GeoAnalyticsDashboard
{
    public class EvAdoptionRecord
    {
        public string Country { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Year { get; set; }
        public double PlugInShare { get; set; }
        public double BatteryShare { get; set; }
    }

    public class CountryAdoptionSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public double BatteryShare { get; set; }
        public double PlugInShare { get; set; }
    }

    public class TopCountryShare
    {
        public string Country { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class YearlySharePoint
    {
        public string Year { get; set; } = string.Empty;
        public double Value { get; set; }
    }
    public class PowertrainMixSlice
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
    }

}
