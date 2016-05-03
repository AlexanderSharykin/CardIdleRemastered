using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CardIdleRemastered
{
    public class BadgeModel:ObservableModel
    {
        public BadgeModel(string id, string title, string card, string hours)
        {
            AppId = id;
            Title = title;
            UpdateStats(card, hours);
        }

        public BadgeModel()
        {
        }

        public string Title { get; set; }
        
        public string AppId { get; set; }

        public string StorePageUrl
        {
            get { return "http://store.steampowered.com/app/" + AppId; }
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

        public string ImageUrl
        {
            get { return "http://cdn.akamai.steamstatic.com/steam/apps/" + AppId + "/header_292x136.jpg"; }
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

        private IdleProcess _idleProcess;
        private bool _isBlacklisted;
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
            return string.IsNullOrWhiteSpace(Title) ? AppId : Title;
        }
    }
}
