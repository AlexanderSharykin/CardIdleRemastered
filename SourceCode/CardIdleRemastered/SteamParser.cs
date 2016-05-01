using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CardIdleRemastered
{
    public class SteamParser
    {
        public async Task<BadgeModel> GetBadge(int id)
        {
            return await GetBadge(id.ToString());
        }

        public async Task<BadgeModel> GetBadge(string id)
        {
            var game = new BadgeModel(id, "Title", "0", "0");
            // todo do all web access and parsing in specialized classes

            // getting game title from store page
            var response = await new WebClient().DownloadStringTaskAsync(game.StorePageUrl);
            var document = new HtmlDocument();
            document.LoadHtml(response);
            var titleNode = document.DocumentNode.SelectSingleNode("//div[@class=\"apphub_AppName\"]");
            if (titleNode != null)
                game.Title = titleNode.InnerText;

            return game;
        }
    }
}
