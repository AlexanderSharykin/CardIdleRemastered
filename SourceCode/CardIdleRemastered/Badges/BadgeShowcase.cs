using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CardIdleRemastered
{
    public class BadgeShowcase : BadgeIdentity
    {
        private bool _isCompleted;
        private bool _isBookmarked;
        private bool _isMarketable = true;
        private bool _isOwned;

        public BadgeShowcase(string appId, string title)
            : base(appId, title)
        {
            CommonBadges = new ObservableCollection<BadgeLevelData>();
        }

        public BadgeShowcase(BadgeModel badge)
            : this(badge.AppId, badge.Title)
        {
        }

        public override string NavigationUrl
        {
            get { return "https://www.steamcardexchange.net/index.php?inventorygame-appid-" + AppId; }
        }

        public ObservableCollection<BadgeLevelData> CommonBadges { get; private set; }

        public BadgeLevelData FoilBadge { get; set; }

        public IEnumerable<BadgeLevelData> Levels
        {
            get
            {
                foreach (var badge in CommonBadges)
                    yield return badge;
                if (FoilBadge != null)
                    yield return FoilBadge;
            }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
            }
        }

        public bool IsBookmarked
        {
            get { return _isBookmarked; }
            set
            {
                _isBookmarked = value;
                OnPropertyChanged();
            }
        }

        public bool IsMarketable
        {
            get { return _isMarketable; }
            set
            {
                if (_isMarketable == value)
                    return;
                _isMarketable = value;
                OnPropertyChanged();
            }
        }

        public bool IsOwned
        {
            get { return _isOwned; }
            set
            {
                if (_isOwned == value)
                    return;
                _isOwned = value;
                OnPropertyChanged();
            }
        }

        public override bool CheckProperty(Enum property)
        {
            if (Equals(property, ShowcaseProperty.Completed))
                return IsCompleted;

            if (Equals(property, ShowcaseProperty.Bookmarked))
                return IsBookmarked;

            if (Equals(property, ShowcaseProperty.Collected))
                return CanCraft;

            if (Equals(property, ShowcaseProperty.Marketable))
                return IsMarketable;

            if (Equals(property, ShowcaseProperty.Owned))
                return IsOwned;

            return false;
        }
    }
}
