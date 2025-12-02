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