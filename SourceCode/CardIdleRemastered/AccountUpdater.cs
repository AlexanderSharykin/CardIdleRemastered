using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HtmlAgilityPack;
using CardIdleRemastered.Properties;

namespace CardIdleRemastered
{
    public class AccountUpdater
    {
        private AccountModel _account;

        private DispatcherTimer _tmSync;
        private int _counter;
        private DispatcherTimer _tmCounter;
        private TimeSpan _interval;

        public AccountUpdater(AccountModel account)
        {
            _account = account;

            _tmSync = new DispatcherTimer();
            _tmSync.Tick += SyncBanges;
            Interval = new TimeSpan(0, 5, 0);
            //_tmSync.Start();

            _tmCounter = new DispatcherTimer();
            _tmCounter.Tick += UpdateSecondCounter;
            _tmCounter.Interval = new TimeSpan(0, 0, 1);
            //_tmCounter.Start();
        }

        public void Start()
        {            
            if (false == _tmSync.IsEnabled)
                _tmSync.Start();

            if (false == _tmCounter.IsEnabled)
            {
                _counter = 0;
                _tmCounter.Start();
            }            
        }

        public void Stop()
        {            
            _tmSync.Stop();            
            _tmCounter.Stop();
        }

        private void UpdateSecondCounter(object sender, EventArgs eventArgs)
        {            
            _counter++;
            int seconds = (int)Interval.TotalSeconds - _counter;
            if (seconds > 0)
            {
                var ts = TimeSpan.FromSeconds(seconds);
                _account.SyncTime = String.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
            }
            else
                _account.SyncTime = "00:00";
        }

        public TimeSpan Interval
        {
            get { return _interval; }
            set
            {
                _interval = value;
                _tmSync.Interval = _interval;
            }
        }

        private void SyncBanges(object sender, EventArgs eventArgs)
        {            
            _counter = 0;
            Sync();            
        }

        public async Task Sync()
        {
            var tBadges = LoadBadgesAsync();
            var tProfile = LoadProfileAsync();
            await Task.WhenAll(tBadges, tProfile);
        }

        private async Task LoadBadgesAsync()
        {
            var profileLink = _account.ProfileUrl + "/badges";
            var pages = new List<string>() { "?p=1" };
            var document = new HtmlDocument();
            int pagesCount = 1;

            try
            {
                // Load Page 1 and check how many pages there are
                var pageURL = string.Format("{0}/?p={1}", profileLink, 1);
                var response = await CookieClient.GetHttpAsync(pageURL);

                // Response should not be empty. User should be unauthorised.
                if (string.IsNullOrEmpty(response))                
                    return;
                
                document.LoadHtml(response);

                // If user is authenticated, check page count. If user is not authenticated, pages are different.
                var pageNodes = document.DocumentNode.SelectNodes("//a[@class=\"pagelink\"]");
                if (pageNodes != null)
                {
                    pages.AddRange(pageNodes.Select(p => p.Attributes["href"].Value).Distinct());
                    pages = pages.Distinct().ToList();
                }

                string lastpage = pages.Last().Replace("?p=", "");
                pagesCount = Convert.ToInt32(lastpage);

                // Get all badges from current page
                ProcessBadgesOnPage(document);

                // Load other pages
                for (var i = 2; i <= pagesCount; i++)
                {

                    // Load Page 2+
                    pageURL = string.Format("{0}/?p={1}", profileLink, i);
                    response = await CookieClient.GetHttpAsync(pageURL);
                    // Response should not be empty. User should be unauthorised.
                    if (string.IsNullOrEmpty(response))                    
                        return;         
           
                    document.LoadHtml(response);

                    // Get all badges from current page
                    ProcessBadgesOnPage(document);
                }

               _account.UpdateTotalValues();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace, ex.GetType().Name);
                return;
            }
        }

        /// <summary>
        /// Processes all badges on page
        /// </summary>
        /// <param name="document">HTML document (1 page) from x</param>
        private void ProcessBadgesOnPage(HtmlDocument document)
        {
            var nodes = document.DocumentNode.SelectNodes("//div[@class=\"badge_row is_link\"]");
            if (nodes == null)
                return;
            foreach (var badge in nodes)
            {
                var appIdNode = badge.SelectSingleNode(".//a[@class=\"badge_row_overlay\"]").Attributes["href"].Value;
                var appid = Regex.Match(appIdNode, @"gamecards/(\d+)/").Groups[1].Value;

                if (string.IsNullOrWhiteSpace(appid) || appid == "368020" || appid == "335590" || appIdNode.Contains("border=1"))
                {
                    continue;
                }

                var hoursNode = badge.SelectSingleNode(".//div[@class=\"badge_title_stats_playtime\"]");
                var hours = hoursNode == null ? string.Empty : Regex.Match(hoursNode.InnerText, @"[0-9\.,]+").Value;

                var nameNode = badge.SelectSingleNode(".//div[@class=\"badge_title\"]");
                var name = WebUtility.HtmlDecode(nameNode.FirstChild.InnerText).Trim();

                var cardNode = badge.SelectSingleNode(".//span[@class=\"progress_info_bold\"]");
                var cards = cardNode == null ? string.Empty : Regex.Match(cardNode.InnerText, @"[0-9]+").Value;

                var badgeInMemory =_account.AllBadges.FirstOrDefault(b => b.AppId == appid);                
                if (badgeInMemory != null)
                {
                    badgeInMemory.UpdateStats(cards, hours);                    
                    if (badgeInMemory.RemainingCard == 0)
                    {
                        _account.AllBadges.Remove(badgeInMemory);
                        _account.IdleQueueBadges.Remove(badgeInMemory);
                    }
                }
                else
                {
                    var b = new BadgeModel(appid, name, cards, hours);                    

                    if (b.RemainingCard > 0)
                    {
                        b.AppImage = App.LoadImage(b.ImageUrl);
                       _account.AllBadges.Add(b);
                    }
                }
            }
        }

        private async Task LoadProfileAsync()
        {
            var profileLink = _account.ProfileUrl;
            var response = await CookieClient.GetHttpAsync(profileLink);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // profile background
            HtmlNode html = null;
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='profile_background_image_content ']");
            if (nodes!= null) 
                html = nodes.FirstOrDefault();
            if (html != null)
            {
                var bgi = html.Attributes["style"].Value;
                int lp = bgi.IndexOf('(') + 1;
                int rp = bgi.IndexOf(')');
                string src = bgi.Substring(lp, rp - lp);
                BitmapImage img = App.LoadImage(src);                
                _account.Background = img;
                _account.BackgroundUrl = src;
            }
            else
            {
                _account.Background = null;
                _account.BackgroundUrl = null;
            }

            // avatar
            html = doc.DocumentNode.SelectNodes("//div[@class='playerAvatarAutoSizeInner']").FirstOrDefault();
            if (html != null)
            {
                string src = html.ChildNodes["img"].Attributes["src"].Value;
                _account.Avatar = App.LoadImage(src);
               _account.AvatarUrl = src;
            }
            

            // user name
            html = doc.DocumentNode.SelectNodes("//span[@class='actual_persona_name']").FirstOrDefault();
            if (html != null)
            {
               _account.UserName = html.InnerText;
            }

            // user level
            // same css class for user level and friends level
            var levels = doc.DocumentNode.SelectNodes("//span[@class='friendPlayerLevelNum']").ToList();
            if (levels.Count > 0)
            {
               _account.Level = levels[0].InnerText;
            }
        }
    }
}
