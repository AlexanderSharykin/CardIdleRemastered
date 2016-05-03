using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;

namespace CardIdleRemastered
{
    public class SteamParser
    {
        public async Task<string> DownloadString(string url)
        {
            return await new WebClient().DownloadStringTaskAsync(url);
        }

        public async Task<string> DownloadStringWithAuth(string url)
        {
            return await new CookieClient().DownloadStringTaskAsync(url);
        }

        public async Task<bool> IsLogined(string profileUrl)
        {
            var response = await DownloadStringWithAuth(profileUrl);
            if (String.IsNullOrWhiteSpace(response))
                return false;
            var document = new HtmlDocument();
            document.LoadHtml(response);
            return document.DocumentNode.SelectSingleNode("//a[@class=\"global_action_link\"]") == null;
        }  

        public async Task<BadgeModel> GetGameInfo(int id)
        {
            return await GetGameInfo(id.ToString());
        }

        public async Task<BadgeModel> GetGameInfo(string id)
        {
            var game = new BadgeModel(id, "Title", "0", "0");            

            // getting game title from store page
            var response = await DownloadString(game.StorePageUrl);
            var document = new HtmlDocument();
            document.LoadHtml(response);
            var titleNode = document.DocumentNode.SelectSingleNode("//div[@class=\"apphub_AppName\"]");
            if (titleNode != null)
                game.Title = titleNode.InnerText;

            return game;
        }

        public async Task LoadProfileAsync(AccountModel account)
        {
            var profileLink = account.ProfileUrl;
            var response = await DownloadStringWithAuth(profileLink);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // profile background
            HtmlNode html = null;
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='profile_background_image_content ']");
            if (nodes != null)
                html = nodes.FirstOrDefault();
            if (html != null)
            {
                var bgi = html.Attributes["style"].Value;
                int lp = bgi.IndexOf('(') + 1;
                int rp = bgi.IndexOf(')');
                string src = bgi.Substring(lp, rp - lp);
                account.BackgroundUrl = src;
            }
            else
            {
                account.BackgroundUrl = null;
            }

            // avatar
            html = doc.DocumentNode.SelectNodes("//div[@class='playerAvatarAutoSizeInner']").FirstOrDefault();
            if (html != null)
            {
                string src = html.ChildNodes["img"].Attributes["src"].Value;
                account.AvatarUrl = src;
            }

            // user name
            html = doc.DocumentNode.SelectNodes("//span[@class='actual_persona_name']").FirstOrDefault();
            if (html != null)
            {
                account.UserName = html.InnerText;
            }

            // user level
            // same css class for user level and friends level
            var levels = doc.DocumentNode.SelectNodes("//span[@class='friendPlayerLevelNum']").ToList();
            if (levels.Count > 0)
            {
                account.Level = levels[0].InnerText;
            }
        }

        public async Task LoadBadgesAsync(AccountModel account)
        {
            var profileLink = account.ProfileUrl + "/badges";
            var pages = new List<string>() { "?p=1" };
            var document = new HtmlDocument();
            int pagesCount = 1;

            try
            {
                // Load Page 1 and check how many pages there are
                var pageUrl = string.Format("{0}/?p={1}", profileLink, 1);
                var response = await DownloadStringWithAuth(pageUrl);

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
                ProcessBadgesOnPage(account, document);

                // Load other pages
                for (var i = 2; i <= pagesCount; i++)
                {

                    // Load Page 2+
                    pageUrl = string.Format("{0}/?p={1}", profileLink, i);
                    response = await DownloadStringWithAuth(pageUrl);
                    // Response should not be empty. User should be unauthorised.
                    if (string.IsNullOrEmpty(response))
                        return;

                    document.LoadHtml(response);

                    // Get all badges from current page
                    ProcessBadgesOnPage(account, document);
                }

                account.UpdateTotalValues();
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
        /// <param name="account"></param>
        /// <param name="document">HTML document (1 page) from x</param>
        private void ProcessBadgesOnPage(AccountModel account, HtmlDocument document)
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

                var badgeInMemory = account.AllBadges.FirstOrDefault(b => b.AppId == appid);
                if (badgeInMemory != null)
                {
                    badgeInMemory.UpdateStats(cards, hours);
                    if (badgeInMemory.RemainingCard == 0)
                    {
                        account.AllBadges.Remove(badgeInMemory);
                        account.IdleQueueBadges.Remove(badgeInMemory);
                    }
                }
                else
                {
                    var b = new BadgeModel(appid, name, cards, hours);

                    if (b.RemainingCard > 0)
                        account.AllBadges.Add(b);
                }
            }
        }
    }
}
