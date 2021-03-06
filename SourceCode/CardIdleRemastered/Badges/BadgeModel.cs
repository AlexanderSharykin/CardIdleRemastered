﻿using System;
using System.Globalization;

namespace CardIdleRemastered
{
    public class BadgeModel : BadgeIdentity
    {
        private int _remainingCard;
        private double _hoursPlayed;

        private IdleProcess _idleProcess;
        private bool _hasTrial;
        private bool _isBlacklisted;
        private bool _isInQueue;

        private int _cardsCurrent;
        private string _badgeProgress;
        private int _cardsTotal;

        public BadgeModel()
        {
        }

        public BadgeModel(string id, string title)
            : this(id, title, "0", "0", 0, 0)
        {
        }

        public BadgeModel(string id, string title, string card, string hours)
            : this(id, title, card, hours, 0, 0)
        {
        }

        public BadgeModel(string id, string title, string card, string hours, int cardsCurrent, int cardsTotal)
            : base(id, title)
        {
            UpdateStats(card, hours);

            CardsTotal = cardsTotal;
            CardsCurrent = cardsCurrent;
        }

        public string ProfileUrl { get; set; }

        public string BadgeUrl
        {
            get { return String.Format("{0}/gamecards/", ProfileUrl) + AppId; }
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

        public int CardsCurrent
        {
            get { return _cardsCurrent; }
            set
            {
                _cardsCurrent = value;
                OnPropertyChanged();

                UpdateProgress();
            }
        }

        public int CardsTotal
        {
            get { return _cardsTotal; }
            set
            {
                _cardsTotal = value;
                UpdateProgress();
            }
        }

        private void UpdateProgress()
        {
            if (CardsTotal > 0 && CardsCurrent >= 0)
            {
                BadgeProgress = new string((char)9733, CardsCurrent) +
                                new string((char)9734, CardsTotal - CardsCurrent);
            }
        }

        public string BadgeProgress
        {
            get { return _badgeProgress; }
            private set
            {
                _badgeProgress = value;
                OnPropertyChanged();
            }
        }

        public double HoursPlayed
        {
            get { return _hoursPlayed; }
            set
            {
                _hoursPlayed = value;
                OnPropertyChanged();
            }
        }

        public bool HasTrial
        {
            get { return _hasTrial; }
            set
            {
                _hasTrial = value;
                OnPropertyChanged();
            }
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

        public IdleProcess CardIdleProcess
        {
            get
            {
                if (_idleProcess == null)
                {
                    _idleProcess = new IdleProcess(AppId);
                    _idleProcess.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == "IsRunning")
                            OnPropertyChanged("CardIdleActive");
                    };
                    _idleProcess.IdleStopped += (sender, args) =>
                    {
                        if (IdleStopped != null)
                            IdleStopped(this, EventArgs.Empty);
                    };
                }
                return _idleProcess;
            }
        }

        public event EventHandler IdleStopped;

        public bool CardIdleActive
        {
            get { return CardIdleProcess.IsRunning; }
        }

        public override bool CheckProperty(Enum property)
        {
            if (Equals(property, BadgeProperty.Running))
                return CardIdleActive;

            if (Equals(property, BadgeProperty.Enqueued))
                return IsInQueue;

            if (Equals(property, BadgeProperty.HasTrial))
                return HasTrial;

            if (Equals(property, BadgeProperty.Blacklisted))
                return IsBlacklisted;

            throw new ArgumentException();
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
    }
}
