using Panacea.Core;
using Panacea.Models;
using Panacea.Modularity.Favorites;
using Panacea.Modularity.UiManager;
using Panacea.Modularity.UserAccount;
using Panacea.Multilinguality;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Modules.Favorites
{
    public class FavoritesManager : IFavoritesManager
    {
        PanaceaServices _core;
        Translator _translator;
        string _favoritesResponse;
        private List<PluginFavorites<ServerItem>> cachedFavorites;
        public FavoritesManager(PanaceaServices core)
        {
            _translator = new Translator("Favorites");
            _core = core;
        }
        public event EventHandler FavoritesChanged;
        private void AddToCache(string pluginName, ServerItem item)
        {
            List<ServerItem> Items;
            if (cachedFavorites.Any(p => p.Name == pluginName))
            {
                var pluginFavorites = cachedFavorites.FirstOrDefault(p => p.Name == pluginName);
                pluginFavorites.Items.Add(item);
                Items = pluginFavorites.Items;
            } else
            {
                var pluginFavorites = new PluginFavorites<ServerItem>() { Name = pluginName, Items = new List<ServerItem>() { item } };
                Items = pluginFavorites.Items;
            }
            (_core.PluginLoader.LoadedPlugins.First(o => o.Key == pluginName).Value as IHasFavoritesPlugin).Favorites = Items;
        }
        private void RemoveFromCache(string pluginName, string id)
        {
            if (cachedFavorites.Any(p => p.Name == pluginName))
            {
                var pluginFavorites = cachedFavorites.FirstOrDefault(p => p.Name == pluginName);
                pluginFavorites.Items.Remove(pluginFavorites.Items.First(f => f.Id == id));
                (_core.PluginLoader.LoadedPlugins.First(o => o.Key == pluginName).Value as IHasFavoritesPlugin).Favorites = pluginFavorites.Items;
            }
        }
        private async Task<bool> FavoriteAddAsync(string pluginName, string id)
        {
            try
            {
                ServerResponse res = await _core.HttpClient.GetObjectAsync<object>(
                    "set_favorites/",
                    postData: new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("plugin", pluginName),
                        new KeyValuePair<string, string>("favorite", id),
                    });
                if (!res.Success)
                {
                    throw new Exception(res.Error);
                }
            }
            catch (Exception ex)
            {
                _core.Logger.Error(this, ex.Message);
                return false;
            }
            return true;
        }
        public async Task<bool> FavoriteRemoveAsync(string pluginName, string id)
        {
            try
            {
                ServerResponse res = await _core.HttpClient.GetObjectAsync<object>(
                    "unset_favorites/",
                    postData: new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("plugin", pluginName),
                        new KeyValuePair<string, string>("favorite", id),
                    });
                if (!res.Success)
                {
                    throw new Exception(res.Error);
                }
            }
            catch (Exception ex)
            {
                _core.Logger.Error(this, ex.Message);
                return false;
            }
            return true;
        }
        public void Clear()
        {
            _favoritesResponse = null;
            cachedFavorites = null;
        }

        private async Task Refresh()
        {
            if (_core.UserService.User?.Id != null)
            {
                cachedFavorites = new List<PluginFavorites<ServerItem>>();
                _favoritesResponse = await _core.HttpClient.GetStringAsync("get_favorites/");
                //FavoritesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task UpdateFavorites()
        {
            await Refresh();
            var relevantPlugins = _core.PluginLoader.LoadedPlugins.Where(o => o.Value is IHasFavoritesPlugin);
            foreach (var obj in relevantPlugins)
            {
                var pluginName = obj.Key;
                var plugin = obj.Value as IHasFavoritesPlugin;
                Type t = plugin.GetContentType();
                var res = GetType().GetMethod(nameof(SetPluginFavorites)).MakeGenericMethod(t).Invoke(this, new object[] {plugin, pluginName });
            }
        }

        public void SetPluginFavorites<T>(IHasFavoritesPlugin plugin, string pluginName) where T : ServerItem
        {
            if (_favoritesResponse == null) return;
            var obj = JsonSerializer.DeserializeFromString<ServerResponse<List<PluginFavorites<T>>>>(_favoritesResponse);
            List<ServerItem> itemList;
            if (obj.Result.Any(o => o.Name == pluginName))
            {
                itemList = obj.Result.First(o => o.Name == pluginName).Items.Cast<ServerItem>().ToList();
            }
            else
            {
                itemList = new List<ServerItem>();
            }
            plugin.Favorites = itemList;
            var pf = new PluginFavorites<ServerItem>() { Name = pluginName, Items = itemList };
            cachedFavorites.Add(pf);
        }

        public async Task<bool> AddOrRemoveFavoriteAsync(string pluginName, ServerItem item)
        {
            if (_core.UserService.User.Id == null)
            {
                if (_core.TryGetUserAccountManager(out IUserAccountManager _userAccount))
                {
                    var logged = await _userAccount.RequestLoginAsync(_translator.Translate("You need an account to add favorites"));
                    if (!logged)
                    {
                        return false;
                    }
                }
            }
            if (cachedFavorites.Any(p => p.Name == pluginName))
            {
                var pluginFavs = cachedFavorites.First(p => p.Name == pluginName);
                if (pluginFavs.Items.Any(f => f.Id == item.Id))
                {
                    if(await FavoriteRemoveAsync(pluginName, item.Id))
                    {
                        RemoveFromCache(pluginName, item.Id);
                        if (_core.TryGetUiManager(out IUiManager _uiManager)){
                            _uiManager.Toast(_translator.Translate("This item has been removed from your favorites"));
                        }
                        return true;
                    }
                    else
                    {
                        if (_core.TryGetUiManager(out IUiManager _ui))
                        {
                            _ui.Toast(_translator.Translate("Something went wrong when trying to remove from your favorites"));
                        }
                        return false;
                    }
                }
            }
            if(await FavoriteAddAsync(pluginName, item.Id))
            {
                AddToCache(pluginName, item);
                if (_core.TryGetUiManager(out IUiManager _ui))
                {
                    _ui.Toast(_translator.Translate("This item has been added to your favorites"));
                }
                return true;
            }
            else
            {
                if (_core.TryGetUiManager(out IUiManager _ui))
                {
                    _ui.Toast(_translator.Translate("Something went wrong when trying to add to your favorites"));
                }
                return false;
            }
        }
    }
}
