﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Autofac;
using VLC_WinRT.Commands.Navigation;
using VLC_WinRT.Helpers;
using VLC_WinRT.Commands;
using VLC_WinRT.Model.Search;
using VLC_WinRT.Services.RunTime;
using Panel = VLC_WinRT.Model.Panel;
using Windows.UI.Popups;
using VLC_WinRT.Model;
using libVLCX;
using VLC_WinRT.Utils;
using WinRTXamlToolkit.Controls.Extensions;
using VLC_WinRT.Views.UserControls;
using Windows.UI.Xaml;
using VLC_WinRT.Views.MusicPages;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using VLC_WinRT.UI.Legacy.Views.UserControls;

namespace VLC_WinRT.ViewModels
{
    public class MainVM : BindableBase
    {
        #region private fields
        private ObservableCollection<Panel> _panels = new ObservableCollection<Panel>();
        #endregion
        #region private props
        private NetworkListenerService networkListenerService;
        private KeyboardListenerService keyboardListenerService;
        private Panel _currentPanel;
        private bool _isInternet;
        private string _searchTag = "";
        private bool _preventAppExit = false;
        private string _informationText;
        private bool _isBackground = false;
        private Thickness _titleBarMargin;

        // Navigation props
        private VLCPage currentPage;
        private bool canGoBack;
        #endregion

        #region public props
        public Panel CurrentPanel
        {
            get { return _currentPanel; }
            set
            {
                SetProperty(ref _currentPanel, value);
#if WINDOWS_PHONE_APP
            var iPreviousView = App.RootPage.ShellContent.CurrentViewIndex;
            var iNewView = Locator.MainVM.Panels.IndexOf(panel);
            App.RootPage.ShellContent.SetPivotAnimation(iNewView > iPreviousView);
#endif
                Locator.NavigationService.Go(value.Target);
            }
        }

        public bool CanGoBack
        {
            get
            {
                return canGoBack;
            }
            set
            {
                SetProperty(ref canGoBack, value);
                OnPropertyChanged(nameof(IsMainBackButtonVisible));
            }
        }

        public bool IsMainBackButtonVisible => CanGoBack && !Locator.NavigationService.IsFlyout(currentPage);

        public KeyboardListenerService KeyboardListenerService { get { return keyboardListenerService; } }
        public bool IsInternet
        {
            get { return _isInternet; }
            set
            {
                InformationText = !value ? Strings.NoInternetConnection : "";
                SetProperty(ref _isInternet, value);
            }
        }

        public GoBackCommand GoBackCommand { get; } = new GoBackCommand();
        
        public ActionCommand GoToSettingsPageCommand { get; } = new ActionCommand(() => Locator.NavigationService.Go(VLCPage.SettingsPage));

        public ActionCommand GoToThanksPageCommand { get; } = new ActionCommand(() => Locator.NavigationService.Go(VLCPage.SpecialThanksPage));

        public ActionCommand GoToLicensePageCommand { get; } = new ActionCommand(() => Locator.NavigationService.Go(VLCPage.LicensePage));

        public ActionCommand GotoSearchPageCommand { get; } = new ActionCommand(() => Locator.NavigationService.Go(VLCPage.SearchPage));

        public ActionCommand GoToFeedbackPageCommand { get; } = new ActionCommand(() => Locator.NavigationService.Go(VLCPage.FeedbackPage));
        public ActionCommand GoToStreamPanel { get; } = new ActionCommand(() => Locator.MainVM.OpenStreamFlyout());

        public ChangeMainPageVideoViewCommand ChangeMainPageVideoViewCommand { get; } = new ChangeMainPageVideoViewCommand();

        public CreateMiniPlayerView CreateMiniPlayerView { get; } = new CreateMiniPlayerView();

        public DisplayMenuBarControlToggleCommand DisplayMenuBarControlToggleCommand { get; } = new DisplayMenuBarControlToggleCommand();

        public ScrollDetectedCommand ScrollDetectedCommand { get; } = new ScrollDetectedCommand();

        public bool PreventAppExit
        {
            get { return _preventAppExit; }
            set { SetProperty(ref _preventAppExit, value); }
        }

        public string InformationText
        {
            get { return _informationText; }
            set { SetProperty(ref _informationText, value); }
        }

        public bool IsBackground
        {
            get { return _isBackground; }
            private set { SetProperty(ref _isBackground, value); }
        }

