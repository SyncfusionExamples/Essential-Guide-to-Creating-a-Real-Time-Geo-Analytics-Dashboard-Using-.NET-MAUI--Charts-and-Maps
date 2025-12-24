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
        }

        private void MapShapeLayer_SelectionChanged(object sender, ShapeSelectedEventArgs e)
        {
            if (BindingContext is MainPageViewModel vm && e.DataItem is CountryAdoptionSnapshot cs)
            {
                vm.ApplySelection(cs);

                if (DetailsSheet != null)
                {
                    DetailsSheet.Show();
                }
            }
        }
    }
}
