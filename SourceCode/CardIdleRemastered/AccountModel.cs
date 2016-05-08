using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CardIdleRemastered.Commands;
using CardIdleRemastered.Properties;

namespace CardIdleRemastered
{
    public class AccountModel:ObservableModel
    {        
        private readonly AccountUpdater _updater;
        private readonly IdleManager _idler;
        private string _userName = "Card Idle";
        private string _level = "0";
        
        private readonly ICollectionView _badges;
        private BadgeModelFilter _filter;               

        private DispatcherTimer _tmSteamStatus;        

        private int _totalCards;
        private int _totalGames;        
        private string _syncTime;
        private string _gameTitle;
        private string _avatarUrl = "../Resources/Avatar.png";
        private string _backgroundUrl;
        private string _customBackgroundUrl;
        private string _storePageUrl;
        private BadgeModel _selectedGame;
        private bool _isAuthorized = false;
        private int _activeProcessCount;

        public AccountModel()
        {
            _updater = new AccountUpdater(this);
            _idler = new IdleManager(this);
            
            AllBadges = new ObservableCollection<BadgeModel>();

            IdleQueueBadges = new ObservableCollection<BadgeModel>();
            _filter = BadgeModelFilter.All;

            Games = new ObservableCollection<BadgeModel> {new BadgeModel("-1", "new", "0", "0")};

            LoginCmd = new BaseCommand(_ => Login());
            LogoutCmd = new BaseCommand(_ => Logout());

            StartBadgeIdleCmd = new BaseCommand(StartBadgeIdle, CanStartBadgeIdle);
            StopBadgeIdleCmd = new BaseCommand(StopBadgeIdle, CanStopBadgeIdle);
            BlacklistBadgeCmd = new BaseCommand(BlacklistBadge);

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

            SettingsCmd = new BaseCommand(o=>App.CardIdle.SettingsDialog());
            
            _badges = CollectionViewSource.GetDefaultView(AllBadges);
            var quick = (ICollectionViewLiveShaping)_badges;
            quick.LiveFilteringProperties.Add("IsBlacklisted");
            quick.LiveFilteringProperties.Add("HasTrial");
            quick.LiveFilteringProperties.Add("CardIdleActive");
            quick.LiveFilteringProperties.Add("IsInQueue");
            quick.IsLiveFiltering = true;            
        }

        #region Badges filters
        private bool TitleSearch(object o)
        {
            return string.IsNullOrWhiteSpace(GameTitle) || ((BadgeModel) o).Title.ToLower().Contains(GameTitle);
        }

        private bool IsTrialGame(object o)
        {
            return ((BadgeModel) o).HasTrial && TitleSearch(o);
        }

        private bool IsRunningGame(object o)
        {
            return ((BadgeModel)o).CardIdleActive && TitleSearch(o);
        }

        private bool NotEnqueuedGame(object o)
        {
            return ((BadgeModel)o).IsInQueue == false && TitleSearch(o);
        }

        private bool IsBlacklistedGame(object o)
        {
            return ((BadgeModel)o).IsBlacklisted && TitleSearch(o);
        }
        #endregion

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

        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        public string ProfileUrl { get; set; }

        public string AvatarUrl
        {
            get { return _avatarUrl; }
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
            get { return _level; }
            set
            {
                _level = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView Badges { get { return _badges; } }

        public ObservableCollection<BadgeModel> AllBadges { get; private set; }
        
        public ObservableCollection<BadgeModel> IdleQueueBadges { get; private set; }

        public ObservableCollection<BadgeModel> Games { get; private set; } 

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

        public void UpdateTotalValues()
        {
            TotalGames = Badges.Cast<BadgeModel>().Count();
            TotalCards = Badges.Cast<BadgeModel>().Sum(b => b.RemainingCard);
        }

        public string SyncTime
        {
            get { return _syncTime; }
            set
            {
                _syncTime = value; 
                OnPropertyChanged();
            }
        }

        public IEnumerable<string> BadgeFilters
        {
            get
            {
                return Enum.GetValues(typeof (BadgeModelFilter))
                    .Cast<BadgeModelFilter>()
                    .Select(e => EnumLocalizationConverter.GetLocalValue(e));
            }
        }

        public BadgeModelFilter Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                OnPropertyChanged();
                SetFilter();                
            }
        }

