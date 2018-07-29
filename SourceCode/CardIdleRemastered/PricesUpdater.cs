using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using CardIdleRemastered.Badges;

namespace CardIdleRemastered
{
    public class PricesUpdater
    {
        private Dictionary<string, BadgeStockModel> _prices;

        public FileStorage Storage { get; set; }

        public Dictionary<string, BadgeStockModel> Prices
        {
            get
            {
                if (_prices == null)
                {
                    var values = Storage.ReadContent();
                    var js = new JavaScriptSerializer();
                    _prices = js.Deserialize<Dictionary<string, BadgeStockModel>>(values);
                }
                return _prices;
            }
        }

        public async Task<bool> DownloadCatalog()
        {
            try
            {
                string values = await new SteamParser().DownloadString("http://api.steamcardexchange.net/GetBadgePrices.json");
                Storage.WriteContent(values);
                _prices = null;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "");
                return false;
            }
            return true;
        }

        public BadgeStockModel GetStockModel(string id)
        {
            BadgeStockModel b;
            Prices.TryGetValue(id, out b);
            return b;
        }
    }
}
