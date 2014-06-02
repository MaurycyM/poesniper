﻿using Newtonsoft.Json;
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

        private string[] _accessories = new string[]
        {
            "Amulet",
            "Ring",
            "Belt",
        };

        //private List<string> staticCorruptedImplictProperties = new List<string>();
        //private List<string> staticUniqueItemMagicProperties = new List<string>();

        private MagicPropertiesParser _magicPropertiesParser;
        private SocketsParser _socketsParser;
        private Dictionary<string, string> _itemBaseTypeMapping = new Dictionary<string, string>();
        private string[] _itemPrefixes;
        private string[] _itemSuffixes;

        public ItemTextParser()
        {
            _magicPropertiesParser = new MagicPropertiesParser();
            _socketsParser = new SocketsParser();

            _itemPrefixes = File.ReadAllLines(@"Data\itemPrefixes.dat");
            _itemSuffixes = File.ReadAllLines(@"Data\itemSuffixes.dat");
            var itemBasesString = File.ReadAllText(@"Data\itemBases.dat");
            var itemBases = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(itemBasesString);
            _itemBaseTypeMapping = itemBases.SelectMany(i => i.Value, (o, i) => new { type = o.Key, item = i }).ToDictionary(k => k.item, e => e.type);

            //staticCorruptedImplictProperties = File.ReadAllLines(@"Data/staticCorruptedImplicitProperties.dat").ToList();
            //staticUniqueItemMagicProperties = File.ReadAllLines(@"Data/staticUniqueItemMagicProperties.dat").ToList();
        }

        private List<string> explicitProps = new List<string>();
        private List<string> implicitProps = new List<string>();
        private List<string> corruptedImplicitProps = new List<string>();
        private List<string> uniqueItemProps = new List<string>();


        // try to find item base - first match the name with the dictionary of item bases (for normal, rares and uniques)
        // if that doesn't work, try to trim prefix and suffix (for magic items)
        private bool TryFindItemBaseType(string itemBase, out string itemType)
        {
            itemBase = itemBase.Replace("Superior ", "");
            if (_itemBaseTypeMapping.TryGetValue(itemBase, out itemType))
            {
                return true;
            }

            var magicItemBase = itemBase;
            foreach (var prefix in _itemPrefixes)
            {
                if (magicItemBase.StartsWith(prefix + " "))
                {
                    magicItemBase = magicItemBase.Substring(prefix.Length + 1);
                    break;
                }
            }

            foreach (var suffix in _itemSuffixes)
            {
                if (magicItemBase.EndsWith(" " + suffix))
                {
                    magicItemBase = magicItemBase.Substring(0, magicItemBase.IndexOf(" " + suffix));
                    break;
                }
            }

            if (_itemBaseTypeMapping.TryGetValue((string)magicItemBase, out itemType))
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

            // TEMP
            explicitProps = File.ReadAllLines(@"Data/explicitMagicProperties.dat").ToList();
            implicitProps = File.ReadAllLines(@"Data/implicitMagicProperties.dat").ToList();
            corruptedImplicitProps = File.ReadAllLines(@"Data/corruptedImplicitProperties.dat").ToList();
            uniqueItemProps = File.ReadAllLines(@"Data/uniqeItemMagicProperties.dat").ToList();

            Item result = null;
            string itemType;
            if (TryFindItemBaseType((string)itemJson.typeLine, out itemType))
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
                result.Base = itemJson.typeLine;
                result.IsVerified = itemJson.verified;
                result.IsIdentified = itemJson.identified;
                result.IsCorrupted = itemJson.corrupted;
                result.Requirements = this.ParseRequirements(itemJson.requirements);
                result.ImplicitProperties = new List<MagicProperty>();

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
                    if (itemJson.corrupted)
                    {
                        var implicitProperties = _magicPropertiesParser.ParseMagicProperties(itemJson.implicitMods, corruptedImplicitProps, true, implicitProps);
                        result.ImplicitProperties.AddRange(implicitProperties);
                    }
                    else
                    {
                        var implicitProperties = _magicPropertiesParser.ParseMagicProperties(itemJson.implicitMods, implicitProps, false, null);
                        result.ImplicitProperties.AddRange(implicitProperties);
                    }
                }

                result.ExplicitProperties = new List<MagicProperty>();
                if (result.Type != ItemType.Currency && result.Type != ItemType.Gem && result.Type != ItemType.Map && result.Type != ItemType.VaalFragment)
                {
                    if (itemJson.explicitMods != null)
                    {
                        if (itemJson.flavourText != null)
                        {
                            var explicitProperties = _magicPropertiesParser.ParseMagicProperties(itemJson.explicitMods, uniqueItemProps, true, explicitProps);
                            result.ExplicitProperties.AddRange(explicitProperties);
                        }
                        else
                        {
                            var explicitProperties = _magicPropertiesParser.ParseMagicProperties(itemJson.explicitMods, explicitProps, false, null);
                            result.ExplicitProperties.AddRange(explicitProperties);
                        }
                    }
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

                }





            }
            else
            {
                // TODO: logging etc

                Console.WriteLine("UNKNOWN ITEM!!!");
                Console.WriteLine("Item type: " + itemType + " TypeLine: " + itemJson.typeLine + " Name: " + itemJson.name);
                Console.WriteLine();
                return null;
            }

            // temp
            //File.WriteAllLines(@"Data/explicitMagicProperties.dat", explicitProps);
            //File.WriteAllLines(@"Data/implicitMagicProperties.dat", implicitProps);
            //File.WriteAllLines(@"Data/corruptedImplicitProperties.dat", corruptedImplicitProps);
            //File.WriteAllLines(@"Data/uniqeItemMagicProperties.dat", uniqueItemProps);

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

            var minPhysicalDamageAddedProperty = weapon.ExplicitProperties.Where(p => p.Name == "Adds X Min Physical Damage").SingleOrDefault();
            var maxPhysicalDamageAddedProperty = weapon.ExplicitProperties.Where(p => p.Name == "Adds X Max Physical Damage").SingleOrDefault();
            var increasedPhysicalDamageProperty = weapon.ExplicitProperties.Where(p => p.Name == "X% increased Physical Damage").SingleOrDefault();
            var minPhysicalDamageAdded = minPhysicalDamageAddedProperty != null ? minPhysicalDamageAddedProperty.Value : 0;
            var maxPhysicalDamageAdded = maxPhysicalDamageAddedProperty != null ? maxPhysicalDamageAddedProperty.Value : 0;
            var increasedPhysicalDamage = increasedPhysicalDamageProperty != null ? increasedPhysicalDamageProperty.Value : 0;

            var physicalDamageMultiplier = 1 + ((decimal)weapon.Quality / 100) + ((decimal)increasedPhysicalDamage / 100);
            var physicalDpsBeforeMultiplier = weapon.PhysicalDps / physicalDamageMultiplier;
            weapon.PhysicalDpsWithMaxQuality = physicalDpsBeforeMultiplier * (1 + 0.2M + ((decimal)increasedPhysicalDamage / 100));

            // divide by 1 + quality + added physical damage

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