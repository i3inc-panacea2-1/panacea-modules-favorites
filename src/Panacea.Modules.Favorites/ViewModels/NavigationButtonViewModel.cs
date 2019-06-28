using Panacea.Controls;
using Panacea.Modules.Favorites.Views;
using Panacea.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Panacea.Modules.Favorites.ViewModels
{
    [View(typeof(NavigationButton))]
    class NavigationButtonViewModel : ViewModelBase
    {
        public NavigationButtonViewModel(FavoritesPlugin plugin)
        {
            ClickCommand = new RelayCommand(args =>
            {
                plugin.Call();
            });
        }
        public ICommand ClickCommand { get; }
    }
}
