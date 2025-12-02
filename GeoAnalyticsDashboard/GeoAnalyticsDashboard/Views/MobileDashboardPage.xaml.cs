using Syncfusion.Maui.Maps;

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
        }

        private void MapShapeLayer_SelectionChanged(object sender, ShapeSelectedEventArgs e)
        {
            if (BindingContext is MainPageViewModel vm && e.DataItem is CountryAdoptionSnapshot cs)
            {
                vm.ApplySelection(cs);

                // Open bottom sheet when a country is tapped (HalfExpandedRatio configured in XAML)
                if (DetailsSheet != null)
                {
                    DetailsSheet.Show();
                }
            }
        }
    }
}
