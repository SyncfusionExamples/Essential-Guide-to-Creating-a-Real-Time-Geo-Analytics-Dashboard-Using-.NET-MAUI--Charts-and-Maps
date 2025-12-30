using Syncfusion.Maui.Maps;
using Syncfusion.Maui.Toolkit.BottomSheet;
using System.Threading.Tasks;

namespace GeoAnalyticsDashboard.Views
{
    public partial class MobileDashboardPage : ContentPage
    {
        public MobileDashboardPage()
        {
            InitializeComponent();
            var vm = new MainPageViewModel();
            BindingContext = vm;
            MobileShapeLayer.ShapesSource = MapSource.FromResource("GeoAnalyticsDashboard.Resources.Raw.world-map.json");

            // Dynamically keep legend on one centered row and wrap only when out of space.
            CountryPieChart.SizeChanged += (s, e) =>
            {
                var legendPanel = this.FindByName<FlexLayout>("CountryLegendPanel");
                if (legendPanel != null && CountryPieChart != null)
                {
                    // Subtract a small margin so wrap triggers only when truly needed
                    legendPanel.WidthRequest = Math.Max(0, CountryPieChart.Width - 24);
                }
            };

            ContinentPieChart.SizeChanged += (s, e) =>
            {
                var legendPanel = this.FindByName<FlexLayout>("ContinentLegendPanel");
                if (legendPanel != null && ContinentPieChart != null)
                {
                    // Subtract a small margin so wrap triggers only when truly needed
                    legendPanel.WidthRequest = Math.Max(0, ContinentPieChart.Width - 24);
                }
            };
        }

        private void MapShapeLayer_SelectionChanged(object sender, ShapeSelectedEventArgs e)
        {
            if (BindingContext is MainPageViewModel vm && e.DataItem is CountryAdoptionSnapshot cs)
            {
                // Block selection for countries with no EV data (both shares are zero)
                if (cs.BatteryShare <= 0 && cs.PlugInShare <= 0)
                {
                    return;
                }

                vm.ApplySelection(cs);

                if (DetailsSheet != null)
                {
                    DetailsSheet.Show();
                }
            }
        }
    }
}
