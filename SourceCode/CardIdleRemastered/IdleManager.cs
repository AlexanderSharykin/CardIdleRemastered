using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CardIdleRemastered
{
    public class IdleManager: ObservableModel
    {
        public static readonly byte DefaultIdleInstanceCount = 16;
        private bool _isActive;
        private IdleMode _mode;
        private AccountModel _account;
        private byte _maxIdleInstanceCount;
        private Random _rand = new Random();

        public IdleManager(AccountModel acc)
        {
            _account = acc;
            _maxIdleInstanceCount = DefaultIdleInstanceCount;
        }

        public bool IsActive
        {
            get { return _isActive; }
            private set
            {
                _isActive = value; 
                OnPropertyChanged();
            }
        }

        public IdleMode Mode
        {
            get { return _mode; }
            set
            {
                _mode = value; 
                OnPropertyChanged();
            }
        }

        public IEnumerable<string> IdleModes
        {
            get
            {
                return Enum.GetValues(typeof(IdleMode))
                    .Cast<IdleMode>()
                    .Select(e => EnumLocalizationConverter.GetLocalValue(e));
            }
        }

        public byte MaxIdleInstanceCount
        {
            get { return _maxIdleInstanceCount; }
            set
            {
                _maxIdleInstanceCount = value;
                OnPropertyChanged();
            }
        }

        
        private List<BadgeWrapper> _badgeBuffer;        
        private IdleMode _currentMode;
        private DispatcherTimer _tmCounter;

        public async void Start()
        {
            if (IsActive)
                return;

            IsActive = true;

            _currentMode = Mode;
            _badgeBuffer = new List<BadgeWrapper>(Math.Max((int)MaxIdleInstanceCount, 1));
            _tmCounter = new DispatcherTimer();
            _tmCounter.Tick += UpdateGameTimeCounter;
            _tmCounter.Interval = new TimeSpan(0, 1, 0);

            await Proceed();
            _tmCounter.Start();
        }

        private async Task Proceed()
        {
            var trial = _account.IdleQueueBadges.Where(b => b.HoursPlayed < 2).ToArray();

            if (_badgeBuffer.Count == 0)
            {
                if (_currentMode == IdleMode.OneByOne)
                {
                    var next = _account.IdleQueueBadges.FirstOrDefault();
                    if (next != null)
                        await AddGame(next);
                }
                else
                {
                    var next = _account.IdleQueueBadges.FirstOrDefault(b => b.HoursPlayed >= 2.0 && b.RemainingCard > 0);
                    
                    if (_mode == IdleMode.TrialFirst && trial.Length > 0 || next == null)                    
                        await AddTrialGames(trial);

                    if (_badgeBuffer.Count == 0 && next != null)
                        await AddGame(next);
                }
            }
            else
            {
                await AddTrialGames(trial);
            }

            if (_badgeBuffer.Count == 0)
            {
                IsActive = false;
                _tmCounter.Stop();                
            }            
        }

        private async Task AddGame(BadgeModel next, bool trial = false)
        {
            if (_badgeBuffer.Any(w=>w.Badge == next) || _badgeBuffer.Count == _badgeBuffer.Capacity)
                return;
            _badgeBuffer.Add(new BadgeWrapper{Badge = next, IsTrial = trial, Hours = next.HoursPlayed});            
            next.IdleStopped += BadgeIdleProcessStopped;

            next.CardIdleProcess.Start();
            if (trial)
                next.PropertyChanged += BadgeHoursChanged;
            next.PropertyChanged += BadgeQueueStateChanged;

            // Make a short but random amount of time pass before starting next game
            var wait = _rand.Next(40, 80);
            wait = wait * 100;
            await Task.Delay(wait);
        }

        private async Task AddTrialGames(BadgeModel[] trial)
        {
            foreach (var badge in trial)
            {
                if (_badgeBuffer.Count == _badgeBuffer.Capacity)
                    break;
                await AddGame(badge, true);
            }
        }

        private void UpdateGameTimeCounter(object sender, EventArgs e)
        {
            // if Steam doesn't update time in game when idling multiple games,
            // Idle Manager will count minutes
            var trials = _badgeBuffer.Where(x => x.IsTrial).ToList();
            foreach (var item in trials)
            {
                item.Minutes++;                
                var hours = item.Hours + item.Minutes/60.0;
                if (hours >= 2.0)
                {
                    item.Badge.HoursPlayed = hours;
                    //item.Badge.CardIdleProcess.Stop();                    
                }
            }
        }

        private void BadgeHoursChanged(object sender, PropertyChangedEventArgs e)
        {
            var badge = (BadgeModel) sender;
            if (e.PropertyName == "HoursPlayed")
            {
                // Steam delay for trial games is 2 hours (refund condition)
                if (badge.HoursPlayed >= 2.0)
                    badge.CardIdleProcess.Stop();
            }
        }

        private void BadgeQueueStateChanged(object sender, PropertyChangedEventArgs e)
        {
            var badge = (BadgeModel)sender;
            if (e.PropertyName == "IsInQueue")
            {
                if (false == badge.IsInQueue)
                    badge.CardIdleProcess.Stop();
            }
        }

        private async void BadgeIdleProcessStopped(object sender, EventArgs eventArgs)
        {
            RemoveGame((BadgeModel)sender);
            await Proceed();
        }

        private void RemoveGame(BadgeModel badge)
        {
            _badgeBuffer.RemoveAll(b=>b.Badge == badge);
            badge.IdleStopped -= BadgeIdleProcessStopped;            
            badge.PropertyChanged -= BadgeHoursChanged;
            badge.PropertyChanged -= BadgeQueueStateChanged;               
        }

        public void Stop()
        {
            if (IsActive == false)
                return;
            for (int i = _badgeBuffer.Count-1; i >= 0; i--)
            {
                var badge = _badgeBuffer[i].Badge;
                RemoveGame(badge);
                badge.CardIdleProcess.Stop();
            }
            
            IsActive = false;
            _tmCounter.Stop();
        }

        private class BadgeWrapper
        {
            public BadgeModel Badge { get; set; }
            public double Hours { get; set; }
            public int Minutes { get; set; }
            public bool IsTrial { get; set; }
        }
    }
}
