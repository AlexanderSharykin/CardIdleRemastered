using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered
{
    public interface ISettingsStorage
    {
        string SessionId { get; set; }
        string SteamLogin { get; set; }
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

        int IdleMode { get; set; }
        string BadgeFilter { get; set; }
        byte MaxIdleProcessCount { get; set; }
        byte SwitchMinutes { get; set; }
        byte SwitchSeconds { get; set; }        

        StringCollection IdleQueue { get; }

        StringCollection Blacklist { get; }

        StringCollection Games { get; }

        StringCollection AppBrushes { get; }

        void Save();
    }
}
