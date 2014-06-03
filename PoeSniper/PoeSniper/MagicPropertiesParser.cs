using PoeSniper.PoeSniper.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PoeSniper
{
    public class MagicPropertiesParser
    {
        private Regex damageRangeRegex = new Regex(@"[^0-9" + Regex.Escape("-") + @"]*(?<damageRange>\d+-\d+) .*");
        private Regex genericPropertyRegex = new Regex(@"[^0-9" + Regex.Escape("-") + @"]*(?<value>" + Regex.Escape("+") + @"?-?\d+)");

        private List<string> staticCorruptedImplictProperties = new List<string>();
        private List<string> staticUniqueItemMagicProperties = new List<string>();

        public MagicPropertiesParser()
        {
            staticCorruptedImplictProperties = File.ReadAllLines(@"Data/staticCorruptedImplicitProperties.dat").ToList();
            staticUniqueItemMagicProperties = File.ReadAllLines(@"Data/staticUniqueItemMagicProperties.dat").ToList();
        }

        // returns array of properties, because for Adds X-Y physical/elemental/chaos damage we generate 2 properties
        public List<MagicProperty> ParseMagicProperties(
            List<string> propertyStrings,
            List<string> propertyStringList,
            bool isCorruptedOrUnique,
            bool isImplicit,
            List<string> nonCorruptedNonUniquePropertyStringList)
        {
            List<MagicProperty> result = new List<MagicProperty>();
            foreach (var propertyString in propertyStrings)
            {
                if (isCorruptedOrUnique)
                {
                    if (staticCorruptedImplictProperties.Contains(propertyString) ||
                        staticUniqueItemMagicProperties.Contains(propertyString))
                    {
                        var staticCorruptedOrUniqueProperty = new MagicProperty { Name = propertyString, IsImplicit = isImplicit };
                        result.Add(staticCorruptedOrUniqueProperty);
                        continue;
                    }
                }

                // only static magic property for regular items, not worth doing a file just for that one
                // if more properties like that appear - consider adding file for them
                if (propertyString == "Has 1 Socket")
                {
                    Debug.Assert(isImplicit, "Expecting corrupted mod to be implicit!");
                    var hasSocketProperty = new MagicProperty { Name = propertyString, IsImplicit = isImplicit };
                    AddPropertyToPropertyStringList(hasSocketProperty.Name, propertyStringList, isCorruptedOrUnique, nonCorruptedNonUniquePropertyStringList);

                    result.Add(hasSocketProperty);
                    continue;
                }

                var addsDamageMatch = damageRangeRegex.Match(propertyString);
                if (addsDamageMatch.Success)
                {
                    var damageRange = addsDamageMatch.Groups["damageRange"].Value;
                    var minDamage = damageRange.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    var maxDamage = damageRange.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[1];

                    var minDamageProperty = new MagicProperty { Value = int.Parse(minDamage), Name = propertyString.Replace(damageRange, "X Min") };
                    var maxDamageProperty = new MagicProperty { Value = int.Parse(maxDamage), Name = propertyString.Replace(damageRange, "X Max") };

                    AddPropertyToPropertyStringList(minDamageProperty.Name, propertyStringList, isCorruptedOrUnique, nonCorruptedNonUniquePropertyStringList);
                    AddPropertyToPropertyStringList(maxDamageProperty.Name, propertyStringList, isCorruptedOrUnique, nonCorruptedNonUniquePropertyStringList);

                    result.Add(minDamageProperty);
                    result.Add(maxDamageProperty);
                    continue;
                }

                var match = genericPropertyRegex.Match(propertyString);
                if (match.Success)
                {
                    var value = match.Groups["value"].Value;
                    var genericProperty = new MagicProperty { Value = int.Parse(value), Name = propertyString.Replace(value, "X"), IsImplicit = isImplicit };
                    AddPropertyToPropertyStringList(genericProperty.Name, propertyStringList, isCorruptedOrUnique, nonCorruptedNonUniquePropertyStringList);

                    result.Add(genericProperty);
                    continue;
                }

                Console.WriteLine("UNRECOGNIZED MAGIC PROPERTY:");
                Console.WriteLine(propertyString);
                Console.WriteLine();
                Console.WriteLine();
            }

            return result;
        }

        // used to build property name dictionary, can be commented out once the dictionary is built
        private void AddPropertyToPropertyStringList(
            string propertyName,
            List<string> propertyStringList,
            bool isCorruptedOrUnique,
            List<string> nonCorruptedNonUniquePropertyStringList)
        {
            if (isCorruptedOrUnique)
            {
                if (!nonCorruptedNonUniquePropertyStringList.Contains(propertyName))
                {
                    if (!propertyStringList.Contains(propertyName))
                    {
                        propertyStringList.Add(propertyName);
                    }
                }
            }
            else
            {
                if (!propertyStringList.Contains(propertyName))
                {
                    propertyStringList.Add(propertyName);
                }
            }
        }
    }
}
