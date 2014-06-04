using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeSniper.PoeSniper.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoeSniper
{
    public class ItemParser
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

        private string[] _accessories = new string[]
        {
            "Amulet",
            "Ring",
            "Belt",
        };

        private class WeaponDamageJsonObject
        {
            public int min { get; set; }
            public int max { get; set; }
        }

        private MagicPropertiesParser _magicPropertiesParser;
        private SocketsParser _socketsParser;
        private Dictionary<string, string> _itemBaseTypeMapping = new Dictionary<string, string>();
        private Dictionary<string, WeaponDamageJsonObject> _weaponBaseDamage;
        private string[] _itemPrefixes;
        private string[] _itemSuffixes;
        private const string _itemPrefixesFileLocation = @"Data\ItemPrefixes.dat";
        private const string _itemSuffixesFileLocation = @"Data\ItemSuffixes.dat";
        private const string _itemBasesFileLocation = @"Data\ItemBases.dat";
        private const string _weaponBaseDamageFileLocation = @"Data\WeaponBaseDamage.dat";

        public ItemParser()
        {
            _magicPropertiesParser = new MagicPropertiesParser();
            _socketsParser = new SocketsParser();

            _itemPrefixes = File.ReadAllLines(_itemPrefixesFileLocation);
            _itemSuffixes = File.ReadAllLines(_itemSuffixesFileLocation);
            
            var itemBasesString = File.ReadAllText(_itemBasesFileLocation);
            var itemBases = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(itemBasesString);
            _itemBaseTypeMapping = itemBases.SelectMany(i => i.Value, (o, i) => new { type = o.Key, item = i }).ToDictionary(k => k.item, e => e.type);
            
            var weaponBaseDamage = File.ReadAllText(_weaponBaseDamageFileLocation);
            _weaponBaseDamage = JsonConvert.DeserializeObject<Dictionary<string, WeaponDamageJsonObject>>(weaponBaseDamage);
        }

        // try to find item base - first match the name with the dictionary of item bases (for normal, rares and uniques)
        // if that doesn't work, try to trim prefix and suffix (for magic items)
        private bool TryFindItemBaseType(string typeLine, out string itemType, out string itemBase)
        {
            itemBase = typeLine.Replace("Superior ", "");
            if (_itemBaseTypeMapping.TryGetValue(itemBase, out itemType))
            {
                return true;
            }

            foreach (var prefix in _itemPrefixes)
            {
                if (itemBase.StartsWith(prefix + " "))
                {
                    itemBase = itemBase.Substring(prefix.Length + 1);
                    break;
                }
            }

            foreach (var suffix in _itemSuffixes)
            {
                if (itemBase.EndsWith(" " + suffix))
                {
                    itemBase = itemBase.Substring(0, itemBase.IndexOf(" " + suffix));
                    break;
                }
            }

            if (_itemBaseTypeMapping.TryGetValue((string)itemBase, out itemType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Item ParseItem(ItemJsonObject itemJson)
        {
            if (!itemJson.verified)
            {
                return null;
            }

            Item result = null;
            string itemType;
            string itemBase;
            if (TryFindItemBaseType((string)itemJson.typeLine, out itemType, out itemBase))
            {
                if (_weapons.Contains(itemType))
                {
                    result = new Weapon();
                }
                else if (_armors.Contains(itemType))
                {
                    result = new Armor();
                }
                else if (_accessories.Contains(itemType) || itemType == "Quiver")
                {
                    result = new Item();
                }
                else
                {
                    // TODO: for now only logging items (no gems, flasks, maps, vaal fragments)
                    //Console.WriteLine("Not logging. Item type: " + itemType + ". Item name: " + itemJson.typeLine);
                    return null;
                }

                result.Name = itemJson.name;
                result.Type = (ItemType)Enum.Parse(typeof(ItemType), (string)itemType.Replace(" ", ""));

                // TODO: is this needed?
                if (result.Type == ItemType.Currency || result.Type == ItemType.Gem || result.Type == ItemType.Map || result.Type == ItemType.VaalFragment)
                {
                    return null;
                }

                result.Base = itemBase;
                result.IsVerified = itemJson.verified;
                result.IsIdentified = itemJson.identified;
                result.IsCorrupted = itemJson.corrupted;
                result.Requirements = this.ParseRequirements(itemJson.requirements);
                result.MagicProperties = new List<MagicProperty>();

                if (itemJson.properties != null)
                {
                    var qualityArray = itemJson.properties.Where(p => p.name == "Quality").Select(p => p.values).SingleOrDefault();
                    if (qualityArray != null)
                    {
                        var qualityString = (string)((JArray)qualityArray[0])[0];
                        result.Quality = int.Parse(qualityString.Replace("+", "").Replace("%", ""));
                    }
                }

                if (itemJson.implicitMods != null)
                {
                    var implicitProperties = _magicPropertiesParser.ParseMagicProperties(itemJson.implicitMods, true, isUnique: false /*for implicit props it doesnt matter */);
                    result.MagicProperties.AddRange(implicitProperties);
                }

                if (itemJson.explicitMods != null)
                {
                    var explicitProperties = _magicPropertiesParser.ParseMagicProperties(itemJson.explicitMods, false, itemJson.flavourText != null);
                    result.MagicProperties.AddRange(explicitProperties);
                }

                if (itemJson.sockets != null && itemJson.sockets.Count > 0)
                {
                    var sockets = _socketsParser.ParseSockets(itemJson.sockets);
                    result.Sockets = sockets;
                }

                var weapon = result as Weapon;
                var armor = result as Armor;
                if (weapon != null)
                {
                    ParseWeaponSpecificProperties(weapon, itemJson.properties);
                }
                else if (armor != null)
                {
                    ParseArmorSpecificProperties(armor, itemJson.properties);
                }
            }
            else
            {
                Console.WriteLine("UNKNOWN ITEM!!!");
                Console.WriteLine("Item type: " + itemType + " TypeLine: " + itemJson.typeLine + " Name: " + itemJson.name);
                Console.WriteLine();
                return null;
            }

            return result;
        }

        private void ParseWeaponSpecificProperties(Weapon weapon, List<PropertyJsonObject> jsonProperties)
        {
            var physicalDamage = jsonProperties.Where(p => p.name == "Physical Damage").Select(p => p.values).SingleOrDefault();
            var elementalDamage = jsonProperties.Where(p => p.name == "Elemental Damage").Select(p => p.values).SingleOrDefault();
            var criticalStrikeChance = jsonProperties.Where(p => p.name == "Critical Strike Chance").Select(p => p.values).Single();
            var attacksPerSecond = jsonProperties.Where(p => p.name == "Attacks per Second").Select(p => p.values).Single();

            if (physicalDamage != null)
            {
                var physicalDamageValuesString = ((string)((JArray)physicalDamage[0])[0]);
                var physicalDamageValues = physicalDamageValuesString.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                weapon.MinPhysicalDamage = int.Parse(physicalDamageValues[0]);
                weapon.MaxPhysicalDamage = int.Parse(physicalDamageValues[1]);
            }

            if (elementalDamage != null)
            {
                foreach (JArray elementalDamageValueArray in (List<object>)elementalDamage)
                {
                    var elementalDamageValues = ((string)elementalDamageValueArray[0]).Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                    var minElementalDamage = int.Parse(elementalDamageValues[0]);
                    var maxElementalDamage = int.Parse(elementalDamageValues[1]);
                    var damageType = (int)elementalDamageValueArray[1];
                    if (damageType == 4)
                    {
                        weapon.MinFireDamage = minElementalDamage;
                        weapon.MaxFireDamage = maxElementalDamage;
                    }
                    else if (damageType == 5)
                    {
                        weapon.MinColdDamage = minElementalDamage;
                        weapon.MaxColdDamage = maxElementalDamage;
                    }
                    else if (damageType == 6)
                    {
                        weapon.MinLightningDamage = minElementalDamage;
                        weapon.MaxLightningDamage = maxElementalDamage;
                    }
                    else
                    {
                        throw new Exception("invalid damage type: " + damageType);
                    }
                }
            }

            var criticalStrikeChanceValueString = ((string)((JArray)criticalStrikeChance[0])[0]);
            weapon.CriticalStrikeChance = decimal.Parse(criticalStrikeChanceValueString.Replace("%", ""), NumberStyles.AllowDecimalPoint);

            var attacksPerSecondValueString = ((string)((JArray)attacksPerSecond[0])[0]);
            weapon.AttacksPerSecond = decimal.Parse(attacksPerSecondValueString, NumberStyles.AllowDecimalPoint);

            CalculateWeaponDps(weapon);
        }

        private void CalculateWeaponDps(Weapon weapon)
        {
            weapon.PhysicalDps = (weapon.MinPhysicalDamage + weapon.MaxPhysicalDamage) / 2 * weapon.AttacksPerSecond;
            weapon.ElementalDps = (weapon.MinFireDamage + weapon.MaxFireDamage
                + weapon.MinColdDamage + weapon.MaxColdDamage
                + weapon.MinLightningDamage + weapon.MaxLightningDamage
                + weapon.MinChaosDamage + weapon.MaxChaosDamage) / 2 * weapon.AttacksPerSecond;

            weapon.Dps = weapon.PhysicalDps + weapon.ElementalDps;

            var minPhysicalDamageAddedProperty = weapon.MagicProperties.Where(p => p.Name == "Adds X Min Physical Damage").SingleOrDefault();
            var maxPhysicalDamageAddedProperty = weapon.MagicProperties.Where(p => p.Name == "Adds X Max Physical Damage").SingleOrDefault();
            var increasedPhysicalDamageProperty = weapon.MagicProperties.Where(p => p.Name == "X% increased Physical Damage").SingleOrDefault();
            var minPhysicalDamageAdded = minPhysicalDamageAddedProperty != null ? minPhysicalDamageAddedProperty.Value : 0;
            var maxPhysicalDamageAdded = maxPhysicalDamageAddedProperty != null ? maxPhysicalDamageAddedProperty.Value : 0;
            var increasedPhysicalDamage = increasedPhysicalDamageProperty != null ? increasedPhysicalDamageProperty.Value : 0;

            WeaponDamageJsonObject baseDamage;
            if (_weaponBaseDamage.TryGetValue(weapon.Base, out baseDamage))
            {
                var minDamageWithAddedPhysical = baseDamage.min + minPhysicalDamageAdded;
                var maxDamageWithAddedPhysical = baseDamage.max + maxPhysicalDamageAdded;
                var minDamageWithMultiplier = minDamageWithAddedPhysical * (1 + 0.2M + ((decimal)increasedPhysicalDamage / 100));
                var maxDamageWithMultiplier = maxDamageWithAddedPhysical * (1 + 0.2M + ((decimal)increasedPhysicalDamage / 100));
                weapon.PhysicalDpsWithMaxQuality = (decimal)(minDamageWithMultiplier + maxDamageWithMultiplier) * weapon.AttacksPerSecond / 2;
                weapon.DpsWithMaxQuality = weapon.PhysicalDpsWithMaxQuality + weapon.ElementalDps;
            }
            else
            {
                //TODO: Log
                Console.WriteLine("Couldn't calculate DPS - unknown item type: " + weapon.Base);
            }
        }

        private void ParseArmorSpecificProperties(Armor armor, List<PropertyJsonObject> jsonProperties)
        {
            if (jsonProperties != null)
            {
                var armour = jsonProperties.Where(p => p.name == "Armour").Select(p => p.values).SingleOrDefault();
                var evasionRating = jsonProperties.Where(p => p.name == "Evasion Rating").Select(p => p.values).SingleOrDefault();
                var energyShield = jsonProperties.Where(p => p.name == "Energy Shield").Select(p => p.values).SingleOrDefault();

                if (armour != null)
                {
                    var armourString = ((string)((JArray)armour[0])[0]);
                    armor.Armour = int.Parse(armourString);
                    armor.ArmourWithMaxQuality = armor.Quality != 20 ? (int)((armor.Armour / (1M + (decimal)armor.Quality / 100M)) * 1.2M) : armor.Armour;
                }

                if (evasionRating != null)
                {
                    var evasionRatingString = ((string)((JArray)evasionRating[0])[0]);
                    armor.EvasionRating = int.Parse(evasionRatingString);
                    armor.EvasionRatingWithMaxQuality = armor.Quality != 20 ? (int)((armor.EvasionRating / (1M + (decimal)armor.Quality / 100M)) * 1.2M) : armor.EvasionRating;
                }

                if (energyShield != null)
                {
                    var energyShieldString = ((string)((JArray)energyShield[0])[0]);
                    armor.EnergyShield = int.Parse(energyShieldString);
                    armor.EnergyShieldWithMaxQuality = armor.Quality != 20 ? (int)((armor.EnergyShield / (1M + (decimal)armor.Quality / 100M)) * 1.2M) : armor.EnergyShield;
                }
            }
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

        //private Regex damageRangeRegex = new Regex(@"[^0-9" + Regex.Escape("-") + @"]*(?<damageRange>\d+-\d+) .*");
        //private Regex genericPropertyRegex = new Regex(@"[^0-9" + Regex.Escape("-") + @"]*(?<value>" + Regex.Escape("+") + @"?-?\d+)");

        //// returns array of properties, because for Adds X-Y physical/elemental/chaos damage we generate 2 properties
        //public List<MagicProperty> ParseMagicProperties(
        //    List<string> propertyStrings,
        //    List<string> propertyStringList,
        //    bool isCorruptedOrUnique,
        //    List<string> nonCorruptedNonUniquePropertyStringList)
        //{
        //    List<MagicProperty> result = new List<MagicProperty>();
        //    foreach (var propertyString in propertyStrings)
        //    {
        //        if (isCorruptedOrUnique)
        //        {
        //            if (staticCorruptedImplictProperties.Contains(propertyString) ||
        //                staticUniqueItemMagicProperties.Contains(propertyString))
        //            {
        //                var staticCorruptedProperty = new MagicProperty { Name = propertyString };
        //                result.Add(staticCorruptedProperty);
        //                continue;
        //            }
        //        }

        //        // only static magic property for regular items, not worth doing a file just for that one
        //        // if more properties like that appear - consider adding file for them
        //        if (propertyString == "Has 1 Socket")
        //        {
        //            var hasSocketProperty = new MagicProperty { Name = propertyString };
        //            AddPropertyToPropertyStringList(hasSocketProperty.Name, propertyStringList, isCorruptedOrUnique, nonCorruptedNonUniquePropertyStringList);

        //            result.Add(hasSocketProperty);
        //            continue;
        //        }

        //        var addsDamageMatch = damageRangeRegex.Match(propertyString);
        //        if (addsDamageMatch.Success)
        //        {
        //            var damageRange = addsDamageMatch.Groups["damageRange"].Value;
        //            var minDamage = damageRange.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[0];
        //            var maxDamage = damageRange.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[1];

        //            var minDamageProperty = new MagicProperty { Value = int.Parse(minDamage), Name = propertyString.Replace(damageRange, "X Min") };
        //            var maxDamageProperty = new MagicProperty { Value = int.Parse(maxDamage), Name = propertyString.Replace(damageRange, "X Max") };

        //            AddPropertyToPropertyStringList(minDamageProperty.Name, propertyStringList, isCorruptedOrUnique, nonCorruptedNonUniquePropertyStringList);
        //            AddPropertyToPropertyStringList(maxDamageProperty.Name, propertyStringList, isCorruptedOrUnique, nonCorruptedNonUniquePropertyStringList);

        //            result.Add(minDamageProperty);
        //            result.Add(maxDamageProperty);
        //            continue;
        //        }

        //        var match = genericPropertyRegex.Match(propertyString);
        //        if (match.Success)
        //        {
        //            var value = match.Groups["value"].Value;
        //            var genericProperty = new MagicProperty { Value = int.Parse(value), Name = propertyString.Replace(value, "X") };
        //            AddPropertyToPropertyStringList(genericProperty.Name, propertyStringList, isCorruptedOrUnique, nonCorruptedNonUniquePropertyStringList);

        //            result.Add(genericProperty);
        //            continue;
        //        }

        //        Console.WriteLine("UNRECOGNIZED MAGIC PROPERTY:");
        //        Console.WriteLine(propertyString);
        //        Console.WriteLine();
        //        Console.WriteLine();
        //    }

        //    return result;
        //}

        //// used to build property name dictionary, can be commented out once the dictionary is built
        //private void AddPropertyToPropertyStringList(
        //    string propertyName,
        //    List<string> propertyStringList, 
        //    bool isCorruptedOrUnique, 
        //    List<string> nonCorruptedNonUniquePropertyStringList)
        //{
        //    if (isCorruptedOrUnique)
        //    {
        //        if (!nonCorruptedNonUniquePropertyStringList.Contains(propertyName))
        //        {
        //            if (!propertyStringList.Contains(propertyName))
        //            {
        //                propertyStringList.Add(propertyName);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (!propertyStringList.Contains(propertyName))
        //        {
        //            propertyStringList.Add(propertyName);
        //        }
        //    }
        //}
    }
}
