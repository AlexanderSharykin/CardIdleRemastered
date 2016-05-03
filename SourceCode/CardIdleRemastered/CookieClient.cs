using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CardIdleRemastered.Properties;

namespace CardIdleRemastered
{
    public class CookieClient : WebClient
    {
        private readonly CookieContainer Cookie;

        public CookieClient()
        {
            Cookie = GenerateCookies();
            Encoding = Encoding.UTF8;
        }

        private CookieContainer GenerateCookies()
        {
            var cookies = new CookieContainer();
            var target = new Uri("http://steamcommunity.com");
            cookies.Add(new Cookie("sessionid", Settings.Default.sessionid) { Domain = target.Host });
            cookies.Add(new Cookie("steamLogin", Settings.Default.steamLogin) { Domain = target.Host });
            cookies.Add(new Cookie("steamparental", Settings.Default.steamparental) { Domain = target.Host });
            cookies.Add(new Cookie("steamRememberLogin", Settings.Default.steamRememberLogin) { Domain = target.Host });
            cookies.Add(new Cookie(GetSteamMachineAuthCookieName(), Settings.Default.steamMachineAuth) { Domain = target.Host });
            return cookies;
        }

        private static string GetSteamMachineAuthCookieName()
        {
            if (Settings.Default.steamLogin != null && Settings.Default.steamLogin.Length > 17)
                return string.Format("steamMachineAuth{0}", Settings.Default.steamLogin.Substring(0, 17));
            return "steamMachineAuth";
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
                (request as HttpWebRequest).CookieContainer = Cookie;
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            HttpWebResponse baseResponse = base.GetWebResponse(request) as HttpWebResponse;

            if (baseResponse == null)
                return null;

            var cookies = baseResponse.Cookies;

            // Check, if cookie should be deleted. This means that sessionID is now invalid and user has to log in again.            
            if (cookies.Count > 0)
            {
                // fix from https://github.com/jshackles/idle_master/pull/197
                var login = cookies["steamLogin"];
                if (login != null && login.Value == "deleted")
                {
                    Settings.Default.sessionid = string.Empty;
                    Settings.Default.steamLogin = string.Empty;
                    Settings.Default.steamparental = string.Empty;
                    Settings.Default.steamMachineAuth = string.Empty;
                    Settings.Default.steamRememberLogin = string.Empty;
                    Settings.Default.Save();
                }
            }

            return baseResponse;
        }

        public static async Task<string> GetHttpAsync(string url, int count = 3)
        {
            var client = new CookieClient();
            var content = string.Empty;
            try
            {
                // If user is NOT authenticated (cookie got deleted in GetWebResponse()), return empty result
                if (String.IsNullOrEmpty(Settings.Default.sessionid))
                {
                    return string.Empty;
                }

                content = await client.DownloadStringTaskAsync(url);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CookieClient -> GetHttpAsync, for url = " + url);
            }
            return content;
        }
    }
}
