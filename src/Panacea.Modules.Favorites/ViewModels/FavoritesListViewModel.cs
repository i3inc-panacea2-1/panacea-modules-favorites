using Panacea.ContentControls;
using Panacea.Controls;
using Panacea.Core;
using Panacea.Models;
using Panacea.Modularity.Content;
using Panacea.Modularity.Favorites;
using Panacea.Modularity.UiManager;
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
    [View(typeof(FavoritesList))]
    public class FavoritesListViewModel : ViewModelBase
    {
        public ILazyItemProvider Provider { get; private set; }

        private List<ServerGroupItem> _lazyCustomCategories;
        public List<ServerGroupItem> LazyCustomCategories
        {
            get => _lazyCustomCategories; set
            {
                _lazyCustomCategories = value;
                OnPropertyChanged();
            }
        }
        private readonly PanaceaServices _core;
        private readonly FavoritesPlugin _plugin;

        public FavoritesListViewModel(PanaceaServices core, FavoritesPlugin plugin)
        {
            Provider = new FavoritesLazyItemProvider(core, 10);
            _core = core;
            _plugin = plugin;
            SetupCommands();
            SetupCategories();
        }
        private void SetupCommands()
        {
            FavoriteClickCommand = new AsyncCommand(async (args) =>
            {
                var arr = args as object[];
                if (Provider.SelectedCategory == null) return;
                var pluginName = Provider.SelectedCategory.Id;
                var plg = _core.PluginLoader.LoadedPlugins[pluginName] as IHasFavoritesPlugin;
                if (plg == null) return;
                var item = arr[0] as ServerItem;
                if (item == null) return;
                if (plg is IUpdatesFavorites)
                {
                    (plg as IUpdatesFavorites).UpdateFavorites();
                }
                else
                {
                    await _plugin.GetManager().AddOrRemoveFavoriteAsync(pluginName, item);
                }
                Provider.Refresh();
                //(arr[1] as ICommand)?.Execute(null);
            });
            ItemClickCommand = new RelayCommand(async (arg) =>
            {
                if (_plugin == null) return;
                var pluginName = Provider.SelectedCategory.Id;
                var plg = _core.PluginLoader.LoadedPlugins[pluginName] as IHasFavoritesPlugin;
                if (plg == null) return;
                var contentPlugin = (plg as IContentPlugin);
                if (contentPlugin != null)
                {
                    await contentPlugin.OpenItemAsync(arg as ServerItem);
                }
            });
        }
        private void SetupCategories()
        {
            if (_plugin == null) return;
            var cats = new List<ServerGroupItem>();
            foreach (var plg in _core.PluginLoader.LoadedPlugins)
            {
                try
                {
                    var name = plg.Value.GetType().GetCustomAttributes(typeof(FriendlyNameAttribute), true).Length > 0 ?
                            ((FriendlyNameAttribute)plg.Value.GetType().GetCustomAttributes(typeof(FriendlyNameAttribute), true)[0]).Name : plg.Key;
                    if (plg.Value is IHasFavoritesPlugin)
                    {
                        var gi = new ServerGroupItem()
                        {
                            Name = name,
                            Id = plg.Key
                        };
                        cats.Add(gi);
                    }
                }
                catch
                {
                    var gi = new ServerGroupItem()
                    {
                        Name = plg.Key,
                        Id = plg.Key
                    };
                    cats.Add(gi);
                }
            }
            if (cats.Count > 0)// cats[0].IsChecked = true;
                Provider.SelectedCategory = cats[0];
            LazyCustomCategories = cats;
        }
        public ICommand ItemClickCommand { get; protected set; }
        public ICommand SearchCommand { get; protected set; }
        public AsyncCommand FavoriteClickCommand { get; protected set; }
        public ICommand RefreshCommand { get; protected set; }
    }
}
