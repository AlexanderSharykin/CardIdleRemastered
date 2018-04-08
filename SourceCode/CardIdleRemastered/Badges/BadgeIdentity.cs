using System;

namespace CardIdleRemastered
{
    public abstract class BadgeIdentity : ObservableModel
    {
        private bool _canCraft;

        protected BadgeIdentity()
        {
        }

        protected BadgeIdentity(string appId, string title)
        {
            AppId = appId;
            Title = title;
        }

        public string AppId { get; set; }

        public string Title { get; set; }

        /// <summary>
        /// Name of a completed badge (if any)
        /// </summary>
        public string UnlockedBadge { get; set; }

        /// <summary>
        /// Indication of a badge which can be crafted
        /// </summary>
        public bool CanCraft
        {
            get { return _canCraft; }
            set
            {
                _canCraft = value;
                OnPropertyChanged();
            }
        }

        public string StorePageUrl
        {
            get { return "http://store.steampowered.com/app/" + AppId; }
        }

        public string ImageUrl
        {
            get { return "http://cdn.akamai.steamstatic.com/steam/apps/" + AppId + "/header_292x136.jpg"; }
        }

        public abstract bool CheckProperty(Enum property);

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Title) ? AppId : Title;
        }
    }
}
