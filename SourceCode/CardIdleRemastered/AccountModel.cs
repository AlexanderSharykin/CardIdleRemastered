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
using Steamworks;

namespace CardIdleRemastered
{
    public class AccountModel:ObservableModel
    {        
        private readonly AccountUpdater _updater;
        private readonly IdleManager _idler;
        private string _userName;
        private string _level = "0";
        private BitmapImage _avatar;

        private readonly ICollectionView _badges;
        private BadgeModelFilter _filter;               

        private DispatcherTimer _tmSteamStatus;
        private ICommand _logoutCmd;

        private int _totalCards;
        private int _totalGames;
        private BitmapImage _background;
        private string _syncTime;
        private string _gameTitle;
        private BitmapImage _customBackground;
        private string _avatarUrl;
        private string _backgroundUrl;
        private string _customBackgroundUrl;
        private string _storePageUrl;
        private BadgeModel _selectedGame;

        public AccountModel()
        {
            _updater = new AccountUpdater(this);
            _idler = new IdleManager(this);

            AllBadges = new ObservableCollection<BadgeModel>();

            IdleQueueBadges = new ObservableCollection<BadgeModel>();
            _filter = BadgeModelFilter.All;

            Games = new ObservableCollection<BadgeModel> {new BadgeModel("-1", "new", "0", "0")};

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
            
            _badges = CollectionViewSource.GetDefaultView(AllBadges);
            var quick = (ICollectionViewLiveShaping)_badges;
            quick.LiveFilteringProperties.Add("IsBlacklisted");
            quick.LiveFilteringProperties.Add("HasTrial");
            quick.LiveFilteringProperties.Add("CardIdleActive");
            quick.LiveFilteringProperties.Add("IsInQueue");
            quick.IsLiveFiltering = true;            
        }

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

        public BitmapImage Avatar
        {
            get { return _avatar; }
            set
            {
                _avatar = value;
                OnPropertyChanged();
            }
        }

        public string AvatarUrl
        {
            get { return _avatarUrl; }
            set
            {
                _avatarUrl = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage Background
        {
            get
            {
                if (_customBackground != null)
                    return _customBackground;
                return _background;
            }
            set
            {
                _background = value;                
                OnPropertyChanged();
            }
        }

        public string BackgroundUrl
        {
            get { return _backgroundUrl; }
            set
            {
                _backgroundUrl = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage CustomBackground
        {
            get { return _customBackground; }
            set
            {
                _customBackground = value;
                OnPropertyChanged("Background");
            }
        }

        public string CustomBackgroundUrl
        {
            get { return _customBackgroundUrl; }
            set
            {
                _customBackgroundUrl = value;
                OnPropertyChanged();
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

        public bool IsCardIdleActive
        {
            get
            {
                return AllBadges.Any(b => b.CardIdleActive) || Games.Any(b => b.CardIdleActive);
            }
        }

        public void CheckActivityStatus()
        {
            OnPropertyChanged("IsCardIdleActive");
        }

        public bool IsSteamRunning
        {
            get
            {
                return SteamAPI.IsSteamRunning() || IgnoreClient;
            }
        }

        public bool IgnoreClient { get; set; }

        public IdleManager Idler
        {
            get { return _idler; }
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

        public ICommand LogoutCmd
        {
            get
            {
                if (_logoutCmd == null)
                    _logoutCmd = new BaseCommand(_=>Logout());
                return _logoutCmd;
            }            
        }

        /// <summary>
        /// Log out from current account
        /// </summary>
        private void Logout()
        {
            StopCardIdle();
            StopTimers();

            App.CardIdle.Logout();
        }


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
                Settings.Default.blacklist.Add(badge.AppId.ToString());
            else
                Settings.Default.blacklist.Remove(badge.AppId.ToString());
            Settings.Default.Save();
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
        }

        public ICommand RemoveGameCmd { get; private set; }

        private void RemoveGame(object o)
        {
            var game = o as BadgeModel;
            if (game == null)
                return;
            Settings.Default.Games.Remove(game.AppId);
            Settings.Default.Save();
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
            SelectedGame = await new SteamParser().GetBadge(id);
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
        }
    }
}
