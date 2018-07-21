using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using CardIdleRemastered.Commands;
using CardIdleRemastered.ViewModels;

namespace CardIdleRemastered
{
    public class AccountModel : ObservableModel, IDisposable
    {
        #region Fields
        private readonly AccountUpdater _updater;
        private readonly PricesUpdater _pricesUpdater;
        private readonly IdleManager _idler;
        private readonly ShowcaseManager _showcaseManager;

        private DispatcherTimer _tmSteamStatus;
        private string _syncTime;

        private string _userName;
        private string _level;
        private string _avatarUrl;
        private string _backgroundUrl;
        private string _customBackgroundUrl;
        private BadgeLevelData _favoriteBadge;

        private bool _isAuthorized;
        private bool _showInTaskbar = true;
        private bool _showBackground = true;
        private int _activeProcessCount;
        private int _totalCards;
        private int _totalGames;

        private string _storePageUrl;
        private BadgeModel _selectedGame;

        private string _gameTitle;
        private readonly ICollectionView _badges;

        private bool _allowShowcaseSync;
        private string _showcaseTitle;
        private readonly ICollectionView _showcases;

        private CardIdleProfileInfo _cardIdleProfile;

        private ReleaseInfo _newestRelease;
        private bool _canUpdateApp;

        #endregion

        public AccountModel()
        {
            _pricesUpdater = new PricesUpdater();
            _updater = new AccountUpdater(this, _pricesUpdater);
            _idler = new IdleManager(this);
            _showcaseManager = new ShowcaseManager();
            _updater.BadgeListSync += SyncShowcases;

            AllBadges = new ObservableCollection<BadgeModel>();
            IdleQueueBadges = new ObservableCollection<BadgeModel>();
            Games = new ObservableCollection<BadgeModel> { new BadgeModel("-1", "new", "0", "0") };
            AllShowcases = new ObservableCollection<BadgeShowcase>();

            #region Commands
            LoginCmd = new BaseCommand(_ => Login());
            LogoutCmd = new BaseCommand(_ => Logout());

            StartBadgeIdleCmd = new BaseCommand(StartBadgeIdle, CanStartBadgeIdle);
            StopBadgeIdleCmd = new BaseCommand(StopBadgeIdle, CanStopBadgeIdle);
            BlacklistBadgeCmd = new BaseCommand(BlacklistBadge);
            ForceSyncCmd = new BaseCommand(ForceSync);

            EnqueueAllCmd = new BaseCommand(EnqueueAll);
            DequeueAllCmd = new BaseCommand(_ => DequeueAll());
            SetHigherPriorityCmd = new BaseCommand(SetHigherPriority, CanSetHigherPriority);
            SetLowerPriorityCmd = new BaseCommand(SetLowerPriority, CanSetLowerPriority);

            EnqueueBadgeHighCmd = new BaseCommand(EnqueueBadgeHigh, CanEnqueueBadge);
            EnqueueBadgeLowCmd = new BaseCommand(EnqueueBadgeLow, CanEnqueueBadge);
            DequeueBadgeCmd = new BaseCommand(DequeueBadge, b => !CanEnqueueBadge(b));

            IdleCmd = new BaseCommand(Idle, CanIdle);

            AddGameCmd = new BaseCommand(o => AddGame());
            RemoveGameCmd = new BaseCommand(RemoveGame);

            BookmarkShowcaseCmd = new BaseCommand(BookmarkShowcase);
            #endregion

            _badges = CollectionViewSource.GetDefaultView(AllBadges);
            var quick = (ICollectionViewLiveShaping)_badges;
            quick.LiveFilteringProperties.Add("IsBlacklisted");
            quick.LiveFilteringProperties.Add("HasTrial");
            quick.LiveFilteringProperties.Add("CardIdleActive");
            quick.LiveFilteringProperties.Add("IsInQueue");
            quick.IsLiveFiltering = true;

            BadgePropertiesFilters = FilterStatesCollection.Create<BadgeProperty>().SetNotifier(SetFilter);

            _showcases = CollectionViewSource.GetDefaultView(AllShowcases);
            quick = (ICollectionViewLiveShaping)_showcases;
            quick.LiveFilteringProperties.Add("IsCompleted");
            quick.LiveFilteringProperties.Add("IsBookmarked");
            quick.IsLiveFiltering = true;

            ShowcasePropertiesFilters = FilterStatesCollection.Create<ShowcaseProperty>().SetNotifier(SetShowcaseFilter);
        }

