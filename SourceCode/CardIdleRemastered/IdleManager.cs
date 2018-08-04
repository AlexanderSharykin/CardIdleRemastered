using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CardIdleRemastered
{
    public class IdleManager : ObservableModel
    {
        private bool _isActive;

        private readonly Random _rand = new Random();

        private IdleMode _currentMode;
        private List<BadgeIdlingWrapper> _badgeBuffer;
        private DispatcherTimer _tmCounter;

        public IdleManager()
        {
            IdleQueueBadges = new ObservableCollection<BadgeModel>();
        }

        public ObservableCollection<BadgeModel> IdleQueueBadges { get; private set; }

        public bool IsActive
        {
            get { return _isActive; }
            private set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public IdleMode Mode { get; set; }

        public byte MaxIdleInstanceCount { get; set; }

        public byte PeriodicSwitchRepeatCount { get; set; }

        public double TrialPeriod { get; set; }

        public byte SwitchMinutes { get; set; }

        public byte SwitchSeconds { get; set; }

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
                foreach (BadgeModel badge in IdleQueueBadges)
                    await AddGame(badge);
            }
            else if (_currentMode == IdleMode.PeriodicSwitch)
            {
                var repeats = 0;
                var repeatCount = Math.Max(PeriodicSwitchRepeatCount, (byte)1);

                int sec = Math.Max(SwitchMinutes * 60 + SwitchSeconds, AppConstants.DefaultSwitchSeconds);
                var ts = TimeSpan.FromSeconds(sec);

                do
                {
                    repeats++;
                    int idx = 0;
                    while (idx < IdleQueueBadges.Count && IsActive)
                    {
                        var badge = IdleQueueBadges[idx];

                        badge.CardIdleProcess.Start();

                        await Task.Delay(ts);

                        badge.CardIdleProcess.Stop();

                        await Task.Delay(4000);
                        idx++;
                    }
                }
                while (IsActive && (IdleQueueBadges.Count > 0) && (repeats < repeatCount));
            }
            else
            {
                var trial = IdleQueueBadges.Where(IsTrial).ToArray();

                if (_badgeBuffer.Count == 0)
                {
                    if (_currentMode == IdleMode.OneByOne)
                    {
                        var next = IdleQueueBadges.FirstOrDefault(b => b.RemainingCard > 0);
                        if (next != null)
                            await AddGame(next);
                    }
                    else
                    {
                        var next =
                            IdleQueueBadges.FirstOrDefault(b => IsTrial(b) == false && b.RemainingCard > 0);

                        if (Mode == IdleMode.TrialFirst && trial.Length > 0 || next == null)
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
