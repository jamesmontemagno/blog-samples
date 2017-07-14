using System;
using MvvmHelpers;
using TabIcons.Interfaces;

namespace TabIcons.ViewModel
{
    public class Tab1ViewModel : BaseViewModel, IIconChange
    {
        
        public Tab1ViewModel()
        {
            Title = "Tab1";
        }

        bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (SetProperty(ref isSelected, value))
                    OnPropertyChanged(nameof(CurrentIcon));
            }
        }
        public string CurrentIcon
        {
            get => IsSelected ? "tab_target.png" : "tab_chat.png";
        }
    }
}
