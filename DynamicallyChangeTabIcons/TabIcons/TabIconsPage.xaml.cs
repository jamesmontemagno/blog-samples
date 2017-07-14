using Xamarin.Forms;
using TabIcons.ViewModel;


namespace TabIcons
{
    public partial class TabIconsPage : ContentPage
    {
        public TabIconsPage()
        {
            InitializeComponent();

            BindingContext = new Tab1ViewModel()
            {
                IsSelected = true
            };

        }
    }
}