        private void SetFilter()
        {
            switch (_filter)
            {
                case BadgeModelFilter.All:
                    _badges.Filter = TitleSearch;
                    break;
                case BadgeModelFilter.HasTrial:
                    _badges.Filter = IsTrialGame;
                    break;
                case BadgeModelFilter.Running:
                    _badges.Filter = IsRunningGame;
                    break;
                case BadgeModelFilter.NotEnqueued:
                    _badges.Filter = NotEnqueuedGame;
                    break;
                case BadgeModelFilter.Blacklisted:
                    _badges.Filter = IsBlacklistedGame;
                    break;
            }            
            UpdateTotalValues();
        }

        public string GameTitle
        {
            get { return _gameTitle; }
            set
            {
                value = value.Trim().ToLower();
                if (_gameTitle == value)
                    return;
                _gameTitle = value;
                SetFilter();
                OnPropertyChanged();
            }
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

        public void CheckActivityStatus()
        {
            ActiveProcessCount = AllBadges.Concat(Games).Count(b => b.CardIdleActive);            
        }

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

        public bool IgnoreClient { get; set; }

        public IdleManager Idler
        {
            get { return _idler; }
        }

        public async void LoadAccount()
        {
            if (String.IsNullOrWhiteSpace(Settings.Default.myProfileURL) == false)
            {
                ProfileUrl = Settings.Default.myProfileURL;

                UserName = Settings.Default.SteamUserName;
                Level = Settings.Default.SteamLevel;

                AvatarUrl = Settings.Default.SteamAvatarUrl;
                CustomBackgroundUrl = Settings.Default.CustomBackgroundUrl;
                BackgroundUrl = Settings.Default.SteamBackgroundUrl;

                Filter = (BadgeModelFilter)Settings.Default.BadgeFilter;
                if (Filter == BadgeModelFilter.Running)
                    Filter = BadgeModelFilter.All;

                Idler.Mode = (IdleMode)Settings.Default.IdleMode;

                IgnoreClient = Settings.Default.ignoreclient;

                if (Settings.Default.MaxIdleProcessCount > 0)
                    Idler.MaxIdleInstanceCount = Settings.Default.MaxIdleProcessCount;
            }

            PropertyChanged += SaveConfiguration;
            Idler.PropertyChanged += SaveConfiguration;

            IdleQueueBadges.CollectionChanged += (sender, args) =>
            {
                Settings.Default.IdleQueue.Clear();
                foreach (var b in IdleQueueBadges)
                    Settings.Default.IdleQueue.Add(b.AppId);
                Settings.Default.Save();
            };

            await CheckAuth();

            var games = Settings.Default.Games.Cast<string>().ToList();
            int idx = 0;
            foreach (var id in games)
            {
                var game = await new SteamParser().GetGameInfo(id);
                Games.Insert(idx, game);
                idx++;
            }
        }

        private async Task CheckAuth()
        {
            bool registered = !String.IsNullOrWhiteSpace(Settings.Default.sessionid) &&
                              !String.IsNullOrWhiteSpace(Settings.Default.steamLogin);
            if (registered)
            {
                Logger.Info("Session test");
                registered = await new SteamParser().IsLogined(Settings.Default.myProfileURL);
            }

            if (registered)
            {
                Logger.Info("Welcome back");
                IsAuthorized = true;
                await InitProfile();
            }
            else
                Logger.Info("Login required");
        }

        private async Task InitProfile()
        {
            InitSteamTimer();
            StartTimers();

            // load badges and new profile settings
            await Sync();

            var queue = Settings.Default.IdleQueue.Cast<string>()
                .Join(AllBadges, s => s, b => b.AppId, (s, badge) => badge)
                .ToList();
            foreach (var badge in queue)
                EnqueueBadgeLowCmd.Execute(badge);

            var blacklist = Settings.Default.blacklist.Cast<string>()
                .Join(AllBadges, s => s, b => b.AppId, (s, badge) => badge)
                .ToList();
            foreach (var badge in blacklist)
                badge.IsBlacklisted = true;
        }

        private void SaveConfiguration(object sender, PropertyChangedEventArgs e)
        {
            bool save = false;

            if (e.PropertyName == "UserName")
            {
                Settings.Default.SteamUserName = (sender as AccountModel).UserName;
                save = true;
            }

            if (e.PropertyName == "Level")
            {
                Settings.Default.SteamLevel = (sender as AccountModel).Level;
                save = true;
            }

            if (e.PropertyName == "BackgroundUrl")
            {
                Settings.Default.SteamBackgroundUrl = (sender as AccountModel).BackgroundUrl;
                save = true;
            }

            if (e.PropertyName == "AvatarUrl")
            {
                Settings.Default.SteamAvatarUrl = (sender as AccountModel).AvatarUrl;
                save = true;
            }

            if (e.PropertyName == "CustomBackgroundUrl")
            {
                Settings.Default.CustomBackgroundUrl = (sender as AccountModel).CustomBackgroundUrl;
                save = true;
            }

            if (e.PropertyName == "Filter")
            {
                Settings.Default.BadgeFilter = (int)(sender as AccountModel).Filter;
                save = true;
            }

            if (e.PropertyName == "Mode")
            {
                Settings.Default.IdleMode = (int)(sender as IdleManager).Mode;
                save = true;
            }

            if (e.PropertyName == "MaxIdleInstanceCount")
            {
                Settings.Default.MaxIdleProcessCount = (sender as IdleManager).MaxIdleInstanceCount;
                save = true;
            }

            if (save)
                Settings.Default.Save();
        }

        public void InitSteamTimer()
        {
            _tmSteamStatus = new DispatcherTimer();
            _tmSteamStatus.Interval = new TimeSpan(0,0,5);
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

        public async Task Sync()
        {
            await _updater.Sync();
        }

        public ICommand LoginCmd { get; private set; }

        private void Login()
        {
            new BrowserWindow().ShowDialog();
            CheckAuth();
        }

        public ICommand LogoutCmd { get; private set; }

        /// <summary>
        /// Log out from current account
        /// </summary>
        private void Logout()
        {
            StopCardIdle();
            StopTimers();

            AllBadges.Clear();
            IdleQueueBadges.Clear();

            IsAuthorized = false;
            // todo clear properties

            return;
            // Clear the account settings
            Settings.Default.sessionid = string.Empty;
            Settings.Default.steamLogin = string.Empty;
            Settings.Default.myProfileURL = string.Empty;
            Settings.Default.steamparental = string.Empty;
            Settings.Default.SteamUserName = string.Empty;
            Settings.Default.SteamLevel = string.Empty;
            Settings.Default.SteamAvatarUrl = string.Empty;
            Settings.Default.SteamBackgroundUrl = string.Empty;
            Settings.Default.CustomBackgroundUrl = string.Empty;
            Settings.Default.AppBrushes.Clear();
            Settings.Default.IdleQueue.Clear();
            Settings.Default.IdleMode = 0;
            Settings.Default.BadgeFilter = 0;
            Settings.Default.Save();
            Logger.Info("See you later");


            App.CardIdle.ResetBrushes();
        }


        public ICommand SettingsCmd { get; private set; }        

        public ICommand StartBadgeIdleCmd { get; private set; }

        private void StartBadgeIdle(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return;
            badge.CardIdleProcess.Start();
            CheckActivityStatus();
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
            CheckActivityStatus();
        }

        private bool CanStopBadgeIdle(object o)
        {
            var badge = o as BadgeModel;
            if (badge == null)
                return false;
            return badge.CardIdleProcess.IsRunning;
        }

        public ICommand BlacklistBadgeCmd { get; private set; }

        private void BlacklistBadge(object parameter)
        {
            var badge = parameter as BadgeModel;
            if (badge == null)
                return;

            badge.IsBlacklisted = !badge.IsBlacklisted;
            if (badge.IsBlacklisted)
                Settings.Default.blacklist.Add(badge.AppId);
            else
                Settings.Default.blacklist.Remove(badge.AppId);
            Settings.Default.Save();
        }

        #region Queue
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

        public ICommand SetLowerPriorityCmd { get; private set; }

        private void SetHigherPriority(object o)
        {
            SetPriority(o, -1);
        }

        private bool CanSetHigherPriority(object o)
        {
            return CanSetPriority(o, -1);
        }

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
                Settings.Default.Games.Add(SelectedGame.AppId);
                Settings.Default.Save();
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
            Settings.Default.Games.Remove(game.AppId);
            Settings.Default.Save();
            game.CardIdleProcess.Stop();
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

        public void StartTimers()
        {
            _updater.Start();
        }

        public void StopTimers()
        {
            _updater.Stop();
        }

        public void StopCardIdle()
        {
            Idler.Stop();
            foreach (var badge in AllBadges)
            {
                if (badge.CardIdleActive)
                    badge.CardIdleProcess.Stop();
            }
            CheckActivityStatus();
        }
    }
}
