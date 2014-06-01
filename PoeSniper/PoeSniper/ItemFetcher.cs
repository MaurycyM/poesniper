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

        private int itemCount = 0;


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
            var items = itemsString.ToString().Replace(",[]]])).run(); });", "").Split(new[] { ",[]]," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in items)
            {
                var itemJson = item.Substring(item.IndexOf("{"));

                //var itemDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(itemJson, new JsonItemConverter());

                var itemObject = JsonConvert.DeserializeObject<ItemJsonObject>(itemJson);

                var result = itemTextParser.ParseItemJsonObject(itemObject);

                itemCount++;
                //var result = itemTextParser.ParseItemDictionary(itemDictionary);
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

    public class ItemJsonObject
    {
        public bool verified { get; set; }
        public int w { get; set; }
        public int h { get; set; }
        public string icon { get; set; }
        public bool support { get; set; }
        public string league { get; set; }
        public List<object> sockets { get; set; }
        public string name { get; set; }
        public string typeLine { get; set; }
        public bool identified { get; set; }
        public bool corrupted { get; set; }
        public List<RequirementJsonObject> requirements { get; set; }
        public List<string> implicitMods { get; set; }
        public List<string> explicitMods { get; set; }
        public List<string> flavourText { get; set; }
        public int frameType { get; set; }
        public List<object> socketedItems { get; set; }
    }

    public class ItemTextParser
    {
        private string[] _weapons = new string[] 
        { 
            "One Hand Axe", 
            "One Hand Sword",
            "One Hand Mace",
            "Fishing Rod",
            "Sceptre",
            "Two Hand Axe",
            "Two Hand Sword",
            "Bow",
            "Claw",
            "Two Hand Mace",
            "Dagger",
            "Wand",
            "Staff",
        };

        private string[] _armors = new string[] 
        {
            "Helmet",
            "Body Armour",
            "Gloves",
            "Shield",
            "Boots",
        };

        private List<string> explicitProps = new List<string>();
        private List<string> implicitProps = new List<string>();

        public Item ParseItemJsonObject(ItemJsonObject itemJson)
        {
            // TEMP
            explicitProps = File.ReadAllLines(@"Data/explicitMagicProperties.dat").ToList();
            implicitProps = File.ReadAllLines(@"Data/implicitMagicProperties.dat").ToList();
            
            Item result = null;
            var itemBasesString = File.ReadAllText(@"Data\itemBases.dat");
            var itemBases = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(itemBasesString);

            var itemBasesMapping = itemBases.SelectMany(i => i.Value, (o, i) => new { type = o.Key, item = i }).ToDictionary(k => k.item, e => e.type);
            var itemBase = itemJson.typeLine;
            
            string itemType;
            if (itemBasesMapping.TryGetValue((string)itemBase, out itemType))
            {
                if (_weapons.Contains(itemType))
                {
                    result = new Weapon();
                }
                else if (_armors.Contains(itemType))
                {
                    result = new Armor();
                }
                else
                {
                    result = new Item();
                }

                result.Name = itemJson.name;
                result.Type = (ItemType)Enum.Parse(typeof(ItemType), (string)itemType.Replace(" ", "")); 
                result.Base = itemJson.typeLine;
                result.IsVerified = itemJson.verified;
                result.IsIdentified = itemJson.identified;
                result.IsCorrupted = itemJson.corrupted;
                result.Requirements = this.ParseRequirements(itemJson.requirements);

                if (itemJson.implicitMods != null)
                {
                    result.ImplicitProperty = ParseImplicitProperty(itemJson.implicitMods.Single());
                    


                }
                result.ExplicitProperties = new List<MagicProperty>();

                if (result.Type != ItemType.Currency && result.Type != ItemType.Gem && result.Type != ItemType.Map && result.Type != ItemType.VaalFragment)
                {
                    // temp, dont process uniques due to funky magic props
                    if (itemJson.flavourText == null)
                    {
                        if (itemJson.explicitMods != null)
                        {
                            foreach (var explicitMod in itemJson.explicitMods)
                            {
                                result.ExplicitProperties.AddRange(ParseMagicProperty(explicitMod, isUniqueItem: itemJson.flavourText != null));
                            }
                        }
                    }
                }
                
                Console.WriteLine(itemType);

            }
            else
            {
                // TODO: logging etc
                return null;
            }

            // temp
            File.WriteAllLines(@"Data/explicitMagicProperties.dat", explicitProps);
            File.WriteAllLines(@"Data/implicitMagicProperties.dat", implicitProps);


            return null;

        }

        private class ReqiuirementJsonObject
        {
            public string name { get; set; }
            public List<List<object>> values { get; set; }
            public int displayMode { get; set; }
        }

        private Dictionary<string, Action<Requirements, int>> requirementSetters = new Dictionary<string, Action<Requirements, int>>
        {
            { "Level", (r, i) => r.Level = i },
            { "Str", (r, i) => r.Strength = i },
            { "Dex", (r, i) => r.Dexterity = i },
            { "Int", (r, i) => r.Inteligence = i },
            { "Strength", (r, i) => r.Strength = i },
            { "Dexterity", (r, i) => r.Dexterity = i },
            { "Intelligence", (r, i) => r.Inteligence = i },
        };

        public Requirements ParseRequirements(List<RequirementJsonObject> requirementsJson)
        {
            var result = new Requirements();
            if (requirementsJson != null)
            {
                foreach (var requirement in requirementsJson)
                {
                    requirementSetters[requirement.name.Replace(" (gem)", "")](result, int.Parse(((string)requirement.values[0][0]).Replace("(gem)", "")));
                }
            }

            return result;
        }

        //Adds 11-21 Fire Damage

        private Regex addsDamageRegex = new Regex(@"Adds (?<minDamage>\d+)-(?<maxDamage>\d+) (?<damageType>[a-zA-Z ]+)");
        private Regex reflectsDamageRegex = new Regex(@"Reflects (?<damage>\d+)");

        // returns array of properties, because for Adds X-Y physical/elemental/chaos damage we generate 2 properties
        public MagicProperty[] ParseMagicProperty(string propertyString, bool isUniqueItem)
        {
            var addsDamageMatch = addsDamageRegex.Match(propertyString);
            if (addsDamageMatch.Success)
            {
                var minDamage = addsDamageMatch.Groups["minDamage"].Value;
                var maxDamage = addsDamageMatch.Groups["maxDamage"].Value;
                var damageType = addsDamageMatch.Groups["damageType"].Value;

                var addsMinDamageProperty = new MagicProperty { Value = int.Parse(minDamage), Name = "Min " + damageType };
                var addsMaxDamageProperty = new MagicProperty { Value = int.Parse(maxDamage), Name = "Max " + damageType };


                if (!explicitProps.Contains(addsMinDamageProperty.Name) && !isUniqueItem)
                {
                    explicitProps.Add(addsMinDamageProperty.Name);
                }

                if (!explicitProps.Contains(addsMaxDamageProperty.Name) && !isUniqueItem)
                {
                    explicitProps.Add(addsMaxDamageProperty.Name);
                }



                return new[] { addsMinDamageProperty, addsMaxDamageProperty };
            }

            var reflectDamageMatch = reflectsDamageRegex.Match(propertyString);
            if (reflectDamageMatch.Success)
            {
                var damage = reflectDamageMatch.Groups["damage"].Value;
                var reflectDamageProperty = new MagicProperty { Value = int.Parse(damage), Name = propertyString.Replace(damage + " ", "") };

                if (!explicitProps.Contains(reflectDamageProperty.Name) && !isUniqueItem)
                {
                    explicitProps.Add(reflectDamageProperty.Name);
                }


                return new[] { reflectDamageProperty };
            }

            //Reflects 12 Physical Damage to Melee Attackers

            var separatorIndex = propertyString.IndexOf(" ");

            if (separatorIndex >= 0)
            {
                var value = propertyString.Substring(0, separatorIndex);
                var name = propertyString.Substring(separatorIndex + 1);
                if (value.Contains("%"))
                {
                    name = "% " + name;
                    value = value.Replace("%", "");
                }

                var genericProperty = new MagicProperty { Value = int.Parse(value), Name = name };

                if (!explicitProps.Contains(genericProperty.Name) && !isUniqueItem)
                {
                    explicitProps.Add(genericProperty.Name);
                }

                return new[] { genericProperty };
            }
            else
            {
                var valuelessProperty = new MagicProperty { Name = propertyString };

                if (!explicitProps.Contains(valuelessProperty.Name) && !isUniqueItem)
                {
                    explicitProps.Add(valuelessProperty.Name);
                }

                return new[] { valuelessProperty };
            }

        }

        public MagicProperty ParseImplicitProperty(string propertyString)
        {
            var separatorIndex = propertyString.IndexOf(" ");

            if (propertyString == "Has 1 Socket")
            {
                var hasSocketProperty = new MagicProperty { Name = propertyString };
                if (!implicitProps.Contains(hasSocketProperty.Name))
                {
                    implicitProps.Add(hasSocketProperty.Name);
                }

                return hasSocketProperty;
            }

            // TODO: duplicated!!!!
            var reflectDamageMatch = reflectsDamageRegex.Match(propertyString);
            if (reflectDamageMatch.Success)
            {
                var damage = reflectDamageMatch.Groups["damage"].Value;
                var reflectDamageProperty = new MagicProperty { Value = int.Parse(damage), Name = propertyString.Replace(damage + " ", "") };

                if (!implicitProps.Contains(reflectDamageProperty.Name))
                {
                    implicitProps.Add(reflectDamageProperty.Name);
                }


                return reflectDamageProperty;
            }




            if (separatorIndex >= 0)
            {
                var value = propertyString.Substring(0, separatorIndex);
                var name = propertyString.Substring(separatorIndex + 1);
                if (value.Contains("%"))
                {
                    name = "% " + name;
                    value = value.Replace("%", "");
                }

                var genericProperty = new MagicProperty { Value = int.Parse(value), Name = name };


                if (!implicitProps.Contains(genericProperty.Name))
                {
                    implicitProps.Add(genericProperty.Name);
                }

                return genericProperty;
            }
            else
            {
                var valuelessProperty = new MagicProperty { Name = propertyString };
                if (!implicitProps.Contains(valuelessProperty.Name))
                {
                    implicitProps.Add(valuelessProperty.Name);
                }

                return valuelessProperty;
            }
        }
    }
}







/*
//{"verified":true,
"w":1,"h":1,
"icon":"http:\/\/webcdn.pathofexile.com\/image\/Art\/2DItems\/Amulets\/Amulet1Unique.png?scale=1&w=1&h=1&v=1ad77d738901b6e1f5ab921f2cfdad5b3",

"support":true,"league":"Invasion","sockets":[],

"name":"Sidhebreath",

"typeLine":"Paua Amulet",

"identified":true,"corrupted":true,


"implicitMods":["27% increased Mana Regeneration Rate"],


"explicitMods":["+25% to Cold Resistance","1% of Physical Attack Damage Leeched as Mana","Minions have 12% increased maximum Life","Minions have 13% increased Movement Speed","Minions deal 14% increased Damage"],


"flavourText":["The breath of life is yours to give."],"frameType":3,"socketedItems":[]}


*/