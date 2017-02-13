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

        public async Task<Dictionary<string, string>> LoadProfileAsync(string profileLink)
        {
            var result = new Dictionary<string, string>();
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
                result["BackgroundUrl"] = src;
            }
            else
            {
                result["BackgroundUrl"] = null;
            }

            // avatar
            html = doc.DocumentNode.SelectNodes("//div[@class='playerAvatarAutoSizeInner']").FirstOrDefault();
            if (html != null)
            {
                string src = html.ChildNodes["img"].Attributes["src"].Value;
                result["AvatarUrl"] = src;
            }

            // user name
            html = doc.DocumentNode.SelectNodes("//span[@class='actual_persona_name']").FirstOrDefault();
            if (html != null)
            {
                result["UserName"] = html.InnerText;                
            }

            // user level
            // same css class for user level and friends level
            var levels = doc.DocumentNode.SelectNodes("//span[@class='friendPlayerLevelNum']").ToList();
            if (levels.Count > 0)
            {
                result["Level"] = levels[0].InnerText;
            }
            return result;
        }

        public async Task<IEnumerable<BadgeModel>> LoadBadgesAsync(string profileLink)
        {
            var document = new HtmlDocument();
            var badges = new List<BadgeModel>();
            int pagesCount = 1;

            try
            {                
                // loading pages with user badges
                for (var p = 1; p <= pagesCount; p++)
                {
                    var pageUrl = string.Format("{0}/badges/?p={1}", profileLink, p);
                    var response = await DownloadStringWithAuth(pageUrl);
                    
                    if (string.IsNullOrEmpty(response))
                        return badges;

                    document.LoadHtml(response);

                    if (p == 1)
                    {
                        // get pages count from navigation tab after first request
                        // possible formats depend on badge count, e.g. < 1 2 3 4 5 > or < 1 2 3 ... 15 >
                        var pageNodes = document.DocumentNode.SelectNodes("//a[@class=\"pagelink\"]");
                        if (pageNodes != null)
                            pagesCount = pageNodes
                                .Select(a => a.Attributes["href"].Value)
                                .Select(s => int.Parse(s.Replace("?p=", "")))
                                .Max();
                    }
                    
                    // add all badges from page to list
                    ProcessBadgesOnPage(badges, document);
                }                
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "LoadBadgesAsync");
                return Enumerable.Empty<BadgeModel>();
            }

            foreach (var badge in badges)            
                badge.ProfileUrl = profileLink;
            
            return badges;
        }

        /// <summary>
        /// Processes all badges on page
        /// </summary>
        /// <param name="badges"></param>
        /// <param name="document">HTML document (1 page) from x</param>
        private void ProcessBadgesOnPage(IList<BadgeModel> badges, HtmlDocument document)
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

                var progressNode = badge.SelectSingleNode(".//div[@class=\"badge_progress_info\"]");

                int current = 0, total = 0;
                if (progressNode != null)
                {
                    Match m = Regex.Match(progressNode.InnerText, @"([0-9]+)\s.+\s([0-9]+)");
                    string gCurrent = m.Groups[1].Value;
                    string gTotal = m.Groups[2].Value;
                    int.TryParse(gCurrent, out current);
                    int.TryParse(gTotal, out total);
                }

                badges.Add(new BadgeModel(appid, name, cards, hours, current, total));
            }
        }
    }
}
