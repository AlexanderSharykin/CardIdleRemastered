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
using CardIdleRemastered.Properties;
using CardIdleRemastered.ViewModels;

namespace CardIdleRemastered
{
    public class AccountModel:ObservableModel, IDisposable
    {
        #region Fields
        private readonly AccountUpdater _updater;
        private readonly IdleManager _idler;

        private DispatcherTimer _tmSteamStatus;
        private string _syncTime;

        private string _userName ;
        private string _level;
        private string _avatarUrl;
        private string _backgroundUrl;
        private string _customBackgroundUrl;

        private bool _isAuthorized;
        private int _activeProcessCount;
        private int _totalCards;
        private int _totalGames;        
        
        private string _storePageUrl;        
        private BadgeModel _selectedGame;

        private string _gameTitle;
        private readonly ICollectionView _badges;
        private IList<SelectionItemVm<FilterState>> _badgePropertiesFilters;
        
        private ReleaseInfo _newestRelease;
        private bool _canUpdateApp;

        #endregion

        public AccountModel()
        {
            _updater = new AccountUpdater(this);
            _idler = new IdleManager(this);
            
            AllBadges = new ObservableCollection<BadgeModel>();
            IdleQueueBadges = new ObservableCollection<BadgeModel>();            
            Games = new ObservableCollection<BadgeModel> {new BadgeModel("-1", "new", "0", "0")};

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

            SettingsCmd = new BaseCommand(o=>SettingsDialog());
            #endregion
            
            _badges = CollectionViewSource.GetDefaultView(AllBadges);
            var quick = (ICollectionViewLiveShaping)_badges;
            quick.LiveFilteringProperties.Add("IsBlacklisted");
            quick.LiveFilteringProperties.Add("HasTrial");
            quick.LiveFilteringProperties.Add("CardIdleActive");
            quick.LiveFilteringProperties.Add("IsInQueue");
            quick.IsLiveFiltering = true;


            _badgePropertiesFilters = Enum.GetValues(typeof (BadgeProperty))
                .OfType<BadgeProperty>()
                .Select(p => new SelectionItemVm<FilterState>
                             {
                                 Key = p,
                                 Value = FilterState.Any
                             })
                .ToList();

            foreach (var item in _badgePropertiesFilters)
            {
                item.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName != "Value")
                        return;

                    SetFilter();
                };
            }
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
            NewestRelease = await new SteamParser().GetLatestCardIdlerRelease();

            // compare current version number with latest release version
            var a = Assembly.GetExecutingAssembly().GetName().Version;
            var num = new int[] { a.Major, a.Minor, a.Build, a.Revision};
            int delta = Enumerable.Range(0, num.Length)
                .Select(i => num[i] - NewestRelease.Version[i])
                .SkipWhile(d => d == 0)
                .FirstOrDefault();
            CanUpdateApp = delta < 0;
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
                CustomBackgroundUrl = Storage.CustomBackgroundUrl;
                BackgroundUrl = Storage.SteamBackgroundUrl;
                
                string filtersList = Storage.BadgeFilter;
                if (false == String.IsNullOrWhiteSpace(filtersList))
                {
                    // parsing filter list
                    string[] parts = filtersList.Split(';');
                    foreach (string property in parts)
                    {
                        string[] keyValue = property.Split(':');
                        if (keyValue.Length < 2)
                            continue;
                        FilterState value = (FilterState)int.Parse(keyValue[1]);

                        if (value != FilterState.Any)
                        {
                            BadgeProperty key = (BadgeProperty)Enum.Parse(typeof(BadgeProperty), keyValue[0]);
                            var filter = BadgePropertiesFilters.First(f => key.Equals(f.Key));
                            filter.Value = value;
                        }
                    }
                }

                Idler.Mode = (IdleMode)Storage.IdleMode;                

                if (Storage.MaxIdleProcessCount > 0)
                    Idler.MaxIdleInstanceCount = Storage.MaxIdleProcessCount;
                Idler.SwitchMinutes = Storage.SwitchMinutes;
                if (Storage.SwitchSeconds > 0)
                    Idler.SwitchSeconds = Storage.SwitchSeconds;
                
