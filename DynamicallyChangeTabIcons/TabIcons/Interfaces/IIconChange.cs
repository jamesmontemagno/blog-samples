using System;
namespace TabIcons.Interfaces
{
    public interface IIconChange
    {
        bool IsSelected { get; set; }
        string CurrentIcon { get; }
    }
}
