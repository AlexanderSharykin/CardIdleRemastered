using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CardIdleRemastered.Converters;

namespace CardIdleRemastered
{
    public class IdleManager : ObservableModel
    {
        public static readonly byte DefaultIdleInstanceCount = 16;
        public static readonly byte DefaultSwitchSeconds = 10;

        private AccountModel _account;

        private bool _isActive;
        private IdleMode _mode;
        private byte _periodicSwitchRepeatCount = 1;
        private byte _maxIdleInstanceCount;
        private double _trialPeriod;
        private byte _switchMinutes;
        private byte _switchSeconds;

        private Random _rand = new Random();

        private IdleMode _currentMode;
        private List<BadgeIdlingWrapper> _badgeBuffer;
        private DispatcherTimer _tmCounter;

        public IdleManager(AccountModel acc)
        {
            _account = acc;
            _maxIdleInstanceCount = DefaultIdleInstanceCount;
            _switchSeconds = DefaultSwitchSeconds;
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

        public byte PeriodicSwitchRepeatCount
        {
            get { return _periodicSwitchRepeatCount; }
            set
            {
                _periodicSwitchRepeatCount = value;
                OnPropertyChanged();
            }
        }

        public double TrialPeriod
        {
            get { return _trialPeriod; }
            set
            {
                _trialPeriod = Math.Round(value, 1);
                OnPropertyChanged();
            }
        }

        public byte SwitchMinutes
        {
            get { return _switchMinutes; }
            set
            {
                _switchMinutes = value;
                OnPropertyChanged();
            }
        }

        public byte SwitchSeconds
        {
            get { return _switchSeconds; }
            set
            {
                _switchSeconds = value;
                OnPropertyChanged();
            }
        }

        public bool IsTrial(BadgeModel badge)
        {
            return badge.HoursPlayed < TrialPeriod;
        }

        public async void Start()
        {
            if (IsActive)
                return;

            IsActive = true;

            _currentMode = Mode;
            _badgeBuffer = new List<BadgeIdlingWrapper>(Math.Max((int)MaxIdleInstanceCount, 1));

            _tmCounter = new DispatcherTimer();
            _tmCounter.Tick += UpdateGameTimeCounter;
            _tmCounter.Interval = new TimeSpan(0, 1, 0);

            await Proceed();
            _tmCounter.Start();
        }

        private async Task Proceed()
        {
            if (_currentMode == IdleMode.All)
            {
                foreach (BadgeModel badge in _account.IdleQueueBadges)
                    await AddGame(badge);
            }
            else if (_currentMode == IdleMode.PeriodicSwitch)
            {
                var repeats = 0;
                var repeatCount = Math.Max(PeriodicSwitchRepeatCount, (byte)1);

                int sec = Math.Max(SwitchMinutes * 60 + SwitchSeconds, DefaultSwitchSeconds);
                var ts = TimeSpan.FromSeconds(sec);

                do
                {
                    repeats++;
                    int idx = 0;
                    while (idx < _account.IdleQueueBadges.Count && IsActive)
                    {
                        var badge = _account.IdleQueueBadges[idx];

                        badge.CardIdleProcess.Start();

                        await Task.Delay(ts);

                        badge.CardIdleProcess.Stop();

                        await Task.Delay(4000);
                        idx++;
                    }
                }
                while (IsActive && (_account.IdleQueueBadges.Count > 0) && (repeats < repeatCount));
            }
            else
            {
                var trial = _account.IdleQueueBadges.Where(IsTrial).ToArray();

                if (_badgeBuffer.Count == 0)
                {
                    if (_currentMode == IdleMode.OneByOne)
                    {
                        var next = _account.IdleQueueBadges.FirstOrDefault(b => b.RemainingCard > 0);
                        if (next != null)
                            await AddGame(next);
                    }
                    else
                    {
                        var next =
                            _account.IdleQueueBadges.FirstOrDefault(b => IsTrial(b) == false && b.RemainingCard > 0);

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
            }

            if (_badgeBuffer.Count == 0)
            {
                IsActive = false;
                _tmCounter.Stop();
            }
        }

        private async Task AddGame(BadgeModel next, bool trial = false)
        {
            if (false == IsActive || _badgeBuffer.Any(w => w.Badge == next) || _badgeBuffer.Count == _badgeBuffer.Capacity)
                return;

            _badgeBuffer.Add(new BadgeIdlingWrapper { Badge = next, IsTrial = trial, Hours = next.HoursPlayed });
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
                if (false == IsActive)
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
                var hours = item.Hours + item.Minutes / 60.0;
                if (hours >= TrialPeriod)
                {
                    item.Badge.HoursPlayed = hours;
                    //item.Badge.CardIdleProcess.Stop();                    
                }
            }
        }

        private void BadgeHoursChanged(object sender, PropertyChangedEventArgs e)
        {
            var badge = (BadgeModel)sender;
            if (e.PropertyName == "HoursPlayed")
            {
                // Steam has a delay in card drops for trial games (refund condition)
                if (IsTrial(badge) == false)
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
            _badgeBuffer.RemoveAll(b => b.Badge == badge);
            badge.IdleStopped -= BadgeIdleProcessStopped;
            badge.PropertyChanged -= BadgeHoursChanged;
            badge.PropertyChanged -= BadgeQueueStateChanged;
        }

        public void Stop()
        {
            if (IsActive == false)
                return;

            IsActive = false;
            _tmCounter.Stop();

            for (int i = _badgeBuffer.Count - 1; i >= 0; i--)
            {
                var badge = _badgeBuffer[i].Badge;
                RemoveGame(badge);
                badge.CardIdleProcess.Stop();
            }
        }
    }
}
