using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace CardIdleRemastered
{
    public class SteamParser
    {
        public static async Task<string> DownloadString(string url)
        {
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            return await wc.DownloadStringTaskAsync(url);
        }

        public static async Task<string> DownloadStringWithAuth(string url)
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
            return document.DocumentNode.SelectSingleNode("//div[@class=\"responsive_menu_user_area\"]") != null;
        }

        private static Dictionary<string, GameIdentity> _appsCache;
        public static async Task<IDictionary<string, GameIdentity>> GetSteamApps()
        {
            if (_appsCache == null)
            {
                string response = String.Empty;
                try
                {
                    response = await DownloadString("http://api.steampowered.com/ISteamApps/GetAppList/v2");
                    var appList = JsonConvert.DeserializeObject<SteamAppList>(response);
                    _appsCache = appList.applist.apps.ToDictionary(a => a.appid);
                }
                catch (Exception ex)
                {
                    _appsCache = new Dictionary<string, GameIdentity>();
                    Logger.Exception(ex, "GetSteamApps");
                    if (!String.IsNullOrWhiteSpace(response))
                        File.WriteAllText("AppList.json", response, Encoding.UTF8);
                }
            }
            return _appsCache;
        }

        #region Profiles

        private async Task<HtmlNode> GetSteamHtmlDocument(string profileLink)
        {
            var response = await DownloadStringWithAuth(profileLink);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            return doc.DocumentNode;
        }

        private static void ParseProfile(HtmlNode root, ProfileInfo result)
        {
            // profile background
            HtmlNode html = null;
            var nodes = root.SelectNodes("//div[@class='profile_background_image_content ']");
            if (nodes != null)
                html = nodes.FirstOrDefault();
            if (html != null)
            {
                var bgi = html.Attributes["style"].Value;
                int lp = bgi.IndexOf('(') + 1;
                int rp = bgi.IndexOf(')');
                string src = bgi.Substring(lp, rp - lp);
                result.BackgroundUrl = src;
            }
            else
            {
                result.BackgroundUrl = null;
            }

            // avatar
            html = root.SelectNodes("//div[@class='playerAvatarAutoSizeInner']").FirstOrDefault();
            if (html != null)
            {
                string src = html.ChildNodes["img"].Attributes["src"].Value;
                result.AvatarUrl = src;
            }

            // user name
            html = root.SelectNodes("//span[@class='actual_persona_name']").FirstOrDefault();
            if (html != null)
            {
                result.UserName = html.InnerText;
            }

            // user level
            // same css class for user level and friends level
            var levels = root.SelectNodes("//span[@class='friendPlayerLevelNum']").ToList();
            if (levels.Count > 0)
            {
                result.Level = levels[0].InnerText;
            }

            // favorite badge
            var badge = root.SelectSingleNode("//div[@class='profile_header_badge']");
            if (badge != null)
            {
                var badgeImage = badge.SelectSingleNode("//img[contains(@class, 'badge_icon')]");
                if (badgeImage != null)
                    result.BadgeUrl = badgeImage.Attributes["src"].Value;

                var badgeTitle = badge.SelectSingleNode("//div[@class='favorite_badge_description']//a");
                if (badgeTitle != null)
                    result.BadgeTitle = badgeTitle.InnerText.Trim();
            }
        }

        public async Task<ProfileInfo> LoadProfileAsync(string profileLink)
        {
            var html = await GetSteamHtmlDocument(profileLink);

            var result = new ProfileInfo();

            ParseProfile(html, result);

            return result;
        }

        public async Task<CardIdleProfileInfo> LoadCardIdleProfileAsync()
        {
            string url = "https://steamcommunity.com/profiles/76561198801350858/";

            var html = await GetSteamHtmlDocument(url);

            var result = new CardIdleProfileInfo();
            result.ProfileUrl = url;

            ParseProfile(html, result);

            var titleNode = html.SelectSingleNode("//div[contains(@class, 'profile_customization_header')]");
            if (titleNode != null)
                result.MessageTitle = titleNode.InnerText.Replace("&amp;", "&").Trim();

            var messageNode = html.SelectSingleNode("//div[contains(@class, 'showcase_notes')]");
            if (messageNode != null)
                result.SetMessage(messageNode.InnerText.Replace("&amp;", "&").Trim());


            return result;
        }

        #endregion

        #region Badges

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
                HtmlNode craftNode = null;

                int current = 0, total = 0;
                if (progressNode != null)
                {
                    Match m = Regex.Match(progressNode.InnerText, @"([0-9]+)\s.+\s([0-9]+)");
                    string gCurrent = m.Groups[1].Value;
                    string gTotal = m.Groups[2].Value;
                    int.TryParse(gCurrent, out current);
                    int.TryParse(gTotal, out total);

                    craftNode = progressNode.SelectSingleNode("a[@class='badge_craft_button']");
                }

                var unlockedNode = badge.SelectSingleNode(".//div[@class=\"badge_info_title\"]");
                string unlockedName = null;
                if (unlockedNode != null)
                    unlockedName = unlockedNode.InnerText.Trim();

                var badgeModel = new BadgeModel(appid, name, cards, hours, current, total)
                                 {
                                     UnlockedBadge = unlockedName,
                                     CanCraft = craftNode != null
                                 };

                badges.Add(badgeModel);
            }
        }

        #endregion

        #region Showcases

        /// <summary>
        /// Loads common and foil badges for a single game
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<BadgeShowcase> GetBadgeShowcase(string appId, IDictionary<string, string> args)
        {
            string site = "http://www.st3amcard3xchang3.n3t/ind3x.php?gam3pag3-appid-".Replace("3", "e") + appId;

            var dataRequest = (HttpWebRequest)HttpWebRequest.Create(site);
            dataRequest.CookieContainer = new CookieContainer();

            var dataResponse = (HttpWebResponse)(await dataRequest.GetResponseAsync());

            string response;
            using (var responseRdr = new StreamReader(dataResponse.GetResponseStream(), Encoding.UTF8))
            {
                response = responseRdr.ReadToEnd();
            }

            var showcase = new BadgeShowcase(appId, appId);

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // parse common badges
            HtmlNode badgesDiv = doc.DocumentNode.SelectSingleNode("//div[@id='badges']");

            if (badgesDiv != null)
                badgesDiv = badgesDiv.SelectSingleNode("div[@class='showcase-element-container badge']");

            if (badgesDiv != null)
            {
                var levels = ParseBadgesContainer(badgesDiv).Take(5);
                foreach (var item in levels)
                    showcase.CommonBadges.Add(item);
            }

            // parse foil badge
            HtmlNode foilDiv = doc.DocumentNode.SelectSingleNode("//div[@id='foilbadges']");

            if (foilDiv != null)
                foilDiv = foilDiv.SelectSingleNode("div[@class='showcase-element-container badge']");

            if (foilDiv != null)
            {
                showcase.FoilBadge = ParseBadgesContainer(foilDiv).FirstOrDefault();
                if (showcase.FoilBadge != null)
                    showcase.FoilBadge.Level = "Foil";
            }

            return showcase;
        }

        private IEnumerable<BadgeLevelData> ParseBadgesContainer(HtmlNode badgesDiv)
        {
            foreach (HtmlNode showcaseDiv in badgesDiv.SelectNodes("div[@class='showcase-element']"))
            {
                var img = showcaseDiv.SelectSingleNode("img[@class='element-image']");
                if (img == null)
                    continue;

                var levelSpan = showcaseDiv.SelectSingleNode("span[@class='element-experience']");

                string level = levelSpan.InnerText;
                int idx = level.IndexOf("XP", 0, StringComparison.InvariantCultureIgnoreCase);
                if (idx > 0)
                    level = level.Substring(0, idx);

                yield return new BadgeLevelData
                             {
                                 PictureUrl = img.GetAttributeValue("src", ""),
                                 Name = img.GetAttributeValue("alt", ""),
                                 Level = level
                             };
            }
        }

        #endregion

        public async Task<ReleaseInfo> GetLatestCardIdlerRelease()
        {
            ReleaseInfo release = null;
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            var response = await DownloadString("https://github.com/AlexanderSharykin/CardIdleRemastered/releases");
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // profile background
            HtmlNode html = null;
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='release-header']");
            if (nodes != null)
                html = nodes.FirstOrDefault();
            if (html != null)
            {
                release = new ReleaseInfo();
                release.Title = html.SelectSingleNode("//h1[contains(@class, 'release-title')]").InnerText.Trim();
                release.Date = html.SelectSingleNode("//relative-time").InnerText;
            }

            return release;
        }
    }
}