                IgnoreClient = Storage.IgnoreClient;
            }

            PropertyChanged += SaveConfiguration;
            Idler.PropertyChanged += SaveConfiguration;

            IdleQueueBadges.CollectionChanged += IdleQueueItemsChanged;

            IsAuthorized = await CheckAuth();

            if (IsAuthorized)            
                await InitProfile();            

            // reload games list
            var games = Storage.Games.Cast<string>().ToList();
            int idx = 0;
            foreach (var id in games)
            {
                var game = await new SteamParser().GetGameInfo(id);
                Games.Insert(idx, game);
                idx++;
            }            
        }

        private async Task<bool> CheckAuth()
        {
            bool registered = !String.IsNullOrWhiteSpace(Storage.SessionId) &&
                              !String.IsNullOrWhiteSpace(Storage.SteamLogin) &&
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
            Storage.SteamLogin = string.Empty;            
            Storage.SteamParental = string.Empty;
            UserName = 
            ProfileUrl =
            Level = 
            AvatarUrl = 
            BackgroundUrl =
            CustomBackgroundUrl = null;
            Storage.IdleMode = 0;
            Storage.BadgeFilter = string.Empty;
            
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
        public ICommand SettingsCmd { get; private set; } 

        private void SettingsDialog()
        {
            var vis = new AppVisualSettings();
            vis.GetBrushes();

            vis.BackgroundUrl = CustomBackgroundUrl;
            if (String.IsNullOrEmpty(vis.BackgroundUrl))
                vis.BackgroundUrl = BackgroundUrl;

            vis.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "BackgroundUrl")
                {
                    CustomBackgroundUrl = vis.BackgroundUrl;
                }
            };

            var sw = new SettingsWindow();
            sw.DataContext = vis;
            sw.ShowDialog();
        }

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
                    Storage.SteamBackgroundUrl = account.BackgroundUrl ?? string.Empty;
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
                    Storage.BadgeFilter = String.Join(";", account.BadgePropertiesFilters.Select(f => String.Format("{0}:{1}", f.Key, (int) f.Value)));
                    save = true;
                }
            }

            else if (e.PropertyName == "Mode")
            {
                Storage.IdleMode = (int) (sender as IdleManager).Mode;
                save = true;
            }

            else if (e.PropertyName == "MaxIdleInstanceCount")
            {
                Storage.MaxIdleProcessCount = (sender as IdleManager).MaxIdleInstanceCount;
                save = true;
            }
            else if (e.PropertyName == "SwitchMinutes")
            {
                Storage.SwitchMinutes = (sender as IdleManager).SwitchMinutes;
                save = true;
            }
            else if (e.PropertyName == "SwitchSeconds")
            {
                Storage.SwitchSeconds = (sender as IdleManager).SwitchSeconds;
                save = true;
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

        public IEnumerable<string> BadgeProperties
        {
            get
            {
                return Enum.GetValues(typeof(BadgeProperty))
                    .Cast<BadgeProperty>()
                    .Select(e => EnumLocalizationConverter.GetLocalValue(e));
            }
        }

        public IEnumerable<string> FilterStates
        {
            get
            {
                return Enum.GetValues(typeof(FilterState))
                    .Cast<FilterState>()
                    .Select(e => EnumLocalizationConverter.GetLocalValue(e));
            }
        }

        public IList<SelectionItemVm<FilterState>> BadgePropertiesFilters
        {
            get { return _badgePropertiesFilters; }            
        }

        /// <summary>
        /// Applies selected filter to collection of badges
        /// </summary>
        private void SetFilter()
        {
            var titleSearch = TitleSearchFunc();
            var propertySearch = PropertySearchFunc();

            if (titleSearch == null || propertySearch == null)
                _badges.Filter = titleSearch ?? propertySearch;
            else
                _badges.Filter = o => titleSearch(o) && propertySearch(o);
            
            OnPropertyChanged("Filter");
            UpdateTotalValues();
        }

        private Predicate<object> TitleSearchFunc()
        {
            if (String.IsNullOrWhiteSpace(GameTitle))
                return null;

            // https://stackoverflow.com/questions/444798/case-insensitive-containsstring
            // https://stackoverflow.com/a/15464440/1506454
            return o => CultureInfo.InvariantCulture.CompareInfo.IndexOf(((BadgeModel)o).Title, GameTitle, CompareOptions.IgnoreCase) >= 0;            
        }

        private Predicate<object> PropertySearchFunc()
        {
            Predicate<object> predicate = null;

            var filtered = BadgePropertiesFilters.Where(x => x.Value != FilterState.Any).ToList();

            foreach (var f in filtered)
            {
                FilterState fs = f.Value;
                BadgeProperty key = (BadgeProperty) f.Key;

                bool yes = fs == FilterState.Yes;
                Predicate<object> p = o => ((BadgeModel) o).CheckProperty(key) == yes;

                if (predicate == null)
                    predicate = p;
                else
                {
                    Predicate<object> z = predicate;
                    predicate = (object o) => z(o) && p(o);
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

        #endregion

        public void Dispose()
        {
            StopIdle();
        }
    }
}