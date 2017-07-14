using Xamarin.Forms;
using TabIcons.ViewModel;

namespace TabIcons
{
    public partial class TabIconsPage2 : ContentPage
    {
        public TabIconsPage2()
        {
            InitializeComponent();

            BindingContext = new Tab2ViewModel();

            //this.SetBinding(IconProperty, "CurrentIcon");
        }
    }
}
