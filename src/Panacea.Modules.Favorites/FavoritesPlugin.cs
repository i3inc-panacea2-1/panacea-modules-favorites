using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Panacea.Core;
using Panacea.Models;
using Panacea.Modularity.Favorites;
using Panacea.Modularity.UiManager;
using Panacea.Modularity.UserAccount;
using Panacea.Modules.Favorites.ViewModels;
using Panacea.Multilinguality;

namespace Panacea.Modules.Favorites
{
    public class FavoritesPlugin : IFavoritesPlugin, ICallablePlugin
    {
        public event EventHandler FavoritesChanged;
        PanaceaServices _core;
        private Translator _translator;
        private FavoritesManager _manager;
        NavigationButtonViewModel _navButton;
        public FavoritesPlugin(PanaceaServices core)
        {
            _core = core;
            _core.UserService.UserLoggedIn += UserService_UserLoggedIn;
            _core.UserService.UserLoggedOut += UserService_UserLoggedOut;
            _translator = new Translator("Favorites");
            _manager = new FavoritesManager(_core);
        }
        public IFavoritesManager GetManager()
        {
            return _manager;
        }
        private async Task UserService_UserLoggedIn(IUser user)
        {
            await _manager.UpdateFavorites();
            return;
        }
        private async Task UserService_UserLoggedOut(IUser user)
        {
            _manager.Clear();
            return;
        }
        public Task BeginInit()
        {
            return Task.CompletedTask;
        }
        public async Task EndInit()
        {
            if (_core.UserService.User.Id != null)
            {
                try
                {
                    await _manager.UpdateFavorites();
                }
                catch (Exception e)
                {
                    _core.Logger.Error(this, e.Message);
                }
            }
            if(_core.TryGetUiManager(out IUiManager ui))
            {
                _navButton = new NavigationButtonViewModel();
                ui.AddNavigationBarControl(_navButton);
            }
        }

        public void Dispose()
        {
            return;
        }
        public Task Shutdown()
        {
            if (_navButton != null && _core.TryGetUiManager(out IUiManager ui))
            {
                ui.RemoveMainPageControl(_navButton);
            }
            return Task.CompletedTask;
        }
        public async void Call()
        {
            if (_core.UserService.User.Id == null)
            {
                if (_core.TryGetUserAccountManager(out IUserAccountManager userManager)
                    && !await userManager.RequestLoginAsync(_translator.Translate("You must create an account to view favorites")))
                {
                    return;
                }
            }
            if (_core.TryGetUiManager(out IUiManager ui))
            {
                ui.Navigate(new FavoritesListViewModel(_core, this));
            }
        }
    }
}