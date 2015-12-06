using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CardIdleRemastered.Commands;

namespace CardIdleRemastered
{
    public class BadgeModel:ObservableModel
    {
        public AccountModel _account;
        public BadgeModel(AccountModel acc, string id, string title, string remaining, string hours)
            : this(acc)
        {
            StringId = id;
            Title = title;
            UpdateStats(remaining, hours);
        }

        public BadgeModel(AccountModel account)
        {
            if (account == null)
                throw new ArgumentNullException("account");
            _account = account;
        }

        public AccountModel Account { get { return _account; } }

        public int AppId { get; set; }

        public string ImageUrl
        {
            get { return "http://cdn.akamai.steamstatic.com/steam/apps/" + AppId + "/header_292x136.jpg"; }
        }

        public BitmapImage AppImage { get; set; }

        public string Title { get; set; }

        public string StringId
        {
            get { return AppId.ToString(); }
            set { AppId = string.IsNullOrWhiteSpace(value) ? 0 : int.Parse(value); }
        }

        public int RemainingCard
        {
            get { return _remainingCard; }
            set
            {
                _remainingCard = value;
                OnPropertyChanged();
                if (CardIdleActive && _remainingCard == 0)
                    CardIdleProcess.Stop();
            }
        }

        public double HoursPlayed
        {
            get { return _hoursPlayed; }
            set
            {
                _hoursPlayed = value;
                OnPropertyChanged();
                OnPropertyChanged("HasTrial");
            }
        }
        
        private int _remainingCard;
        private double _hoursPlayed;
        private ICommand _startIdle;
        private ICommand _stopIdle;
        private ICommand _blacklistCmd;
        private ICommand _visitStorePage;
        private ICommand _enqueueCmd;
        private ICommand _dequeueCmd;
        private ICommand _setPriorityCmd;
        private bool _isBlacklisted;
        private IdleProcess _idleProcess;
        private bool _isInQueue;

        public IdleProcess CardIdleProcess
        {
            get
            {
                if (_idleProcess == null)
                {
                    _idleProcess = new IdleProcess(this);
                    _idleProcess.IdleStopped += (sender, args) =>
                    {
                        if (IdleStopped != null)
                            IdleStopped(this, EventArgs.Empty);
                        OnPropertyChanged("CardIdleActive");
                    };
                }
                return _idleProcess;
            }            
        }

        public event EventHandler IdleStopped;

        public bool HasTrial
        {
            get { return !CardIdleActive && HoursPlayed < 2; }
        }

        public bool CardIdleActive
        {
            get { return CardIdleProcess.IsRunning; }
        }

        public bool IsBlacklisted
        {
            get { return _isBlacklisted; }
            set
            {
                _isBlacklisted = value;
                OnPropertyChanged();
            }
        }

        public bool IsInQueue
        {
            get { return _isInQueue; }
            set
            {
                _isInQueue = value; 
                OnPropertyChanged();
            }
        }

        public ICommand StartIdleCmd
        {
            get
            {
                if (_startIdle == null)
                    _startIdle = new IdleStartCommand(this);
                return _startIdle;
            }            
        }

        public ICommand StopIdleCmd
        {
            get
            {
                if (_stopIdle == null)
                    _stopIdle = new IdleStopCommand(this);
                return _stopIdle;
            }
        }

        public ICommand BlacklistCmd
        {
            get
            {
                if (_blacklistCmd == null)
                    _blacklistCmd = new BlackListCommand(this);
                return _blacklistCmd;
            }
        }

        public ICommand VisitStorePageCmd
        {
            get
            {
                if (_visitStorePage == null)
                    _visitStorePage = new NavigationCommand { DefaultUri = "http://store.steampowered.com/app/" + AppId };
                return _visitStorePage;
            }
        }

        public ICommand EnqueueCmd
        {
            get
            {
                if (_enqueueCmd == null)
                    _enqueueCmd = new EnqueueBadgeCommand(this);
                return _enqueueCmd;
            }
        }

        public ICommand DequeueCmd
        {
            get
            {
                if (_dequeueCmd == null)
                    _dequeueCmd = new DequeueBadgeCommand(this);
                return _dequeueCmd;
            }
        }

        public ICommand SetPriorityCmd
        {
            get
            {
                if (_setPriorityCmd == null)
                    _setPriorityCmd = new SetPriorityCommand(this);
                return _setPriorityCmd;
            }
        }

        public bool CanCardDrops()
        {
            return RemainingCard > 0;
        }

        public void UpdateStats(string remaining, string hours)
        {
            RemainingCard = string.IsNullOrWhiteSpace(remaining) ? 0 : int.Parse(remaining);
            HoursPlayed = string.IsNullOrWhiteSpace(hours) ? 0 : double.Parse(hours, new NumberFormatInfo());
        }

        public override bool Equals(object obj)
        {
            var badge = obj as BadgeModel;
            return badge != null && Equals(AppId, badge.AppId);
        }

        public override int GetHashCode()
        {
            return AppId.GetHashCode();
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Title) ? StringId : Title;
        }
    }
}