        public ISettingsStorage Storage { get; set; }

        #region Account Properties
        public string UserName
        {
            get { return String.IsNullOrWhiteSpace(_userName) ? "Card Idle" : _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        public bool IsAuthorized
        {
            get { return _isAuthorized; }
            set
            {
                _isAuthorized = value;
                OnPropertyChanged();
                OnPropertyChanged("IsUnknown");
            }
        }

        public bool IsUnknown { get { return !IsAuthorized; } }

        public bool IsSteamRunning
        {
            get
            {
                try
                {
                    return Steamworks.SteamAPI.IsSteamRunning() || IgnoreClient;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ShowInTaskbar
        {
            get { return _showInTaskbar; }
            set
            {
                _showInTaskbar = value;
                OnPropertyChanged();
            }
        }

        public bool ShowBackground
        {
            get { return _showBackground; }
            set
            {
                _showBackground = value;
                OnPropertyChanged();
                OnPropertyChanged("BackgroundUrl");
            }
        }

        public IdleManager Idler
        {
            get { return _idler; }
        }

        public int ActiveProcessCount
        {
            get { return _activeProcessCount; }
            private set
            {
                if (_activeProcessCount == value)
                    return;
                _activeProcessCount = value;
                OnPropertyChanged();
                OnPropertyChanged("IsCardIdleActive");
            }
        }

        public bool IsCardIdleActive
        {
            get { return ActiveProcessCount > 0; }
        }

        #region Profile

        public string ProfileUrl { get; set; }

        public string AvatarUrl
        {
            get { return String.IsNullOrWhiteSpace(_avatarUrl) ? "../Resources/Avatar.png" : _avatarUrl; }
            set
            {
                _avatarUrl = value;
                OnPropertyChanged();
            }
        }

        public string BackgroundUrl
        {
            get
            {
                if (false == ShowBackground)
                    return null;
                if (String.IsNullOrWhiteSpace(_customBackgroundUrl) == false)
                    return _customBackgroundUrl;
                return _backgroundUrl;
            }
            set
            {
                _backgroundUrl = value;
                OnPropertyChanged();
            }
        }

        public string CustomBackgroundUrl
        {
            get { return _customBackgroundUrl; }
            set
            {
                _customBackgroundUrl = value;
                OnPropertyChanged();
                OnPropertyChanged("BackgroundUrl");
            }
        }

        public string Level
        {
            get { return String.IsNullOrWhiteSpace(_level) ? "0" : _level; }
            set
            {
                _level = value;
                OnPropertyChanged();
            }
        }

        public BadgeLevelData FavoriteBadge
        {
            get { return _favoriteBadge; }
            set
            {
                _favoriteBadge = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public ObservableCollection<BadgeModel> AllBadges { get; private set; }

        public ObservableCollection<BadgeModel> IdleQueueBadges { get; private set; }

        public ObservableCollection<BadgeModel> Games { get; private set; }

        public ICollectionView Badges { get { return _badges; } }

        public int TotalCards
        {
            get { return _totalCards; }
            set
            {
                _totalCards = value;
                OnPropertyChanged();
            }
        }

        public int TotalGames
        {
            get { return _totalGames; }
            set
            {
                _totalGames = value;
                OnPropertyChanged();
            }
        }

        public bool IgnoreClient { get; set; }

        public string SyncTime
        {
            get { return _syncTime; }
            set
            {
                _syncTime = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public void AddBadge(BadgeModel badge)
        {
            AllBadges.Add(badge);
            badge.PropertyChanged += BadgeIdleStatusChanged;
        }

        public void RemoveBadge(BadgeModel badge)
        {
            badge.RemainingCard = 0;
            AllBadges.Remove(badge);
            IdleQueueBadges.Remove(badge);
            badge.PropertyChanged -= BadgeIdleStatusChanged;
        }

        private void BadgeIdleStatusChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CardIdleActive")
                CheckIdleStatus();
        }

        #region Initialization

        private string _currentVersion = "1.0";
        public string CurrentVersion
        {
            get { return _currentVersion; }
            private set
            {
                _currentVersion = value;
                OnPropertyChanged();
            }
        }

        public ReleaseInfo NewestRelease
        {
            get { return _newestRelease; }
            set
            {
                _newestRelease = value;
                OnPropertyChanged();
            }
        }

        public bool CanUpdateApp
        {
            get { return _canUpdateApp; }
            set
            {
                _canUpdateApp = value;
                OnPropertyChanged();
            }
        }

        public async void CheckLatestRelease()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            CurrentVersion = version.Major + "." + version.Minor + "." + version.Build;

            NewestRelease = await new SteamParser().GetLatestCardIdlerRelease();
            CanUpdateApp = NewestRelease.IsOlderThan(version);
        }


        public CardIdleProfileInfo CardIdleProfile
        {
            get { return _cardIdleProfile; }
            set
            {
                _cardIdleProfile = value;
                OnPropertyChanged();
            }
        }

        public async void LoadCardIdleProfile()
        {
            CardIdleProfile = await new SteamParser().LoadCardIdleProfileAsync();
        }

        public FileStorage PricesStorage
        {
            get { return _pricesUpdater.Storage; }
            set { _pricesUpdater.Storage = value; }
        }

        public void Startup()
        {
            InitSteamTimer();
            CheckLatestRelease();
            LoadCardIdleProfile();
            DownloadPricesCatalog();
            LoadAccount();
        }

        /// <summary>
        /// Initialize timer to regularly check Steam client status
        /// </summary>
        public void InitSteamTimer()
        {
            _tmSteamStatus = new DispatcherTimer();
            _tmSteamStatus.Interval = new TimeSpan(0, 0, 5);
            bool steamRunning = false;
            _tmSteamStatus.Tick += (sender, args) =>
            {
                bool connected = IsSteamRunning;
                if (steamRunning != connected)
                    OnPropertyChanged("IsSteamRunning");
                steamRunning = connected;
            };
            _tmSteamStatus.Start();
        }

        public async void DownloadPricesCatalog()
        {
            var dt = DateTime.Today;
            int dayNum = dt.Year * 10000 + dt.Month * 100 + dt.Day;
            if (dayNum > Storage.PricesCatalogDate)
            {
                bool success = await _pricesUpdater.DownloadCatalog();
                if (success)
                {
                    Storage.PricesCatalogDate = dayNum;
                    Storage.Save();
                }
            }
        }

        /// <summary>
        /// Load account when application starts
        /// </summary>
        public async void LoadAccount()
        {
            if (String.IsNullOrWhiteSpace(Storage.SteamProfileUrl) == false)
            {
                // restore account data from Settings
                UserName = Storage.SteamUserName;
                Level = Storage.SteamLevel;

                AvatarUrl = Storage.SteamAvatarUrl;

                BackgroundUrl = Storage.SteamBackgroundUrl;

                FavoriteBadge = new BadgeLevelData
                                {
                                    PictureUrl = Storage.SteamBadgeUrl,
                                    Name = Storage.SteamBadgeTitle,
                                };
            }

            CustomBackgroundUrl = Storage.CustomBackgroundUrl;
            BadgePropertiesFilters.Deserialize<BadgeProperty>(Storage.BadgeFilter);
            ShowcasePropertiesFilters.Deserialize<ShowcaseProperty>(Storage.ShowcaseFilter);

            Idler.Mode = (IdleMode)Storage.IdleMode;

            if (Storage.MaxIdleProcessCount > 0)
                Idler.MaxIdleInstanceCount = Storage.MaxIdleProcessCount;

            if (Storage.PeriodicSwitchRepeatCount > 0)
                Idler.PeriodicSwitchRepeatCount = Storage.PeriodicSwitchRepeatCount;

            if (Storage.TrialPeriod > 0)
                Idler.TrialPeriod = Storage.TrialPeriod;
            else
                Idler.TrialPeriod = 2;

            Idler.SwitchMinutes = Storage.SwitchMinutes;
            if (Storage.SwitchSeconds > 0)
                Idler.SwitchSeconds = Storage.SwitchSeconds;

            IgnoreClient = Storage.IgnoreClient;

            AllowShowcaseSync = Storage.AllowShowcaseSync;
            ShowInTaskbar = Storage.ShowInTaskbar;
            ShowBackground = Storage.ShowBackground;

            PropertyChanged += SaveConfiguration;
            Idler.PropertyChanged += SaveConfiguration;
            Idler.PropertyChanged += TrialPeriodChanged;

            IdleQueueBadges.CollectionChanged += IdleQueueItemsChanged;

            try
            {
                IsAuthorized = await CheckAuth();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Could not authorize");
            }

            if (IsAuthorized)
                await InitProfile();

            // reload games list
            var games = Storage.Games.Cast<string>().ToList();
            int idx = 0;
            foreach (var id in games)
            {
                var game = await new SteamParser().GetGameInfo(id);
                game.PropertyChanged += BadgeIdleStatusChanged;
                Games.Insert(idx, game);
                idx++;
            }
        }

        private async Task<bool> CheckAuth()
        {
            bool registered = !String.IsNullOrWhiteSpace(Storage.SessionId) &&
                              !String.IsNullOrWhiteSpace(Storage.SteamLoginSecure) &&
                              !String.IsNullOrWhiteSpace(Storage.SteamProfileUrl);

            if (registered)
            {
                Logger.Info("Session test");
                registered = await new SteamParser().IsLogined(Storage.SteamProfileUrl);
            }

            if (registered)
                Logger.Info("Welcome back");
            else
                Logger.Info("Login required");

            return registered;
        }

        private async Task InitProfile()
        {
            ProfileUrl = Storage.SteamProfileUrl;

            _updater.Start();

            // load badges and new profile settings
            await _updater.Sync(false);

            // restore queue
            var queue = Storage.IdleQueue.Cast<string>().Distinct()
                .Join(AllBadges, s => s, b => b.AppId, (s, badge) => badge)
                .ToList();
            foreach (var badge in queue)
                EnqueueBadgeLowCmd.Execute(badge);

            // restore blacklist
            var blacklist = Storage.Blacklist.Cast<string>()
                .Join(AllBadges, s => s, b => b.AppId, (s, badge) => badge)
                .ToList();
            foreach (var badge in blacklist)
                badge.IsBlacklisted = true;
        }

        #endregion

        #region Login/Logout commands
        public ICommand LoginCmd { get; private set; }

        private async void Login()
        {
            var w = new BrowserWindow();
            w.Storage = Storage;
            w.ShowDialog();
            IsAuthorized = await CheckAuth();
            if (IsAuthorized)
                await InitProfile();
        }

        public ICommand LogoutCmd { get; private set; }

        /// <summary>
        /// Log out from current account
        /// </summary>
        private void Logout()
        {
            _updater.Stop();
            StopIdle();

            IsAuthorized = false;

            // Clear the account settings
            Storage.SessionId = string.Empty;
            Storage.SteamLoginSecure = string.Empty;
            Storage.SteamParental = string.Empty;
            UserName =
            ProfileUrl =
            Level =
            AvatarUrl =
            BackgroundUrl = null;
            FavoriteBadge = null;
            Storage.IdleMode = 0;
            Storage.BadgeFilter =
            Storage.ShowcaseFilter = string.Empty;

            AllBadges.Clear();
            IdleQueueBadges.Clear();

            Storage.Save();
            Logger.Info("See you later");
        }
        #endregion

        #region Idle

        /// <summary>
        /// Updates number of active idle processes
        /// </summary>
        private void CheckIdleStatus()
        {
            ActiveProcessCount = AllBadges.Concat(Games).Count(b => b.CardIdleActive);
        }

        private void StopIdle()
        {
            Idler.Stop();
            foreach (var badge in AllBadges)
            {
                if (badge.CardIdleActive)
                    badge.CardIdleProcess.Stop();
            }
        }

        public ICommand ForceSyncCmd { get; private set; }

        private void ForceSync(object o)
        {
            _updater.Sync(true);
        }

        public ICommand StartBadgeIdleCmd { get; private set; }

        private void StartBadgeIdle(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return;
            badge.CardIdleProcess.Start();
        }

        private bool CanStartBadgeIdle(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return false;
            return IsSteamRunning && badge.CardIdleProcess.IsRunning == false;
        }


        public ICommand StopBadgeIdleCmd { get; private set; }

        private void StopBadgeIdle(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return;
            badge.CardIdleProcess.Stop();
        }

        private bool CanStopBadgeIdle(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return false;
            return badge.CardIdleProcess.IsRunning;
        }


        public ICommand IdleCmd { get; private set; }

        public static readonly string StartParam = "1";
        public static readonly string StopParam = "0";

        private bool CanIdle(object parameter)
        {
            var p = (string)parameter;
            if (p == StartParam)
                return IsSteamRunning && !_idler.IsActive && IdleQueueBadges.Count > 0;
            if (p == StopParam)
                return _idler.IsActive;
            return false;
        }

        private void Idle(object parameter)
        {
            var p = (string)parameter;
            if (p == StartParam)
                _idler.Start();
            else if (p == StopParam)
                _idler.Stop();
        }
        #endregion

        #region Queue

        /// <summary>
        /// Saves queued games when Queue changes
        /// </summary>
        private void IdleQueueItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Storage.IdleQueue.Clear();
            foreach (var b in IdleQueueBadges)
                Storage.IdleQueue.Add(b.AppId);
            Storage.Save();
        }


        public ICommand EnqueueAllCmd { get; private set; }

        /// <summary>
        /// Adds selected badges to idle queue
        /// </summary>
        /// <param name="parameter"></param>
        private void EnqueueAll(object parameter)
        {
            var order = (string)parameter;
            // depending on parameter, insert badge on top or append
            int idx = order == "0" ? 0 : IdleQueueBadges.Count;
            foreach (BadgeModel badge in Badges)
            {
                if (IdleQueueBadges.Contains(badge) == false)
                {
                    IdleQueueBadges.Insert(idx, badge);
                    badge.IsInQueue = true;
                    idx++;
                }
            }
        }


        public ICommand DequeueAllCmd { get; private set; }

        /// <summary>
        /// Removes all selected badges from idle queue
        /// </summary>
        private void DequeueAll()
        {
            foreach (BadgeModel badge in Badges.OfType<BadgeModel>().Where(b => b.IsInQueue))
            {
                IdleQueueBadges.Remove(badge);
                badge.IsInQueue = false;
            }
        }


        public ICommand EnqueueBadgeHighCmd { get; private set; }

        private void EnqueueBadgeHigh(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return;
            IdleQueueBadges.Insert(0, badge);
            badge.IsInQueue = true;
        }


        public ICommand EnqueueBadgeLowCmd { get; private set; }

        private void EnqueueBadgeLow(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return;
            IdleQueueBadges.Add(badge);
            badge.IsInQueue = true;
        }

        private bool CanEnqueueBadge(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return false;
            return IdleQueueBadges.Contains(badge) == false;
        }


        public ICommand DequeueBadgeCmd { get; private set; }

        private void DequeueBadge(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return;
            IdleQueueBadges.Remove(badge);
            badge.IsInQueue = false;
        }


        public ICommand SetHigherPriorityCmd { get; private set; }

        private void SetHigherPriority(object o)
        {
            SetPriority(o, -1);
        }

        private bool CanSetHigherPriority(object o)
        {
            return CanSetPriority(o, -1);
        }


        public ICommand SetLowerPriorityCmd { get; private set; }

        private void SetLowerPriority(object o)
        {
            SetPriority(o, 1);
        }

        private bool CanSetLowerPriority(object o)
        {
            return CanSetPriority(o, 1);
        }

        private void SetPriority(object o, int priority)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return;

            int idx = IdleQueueBadges.IndexOf(badge);
            IdleQueueBadges.RemoveAt(idx);
            IdleQueueBadges.Insert(idx + priority, badge);
        }

        private bool CanSetPriority(object o, int priority)
        {
            var list = IdleQueueBadges;
            if (list.Count < 2)
                return false;

            var badge = o as BadgeModel;
            if (badge == null)
                return false;

            if (priority < 0)
                return list[0] != badge;

            if (priority > 0)
                return list[list.Count - 1] != badge;

            return false;
        }
        #endregion

        #region Time Idle

        public ICommand AddGameCmd { get; private set; }

        private void AddGame()
        {
            var w = new GameSelectionWindow();
            w.DataContext = this;
            var res = w.ShowDialog();
            if (res == true && SelectedGame != null)
            {
                if (Games.Any(g => g.AppId == SelectedGame.AppId))
                    return;
                Storage.Games.Add(SelectedGame.AppId);
                Storage.Save();
                SelectedGame.PropertyChanged += BadgeIdleStatusChanged;
                Games.Insert(Games.Count - 1, SelectedGame);
            }
            StorePageUrl = String.Empty;
        }


        public ICommand RemoveGameCmd { get; private set; }

        private void RemoveGame(object o)
        {
            var game = o as BadgeModel;
            if (game == null)
                return;
            Storage.Games.Remove(game.AppId);
            Storage.Save();
            game.CardIdleProcess.Stop();
            game.PropertyChanged -= BadgeIdleStatusChanged;
            Games.Remove(game);
        }


        public string StorePageUrl
        {
            get { return _storePageUrl; }
            set
            {
                _storePageUrl = value;

                int id;
                if (int.TryParse(_storePageUrl, out id) == false)
                {
                    var match = Regex.Match(_storePageUrl, @"store\.steampowered\.com/app/(\d+)");
                    if (match.Success)
                    {
                        var stringId = match.Groups[1].Value;
                        id = int.Parse(stringId);
                    }
                }

                if (id > 0)
                    SelectGame(id);
                else
                    SelectedGame = null;
            }
        }

        private async void SelectGame(int id)
        {
            SelectedGame = await new SteamParser().GetGameInfo(id);
        }

        public BadgeModel SelectedGame
        {
            get { return _selectedGame; }
            private set
            {
                _selectedGame = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Blacklist
        public ICommand BlacklistBadgeCmd { get; private set; }

        private void BlacklistBadge(object parameter)
        {
            var badge = parameter as BadgeModel;
            if (badge == null)
                return;

            badge.IsBlacklisted = !badge.IsBlacklisted;
            if (badge.IsBlacklisted)
                Storage.Blacklist.Add(badge.AppId);
            else
                Storage.Blacklist.Remove(badge.AppId);
            Storage.Save();
        }
        #endregion

        #region Settings

        private void SaveConfiguration(object sender, PropertyChangedEventArgs e)
        {
            bool save = false;

            var account = sender as AccountModel;

            if (account != null)
            {
                if (e.PropertyName == "UserName")
                {
                    Storage.SteamUserName = account.UserName ?? string.Empty;
                    save = true;
                }
                else if (e.PropertyName == "Level")
                {
                    Storage.SteamLevel = account.Level;
                    save = true;
                }
                else if (e.PropertyName == "BackgroundUrl")
                {
                    Storage.SteamBackgroundUrl = account._backgroundUrl ?? string.Empty;
                    save = true;
                }

                else if (e.PropertyName == "AvatarUrl")
                {
                    Storage.SteamAvatarUrl = account.AvatarUrl ?? string.Empty;
                    save = true;
                }
                else if (e.PropertyName == "CustomBackgroundUrl")
                {
                    Storage.CustomBackgroundUrl = account.CustomBackgroundUrl ?? string.Empty;
                    save = true;
                }
                else if (e.PropertyName == "Filter")
                {
                    Storage.BadgeFilter = account.BadgePropertiesFilters.Serialize();
                    save = true;
                }
                else if (e.PropertyName == "ShowcaseFilter")
                {
                    Storage.ShowcaseFilter = account.ShowcasePropertiesFilters.Serialize();
                    save = true;
                }
                else if (e.PropertyName == "AllowShowcaseSync")
                {
                    Storage.AllowShowcaseSync = account.AllowShowcaseSync;
                    save = true;
                }
                else if (e.PropertyName == "ShowInTaskbar")
                {
                    Storage.ShowInTaskbar = account.ShowInTaskbar;
                    save = true;
                }
                else if (e.PropertyName == "ShowBackground")
                {
                    Storage.ShowBackground = account.ShowBackground;
                    save = true;
                }
                else if (e.PropertyName == "FavoriteBadge")
                {
                    if (account.FavoriteBadge != null)
                    {
                        Storage.SteamBadgeUrl = account.FavoriteBadge.PictureUrl;
                        Storage.SteamBadgeTitle = account.FavoriteBadge.Name;
                    }
                    else
                    {
                        Storage.SteamBadgeUrl =
                            Storage.SteamBadgeTitle = null;
                    }
                    save = true;
                }
            }
            else
            {
                var idler = (IdleManager)sender;
                if (e.PropertyName == "Mode")
                {
                    Storage.IdleMode = (int)idler.Mode;
                    save = true;
                }
                else if (e.PropertyName == "MaxIdleInstanceCount")
                {
                    Storage.MaxIdleProcessCount = idler.MaxIdleInstanceCount;
                    save = true;
                }
                else if (e.PropertyName == "PeriodicSwitchRepeatCount")
                {
                    Storage.PeriodicSwitchRepeatCount = idler.PeriodicSwitchRepeatCount;
                    save = true;
                }
                else if (e.PropertyName == "TrialPeriod")
                {
                    Storage.TrialPeriod = idler.TrialPeriod;
                    save = true;
                }
                else if (e.PropertyName == "SwitchMinutes")
                {
                    Storage.SwitchMinutes = idler.SwitchMinutes;
                    save = true;
                }
                else if (e.PropertyName == "SwitchSeconds")
                {
                    Storage.SwitchSeconds = idler.SwitchSeconds;
                    save = true;
                }
            }

            if (save)
                Storage.Save();
        }

        #endregion

        #region Badges filters
        /// <summary>
        /// Keyword for game title quick search
        /// </summary>
        public string GameTitle
        {
            get { return _gameTitle; }
            set
            {
                value = value.Trim();
                if (_gameTitle == value)
                    return;
                _gameTitle = value;
                SetFilter();
                OnPropertyChanged();
            }
        }

        public FilterStatesCollection BadgePropertiesFilters { get; set; }

        /// <summary>
        /// Applies selected filter to collection of badges
        /// </summary>
        private void SetFilter()
        {
            var titleSearch = TitleSearchFunc(GameTitle);
            var propertySearch = PropertySearchFunc(BadgePropertiesFilters);

            if (titleSearch == null || propertySearch == null)
                _badges.Filter = titleSearch ?? propertySearch;
            else
                _badges.Filter = o => titleSearch(o) && propertySearch(o);

            OnPropertyChanged("Filter");
            UpdateTotalValues();
        }

        private Predicate<object> TitleSearchFunc(string title)
        {
            if (String.IsNullOrWhiteSpace(title))
                return null;

            // https://stackoverflow.com/questions/444798/case-insensitive-containsstring
            // https://stackoverflow.com/a/15464440/1506454
            return o => CultureInfo.InvariantCulture.CompareInfo.IndexOf(((BadgeIdentity)o).Title, title, CompareOptions.IgnoreCase) >= 0;
        }

        private Predicate<object> PropertySearchFunc(FilterStatesCollection filters)
        {
            Predicate<object> predicate = null;

            var activeFilters = filters.Where(x => x.Value != FilterState.Any).ToList();

            foreach (var f in activeFilters)
            {
                FilterState fs = f.Value;
                Enum key = (Enum)f.Key;

                bool yes = fs == FilterState.Yes;
                Predicate<object> p = o => ((BadgeIdentity)o).CheckProperty(key) == yes;

                if (predicate == null)
                    predicate = p;
                else
                {
                    Predicate<object> z = predicate;
                    predicate = o => z(o) && p(o);
                }
            }

            return predicate;
        }

        /// <summary>
        /// Updates number of badges and remaining cards
        /// </summary>
        public void UpdateTotalValues()
        {
            TotalGames = Badges.Cast<BadgeModel>().Count();
            TotalCards = Badges.Cast<BadgeModel>().Sum(b => b.RemainingCard);
        }

        public void UpdateTrialStatus()
        {
            foreach (var badge in AllBadges)
            {
                badge.HasTrial = _idler.IsTrial(badge);
            }
        }

        private void TrialPeriodChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TrialPeriod")
                UpdateTrialStatus();
        }

        #endregion

        #region Showcases

        public FileStorage ShowcaseStorage
        {
            get { return _showcaseManager.Storage; }
            set { _showcaseManager.Storage = value; }
        }

        public ObservableCollection<BadgeShowcase> AllShowcases { get; private set; }

        public bool AllowShowcaseSync
        {
            get { return _allowShowcaseSync; }
            set
            {
                _allowShowcaseSync = value;
                OnPropertyChanged();

                if (_allowShowcaseSync)
                    SyncShowcases();
            }
        }

        private bool _showcaseSyncActive;

        private async void SyncShowcases()
        {
            if (_showcaseSyncActive || _updater.CompleBadgeList == null)
                return;

            if (false == AllowShowcaseSync)
                return;

            _showcaseSyncActive = true;

            try
            {
                await _showcaseManager.LoadShowcases(_updater.CompleBadgeList, AllShowcases);

                // restore bookmarks
                var bookmarks = Storage.ShowcaseBookmarks.Cast<string>()
                    .Join(AllShowcases, s => s, b => b.AppId, (s, badge) => badge)
                    .ToList();
                foreach (var badge in bookmarks)
                    badge.IsBookmarked = true;
            }
            finally
            {
                _showcaseSyncActive = false;
            }
        }

        public FilterStatesCollection ShowcasePropertiesFilters { get; set; }

        public string ShowcaseTitle
        {
            get { return _showcaseTitle; }
            set
            {
                value = value.Trim();
                if (_showcaseTitle == value)
                    return;
                _showcaseTitle = value;
                OnPropertyChanged();
                SetShowcaseFilter();
            }
        }

        private void SetShowcaseFilter()
        {
            var titleSearch = TitleSearchFunc(ShowcaseTitle);
            var propertySearch = PropertySearchFunc(ShowcasePropertiesFilters);

            if (titleSearch == null || propertySearch == null)
                _showcases.Filter = titleSearch ?? propertySearch;
            else
                _showcases.Filter = o => titleSearch(o) && propertySearch(o);

            OnPropertyChanged("ShowcaseFilter");
        }

        public ICommand BookmarkShowcaseCmd { get; private set; }

        private void BookmarkShowcase(object arg)
        {
            var showcase = arg as BadgeShowcase;
            if (showcase == null)
                return;
            showcase.IsBookmarked = !showcase.IsBookmarked;

            if (showcase.IsBookmarked)
                Storage.ShowcaseBookmarks.Add(showcase.AppId);
            else
                Storage.ShowcaseBookmarks.Remove(showcase.AppId);
            Storage.Save();
        }

        #endregion

        public void Dispose()
        {
            StopIdle();
        }
    }
}