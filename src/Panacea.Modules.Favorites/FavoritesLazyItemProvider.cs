using Panacea.ContentControls;
using Panacea.Core;
using Panacea.Models;
using Panacea.Modularity.Favorites;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Modules.Favorites
{
    public class FavoritesLazyItemProvider : ILazyItemProvider, INotifyPropertyChanged
    {
        public event EventHandler Refreshed;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private readonly int _pageSize;
        private readonly PanaceaServices _core;
        int _currentPage = 1;

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                if (string.IsNullOrEmpty(Search))
                {
                    GetItemsAsync();
                }
                else
                {
                    SearchAsync(Search);
                }
            }
        }

        int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy => false;

        string _search;
        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                OnPropertyChanged();
                CurrentPage = 1;
            }
        }

        List<ServerItem> _items;
        public List<ServerItem> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged();
            }
        }

        ServerGroupItem _selectedCategory;
        public ServerGroupItem SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                TotalPages = 1;
                CurrentPage = 1;
            }
        }

        List<ServerGroupItem> _categories;
        public List<ServerGroupItem> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public FavoritesLazyItemProvider(PanaceaServices core, int pageSize)
        {
            _pageSize = pageSize;
            _core = core;
        }

        public virtual Task<List<ServerGroupItem>> GetCategoriesAsync()
        {

            return Task.FromResult(_core.PluginLoader.LoadedPlugins.Where((obj) => obj.Value is IHasFavoritesPlugin).Select((obj) => new ServerGroupItem { Id = obj.Key, Name = obj.Key }).ToList());
        }

        public virtual async Task GetItemsAsync()
        {
            var items = (_core.PluginLoader.LoadedPlugins[SelectedCategory.Id] as IHasFavoritesPlugin).Favorites;
            TotalPages = (int)Math.Ceiling(items.Count / (double)_pageSize);
            Items = items.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
        }

        public Task<List<ServerItem>> SearchAsync(string wildcard)
        {
            return Task.FromResult((_core.PluginLoader.LoadedPlugins[SelectedCategory.Id] as IHasFavoritesPlugin)
                .Favorites
                .Where((obj) => obj.Name.ToLower().Contains(wildcard.ToLower())).ToList());
        }

        public void Refresh()
        {
            Refreshed?.Invoke(this, EventArgs.Empty);
        }


        public async Task Initialize()
        {
            Categories = await GetCategoriesAsync();
            if (Categories.Count > 0)
                SelectedCategory = Categories[0];
        }
    }
}
