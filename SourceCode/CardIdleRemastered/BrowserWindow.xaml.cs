using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using IdleMaster;
using CardIdleRemastered.Properties;

namespace CardIdleRemastered
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BrowserWindow : Window
    {
        public BrowserWindow()
        {
            InitializeComponent();
        }

        public int SecondsWaiting = 30;

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InternetSetOption(int hInternet, int dwOption, string lpBuffer, int dwBufferLength);

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InternetSetCookie(string lpszUrlName, string lpszCookieName, string lpszCookieData);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Remove any existing session state data
            InternetSetOption(0, 42, null, 0);

            // Delete Steam cookie data from the browser control
            InternetSetCookie("http://steamcommunity.com", "sessionid", ";expires=Mon, 01 Jan 0001 00:00:00 GMT");
            InternetSetCookie("http://steamcommunity.com", "steamLogin", ";expires=Mon, 01 Jan 0001 00:00:00 GMT");
            InternetSetCookie("http://steamcommunity.com", "steamRememberLogin",
                ";expires=Mon, 01 Jan 0001 00:00:00 GMT");

            // When the form is loaded, navigate to the Steam login page using the web browser control
            wbAuth.Navigate("https://steamcommunity.com/login/home/?goto=my/profile", "_self", null,
                "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko");
        }

        // Imports the InternetGetCookieEx function from wininet.dll which allows the application to access the cookie data from the web browser control
        // Reference: http://stackoverflow.com/questions/3382498/is-it-possible-to-transfer-authentication-from-webbrowser-to-webrequest
        [DllImport("wininet.dll", SetLastError = true)]
        public static extern bool InternetGetCookieEx(
            string url,
            string cookieName,
            StringBuilder cookieData,
            ref int size,
            int dwFlags,
            IntPtr lpReserved);

        // Assigns the hex value for the DLL flag that allows Idle Master to be able to access cookie data marked as "HTTP Only"
        private const int InternetCookieHttponly = 0x2000;

        // This function returns cookie data based on a uniform resource identifier
        public static CookieContainer GetUriCookieContainer(Uri uri)
        {
            // First, create a null cookie container
            CookieContainer cookies = null;

            // Determine the size of the cookie
            var datasize = 8192*16;
            var cookieData = new StringBuilder(datasize);

            // Call InternetGetCookieEx from wininet.dll
            if (
                !InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
            {
                if (datasize < 0)
                    return null;
                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(
                    uri.ToString(),
                    null, cookieData,
                    ref datasize,
                    InternetCookieHttponly,
                    IntPtr.Zero))
                    return null;
            }

            // If the cookie contains data, add it to the cookie container
            if (cookieData.Length > 0)
            {
                cookies = new CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }

            // Return the cookie container
            return cookies;
        }

        // This code executes each time the web browser control is in the process of navigating
        private void wbAuth_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            // Get the url that's being navigated to
            var url = e.Uri.AbsoluteUri;

            // Check to see if the page it's navigating to isn't the Steam login page or related calls
            if (url != "https://steamcommunity.com/login/home/?goto=my/profile" &&
                url != "https://store.steampowered.com/login/transfer" &&
                url != "https://store.steampowered.com//login/transfer" && url.StartsWith("javascript:") == false &&
                url.StartsWith("about:") == false)
            {
                //e.Cancel = true;
            }
        }

        private void wbAuth_Navigated(object sender, NavigationEventArgs e)
        {
            // Find the page header, and remove it.  This gives the login form a more streamlined look.
            dynamic htmldoc = wbAuth.Document;
            dynamic globalHeader = htmldoc.GetElementById("global_header");
            if (globalHeader != null)
            {
                try
                {
                    globalHeader.parentNode.removeChild(globalHeader);
                }
                catch (Exception)
                {

                }

            }

            // Get the URL of the page that just finished loading
            var src = wbAuth.Source;
            var url = wbAuth.Source.AbsoluteUri;

            // If the page it just finished loading is the login page
            if (url == "https://steamcommunity.com/login/home/?goto=my/profile" ||
                url == "https://store.steampowered.com/login/transfer" ||
                url == "https://store.steampowered.com//login/transfer")
            {
                // Get a list of cookies from the current page
                CookieContainer container = GetUriCookieContainer(src);
                var cookies = container.GetCookies(src);
                foreach (Cookie cookie in cookies)
                {
                    if (cookie.Name.StartsWith("steamMachineAuth"))
                        Settings.Default.steamMachineAuth = cookie.Value;
                }
            }
            // If the page it just finished loading isn't the login page
            else if (url.StartsWith("javascript:") == false && url.StartsWith("about:") == false)
            {

                try
                {
                    dynamic parentalNotice = htmldoc.GetElementById("parental_notice");
                    if (parentalNotice != null)
                    {
                        if (parentalNotice.OuterHtml != "")
                        {
                            // Steam family options enabled
                            //wbAuth.Show();
                            Width = 1000;
                            Height = 350;
                            return;
                        }
                    }
                }
                catch (Exception)
                {

                }

                // Get a list of cookies from the current page
                var container = GetUriCookieContainer(src);
                var cookies = container.GetCookies(src);

                // Go through the cookie data so that we can extract the cookies we are looking for
                foreach (Cookie cookie in cookies)
                {
                    // Save the "sessionid" cookie
                    if (cookie.Name == "sessionid")
                    {
                        Settings.Default.sessionid = cookie.Value;
                    }

                    // Save the "steamLogin" cookie and construct and save the user's profile link
                    else if (cookie.Name == "steamLogin")
                    {
                        string login = cookie.Value;
                        Settings.Default.steamLogin = login;

                        var steamId = WebUtility.UrlDecode(login);
                        var index = steamId.IndexOf('|');
                        if (index >= 0)
                            steamId = steamId.Remove(index);                        
                        Settings.Default.myProfileURL = "http://steamcommunity.com/profiles/" + steamId;
                    }

                    // Save the "steamparental" cookie"
                    else if (cookie.Name == "steamparental")
                    {
                        Settings.Default.steamparental = cookie.Value;
                    }

                    else if (cookie.Name == "steamRememberLogin")
                    {
                        Settings.Default.steamRememberLogin = cookie.Value;
                    }
                }

                // Save all of the data to the program settings file, and close this form
                Settings.Default.Save();
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool connected = !string.IsNullOrWhiteSpace(Settings.Default.sessionid) &&
                             !string.IsNullOrWhiteSpace(Settings.Default.steamLogin);

            if (connected)            
                App.CardIdle.OpenAccount();
            
        }
    }
}
