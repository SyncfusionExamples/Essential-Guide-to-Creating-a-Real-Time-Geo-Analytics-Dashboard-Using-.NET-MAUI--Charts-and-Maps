using Syncfusion.Maui.Maps;

namespace GeoAnalyticsDashboard.Views
{
    public partial class DesktopDashboardPage : ContentPage
    {
        public DesktopDashboardPage()
        {
            InitializeComponent();
            var vm = new MainPageViewModel();
            BindingContext = vm;
            ShapeLayer.ShapesSource = MapSource.FromResource("GeoAnalyticsDashboard.Resources.Raw.world-map.json");

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
        }

        private void MapShapeLayer_SelectionChanged(object sender, ShapeSelectedEventArgs e)
        {
            if (BindingContext is MainPageViewModel vm && e.DataItem is CountryAdoptionSnapshot cs)
            {
                vm.ApplySelection(cs);
            }
        }
    }
}