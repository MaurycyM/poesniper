using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoeSniper.PoeSniper.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PoeSniper
{
    public class ItemFetcher
    {
        private Regex itemStringRegex = new Regex(Regex.Escape(@"require([""PoE/Item/DeferredItemRenderer""], function(R) { (new R(") + "(?<items>.+)");
        public async Task<string[]> FetchItems(string threadUrl)
        {
            var shopThreadBody = string.Empty;
            using (var client = new HttpClient())
            {
                var httpResponse = await client.GetAsync(threadUrl);

                Stream stream = null;
                stream = await httpResponse.Content.ReadAsStreamAsync();

                string value = string.Empty;
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    shopThreadBody = reader.ReadToEnd();
                }
            }

            var shopThread = new ShopThread
            {
                Url = threadUrl,
                Items = new List<Item>(),
            };

            var priceParser = new ShopThreadPriceParser();
            priceParser.ParseShopThreadPrices(shopThreadBody);

            var itemTextParser = new ItemTextParser();
            var itemsString = itemStringRegex.Match(shopThreadBody).Groups["items"];
            var items = itemsString.ToString().Replace(",[]]])).run(); });", "").Split(new[] { ",[]]," }, StringSplitOptions.RemoveEmptyEntries);
            //var itemNumberRegex = new Regex("^" + Regex.Escape("[") + @"{1,2}(?<itemNumber>\d+),");
            var itemNumberRegex = new Regex(@"\d+");

            foreach (var item in items)
            {
                var itemNumber = int.Parse(itemNumberRegex.Match(item).ToString());
                var itemJson = item.Substring(item.IndexOf("{"));
                var itemObject = JsonConvert.DeserializeObject<ItemJsonObject>(itemJson);
                var result = itemTextParser.ParseItem(itemObject);
            }

            return null;
        }
    }

    public class RequirementJsonObject
    {
        public string name { get; set; }
        public List<List<object>> values { get; set; }
        public int displayMode { get; set; }
    }

    public class SocketJsonObject
    {
        public int group { get; set; }
        public string attr { get; set; }
    }

    public class PropertyJsonObject
    {
        public string name { get; set; }
        public List<object> values { get; set; }
        public int displayMode { get; set; }
    }

    public class ItemJsonObject
    {
        public bool verified { get; set; }
        public int w { get; set; }
        public int h { get; set; }
        public string icon { get; set; }
        public bool support { get; set; }
        public string league { get; set; }
        public List<SocketJsonObject> sockets { get; set; }
        public string name { get; set; }
        public string typeLine { get; set; }
        public bool identified { get; set; }
        public bool corrupted { get; set; }
        public List<PropertyJsonObject> properties { get; set; }
        public List<RequirementJsonObject> requirements { get; set; }
        public List<string> implicitMods { get; set; }
        public List<string> explicitMods { get; set; }
        public List<string> flavourText { get; set; }
        public int frameType { get; set; }
        public List<object> socketedItems { get; set; }
    }
}
