using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CardIdleRemastered
{
    public class BadgeShowcase : BadgeIdentity
    {
        private bool _isCompleted;
        private bool _isBookmarked;

        public BadgeShowcase(string appId, string title)
            : base(appId, title)
        {
            CommonBadges = new ObservableCollection<BadgeLevelData>();
        }

        public BadgeShowcase(BadgeModel badge)
            : this(badge.AppId, badge.Title)
        {
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

        public override bool CheckProperty(Enum property)
        {
            if (Equals(property, ShowcaseProperty.Completed))
                return IsCompleted;

            if (Equals(property, ShowcaseProperty.Bookmarked))
                return IsBookmarked;

            if (Equals(property, ShowcaseProperty.Collected))
                return CanCraft;

            return false;
        }
    }
}
