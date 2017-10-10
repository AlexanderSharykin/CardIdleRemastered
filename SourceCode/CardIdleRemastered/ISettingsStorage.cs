using System.Collections.Specialized;

namespace CardIdleRemastered
{
    public interface ISettingsStorage
    {
        string SessionId { get; set; }
        string SteamLogin { get; set; }
        string SteamLoginSecure { get; set; }
        string SteamProfileUrl { get; set; }
        string SteamParental { get; set; }
        string SteamRememberLogin { get; set; }
        string MachineAuth { get; set; }

        bool IgnoreClient { get; set; }

        string SteamAvatarUrl { get; set; }
        string SteamBackgroundUrl { get; set; }
        string SteamUserName { get; set; }
        string SteamLevel { get; set; }
        string CustomBackgroundUrl { get; set; }
        string SteamBadgeUrl { get; set; }
        string SteamBadgeTitle { get; set; }

        int IdleMode { get; set; }
        string BadgeFilter { get; set; }
        string ShowcaseFilter { get; set; }
        byte MaxIdleProcessCount { get; set; }
        byte PeriodicSwitchRepeatCount { get; set; }
        byte SwitchMinutes { get; set; }
        byte SwitchSeconds { get; set; }

        bool AllowShowcaseSync { get; set; }
        bool ShowInTaskbar { get; set; }

        StringCollection IdleQueue { get; }

        StringCollection Blacklist { get; }

        StringCollection ShowcaseBookmarks { get; }

        StringCollection Games { get; }

        StringCollection AppBrushes { get; }

        void Save();
    }
}