        #endregion


        public MainVM()
        {
            keyboardListenerService = App.Container.Resolve<KeyboardListenerService>();
            networkListenerService = App.Container.Resolve<NetworkListenerService>();
            networkListenerService.InternetConnectionChanged += networkListenerService_InternetConnectionChanged;
            _isInternet = NetworkListenerService.IsConnected;

            Panels.Add(new Panel("home", VLCPage.LeftSidebar, App.Current.Resources["MenuOpenRight"].ToString(), App.Current.Resources["MenuOpenRight"].ToString()));
            Panels.Add(new Panel(Strings.Videos, VLCPage.MainPageVideo, App.Current.Resources["VideoSymbol"].ToString(), App.Current.Resources["VideoFilledSymbol"].ToString()));
            Panels.Add(new Panel(Strings.Music, VLCPage.MainPageMusic, App.Current.Resources["MusicSymbol"].ToString(), App.Current.Resources["MusicFilledSymbol"].ToString()));
            Panels.Add(new Panel(Strings.FileExplorer, VLCPage.MainPageFileExplorer, App.Current.Resources["FileExplorerSymbol"].ToString(), App.Current.Resources["FileExplorerFilledSymbol"].ToString()));
            Panels.Add(new Panel(Strings.Network, VLCPage.MainPageNetwork, App.Current.Resources["StreamSymbol"].ToString(), App.Current.Resources["StreamFilledSymbol"].ToString()));

            CoreWindow.GetForCurrentThread().Activated += ApplicationState_Activated;
            Locator.NavigationService.ViewNavigated += (sender, page) =>
            {
                var appView = ApplicationView.GetForCurrentView();
                if (page != VLCPage.VideoPlayerPage)
                {
                    appView.Title = "";
                }
                else
                {
                    var title = Locator.VideoPlayerVm?.CurrentVideo?.Name;
                    if (!string.IsNullOrEmpty(title))
                        appView.Title = title;
                }
                if (!App.SplitShell.IsTopBarOpen)
                    App.SplitShell.ShowTopBar();
                if (App.SplitShell.FooterContent == null)
                    App.SplitShell.FooterContent = new BottomMiniPlayer();
                if (App.SplitShell.TitleBarContent == null)
                    App.SplitShell.TitleBarContent = new TitleBar();
                if (App.SplitShell.SplitPaneContent == null)
                    App.SplitShell.SplitPaneContent = new SideBar();
                CanGoBack = Locator.NavigationService.CanGoBack();
            };
            InitializeSlideshow();
        }

        private async void InitializeSlideshow()
        {
            await Locator.Slideshow.IsLoaded.Task;
            Locator.Slideshow.RichAnimations = Locator.SettingsVM.RichAnimations;
        }

        private void ApplicationState_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                IsBackground = true;
                if (Locator.MediaPlaybackViewModel.CurrentMedia == null) return;
                if (!Locator.MediaPlaybackViewModel.IsPlaying) return;
                // If we're playing a video, just pause.
                if (Locator.MediaPlaybackViewModel.PlayingType == PlayingType.Video)
                {
                    // TODO: Route Video Player calls through Media Service
                    if (!Locator.SettingsVM.ContinueVideoPlaybackInBackground)
                    {
                        Locator.MediaPlaybackViewModel._mediaService.Pause();
                    }
                }
            }
            else
            {
                IsBackground = false;
            }
        }

        async void networkListenerService_InternetConnectionChanged(object sender, Model.Events.InternetConnectionChangedEventArgs e)
        {
            await App.Dispatcher?.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                IsInternet = e.IsConnected;
                if (!IsInternet)
                {
                    if (Locator.MediaPlaybackViewModel?.IsPlaying == true && Locator.MediaPlaybackViewModel.IsStream)
                    {
                        var lostStreamDialog = new MessageDialog(Strings.ConnectionLostPleaseCheck, Strings.Sorry);
                        await lostStreamDialog.ShowAsyncQueue();
                    }
                }
            });
        }

        public void OpenStreamFlyout()
        {
            Locator.NavigationService.Go(VLCPage.MainPageNetwork);
        }

        public ObservableCollection<Panel> Panels
        {
            get { return _panels; }
            set
            {
                SetProperty(ref _panels, value);
            }
        }
    }
}