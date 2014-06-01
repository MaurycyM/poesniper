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


        //"require(["PoE/Item/DeferredItemRenderer"], function(R) { (new R("
        private Regex foo = new Regex(Regex.Escape(@"require([""PoE/Item/DeferredItemRenderer""], function(R) { (new R(") + "(?<items>.+)");
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

            var itemTextParser = new ItemTextParser();

            var itemsString = foo.Match(shopThreadBody).Groups["items"];
            var items = itemsString.ToString().Split(new[] { ",[]]," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in items)
            {
                var itemJson = item.Substring(item.IndexOf("{"));

                var itemDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(itemJson, new JsonItemConverter());
                var result = itemTextParser.ParseItemDictionary(itemDictionary);
            }

           // var foo = shopThreadBody.ToString();


            Console.WriteLine(items);

            return null;

        }
    }

    public class JsonItemConverter : CustomCreationConverter<IDictionary<string, object>>
    {
        public override IDictionary<string, object> Create(Type objectType)
        {
            return new Dictionary<string, object>();
        }

        public override bool CanConvert(Type objectType)
        {
            // in addition to handling IDictionary<string, object>
            // we want to handle the deserialization of dict value
            // which is of type object
            return objectType == typeof(object) || base.CanConvert(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject
                || reader.TokenType == JsonToken.Null)
                return base.ReadJson(reader, objectType, existingValue, serializer);

            // if the next token is not an object
            // then fall back on standard deserializer (strings, numbers etc.)
            return serializer.Deserialize(reader);
        }
    }


    public class ItemTextParser
    {
        public Item ParseItemDictionary(Dictionary<string, object> itemDictionary)
        {
            var itemBases = File.ReadAllText(@"Data\itemBases.dat");
            var fooooo = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(itemBases);

            var type = itemDictionary["typeLine"];

            return null;

        }
    }
}
