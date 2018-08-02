using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered
{
    public class GameIdentity
    {
        public string appid { get; set; }

        public string name { get; set; }

        private string _title;
        public string Title
        {
            get
            {
                if (_title == null)
                {
                    _title = appid + ". " + name;
                }
                return _title;
            }
        }
    }

    public class SteamApps
    {
        public IList<GameIdentity> apps { get; set; }
    }

    public class SteamAppList
    {
        public SteamApps applist { get; set; }
    }
}
