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
        private Regex valueRangePropertyRegex = new Regex(@"[^0-9" + Regex.Escape("-") + @"]*(?<valueRange>\d+-\d+) .*", RegexOptions.Compiled);
        private Regex valuePropertyRegex = new Regex(@"[^0-9" + Regex.Escape("-") + @"]*(?<value>" + Regex.Escape("+") + @"?-?\d+)", RegexOptions.Compiled);

        private List<string> implicitProperties = new List<string>();
        private List<string> explicitProperties = new List<string>();
        private List<string> staticImplicitProperties = new List<string>();
        private List<string> staticExplicitProperties = new List<string>();

        private const string implicitPropertiesFileLocation = @"Data/ImplicitProperties.dat";
        private const string explicitPropertiesFileLocation = @"Data/ExplicitProperties.dat";
        private const string staticImplicitPropertiesFileLocation = @"Data/StaticImplicitProperties.dat";
        private const string staticExplicitPropertiesFileLocation = @"Data/StaticExplicitProperties.dat";

        public MagicPropertiesParser()
        {
            implicitProperties = File.ReadAllLines(implicitPropertiesFileLocation).ToList();
            explicitProperties = File.ReadAllLines(explicitPropertiesFileLocation).ToList();
            staticImplicitProperties = File.ReadAllLines(staticImplicitPropertiesFileLocation).ToList();
            staticExplicitProperties = File.ReadAllLines(staticExplicitPropertiesFileLocation).ToList();
        }

        public List<MagicProperty> ParseMagicProperties(List<string> propertyStrings, bool isImplicit, bool isUnique)
        {
            List<MagicProperty> magicProperties = new List<MagicProperty>();
            foreach (var propertyString in propertyStrings)
            {
                var staticProperties = isImplicit ? staticImplicitProperties : staticExplicitProperties;
                var properties = isImplicit ? implicitProperties : explicitProperties;
                var staticPropertiesFileLocation = isImplicit ? staticImplicitPropertiesFileLocation : staticExplicitPropertiesFileLocation;
                var propertiesFileLocation = isImplicit ? implicitPropertiesFileLocation : explicitPropertiesFileLocation;

                if (staticProperties.Contains(propertyString))
                {
                    var staticProperty = new MagicProperty { Value = null, Name = propertyString, IsImplicit = isImplicit };
                    magicProperties.Add(staticProperty);
                    continue;
                }

                List<MagicProperty> rangeProperties;
                if (TryParseValueRangeProperty(propertyString, properties, propertiesFileLocation, isImplicit, isUnique, out rangeProperties))
                {
                    magicProperties.AddRange(rangeProperties);
                    continue;
                }

                MagicProperty valueProperty;
                if (TryParseValueProperty(propertyString, properties, propertiesFileLocation, isImplicit, isUnique, out valueProperty))
                {
                    magicProperties.Add(valueProperty);
                    continue;
                }

                // if we get here it means this is a static property that was not known before and should be added to the list 
                AddPropertyStringToList(propertyString, staticProperties, staticPropertiesFileLocation, isUnique);
                var unknownStaticProperty = new MagicProperty { Value = null, Name = propertyString, IsImplicit = isImplicit };
                magicProperties.Add(unknownStaticProperty);
            }

            return magicProperties;
        }

        private bool TryParseValueRangeProperty(
            string propertyString,
            List<string> propertyStringList,
            string propertyListFileLocation,
            bool isImplicit,
            bool isUnique,
            out List<MagicProperty> rangeProperties)
        {
            rangeProperties = new List<MagicProperty>();
            var valueRangePropertyMatch = valueRangePropertyRegex.Match(propertyString);
            if (valueRangePropertyMatch.Success)
            {
                var valueRange = valueRangePropertyMatch.Groups["valueRange"].Value;
                var minValue = valueRange.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[0];
                var maxValue = valueRange.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[1];

                var minValueProperty = new MagicProperty { Value = int.Parse(minValue), Name = propertyString.Replace(valueRange, "X Min"), IsImplicit = isImplicit };
                var maxValueProperty = new MagicProperty { Value = int.Parse(maxValue), Name = propertyString.Replace(valueRange, "X Max"), IsImplicit = isImplicit };

                AddPropertyStringToList(minValueProperty.Name, propertyStringList, propertyListFileLocation, isUnique);
                AddPropertyStringToList(maxValueProperty.Name, propertyStringList, propertyListFileLocation, isUnique);

                rangeProperties.Add(minValueProperty);
                rangeProperties.Add(maxValueProperty);

                return true;
            }

            return false;
        }

        private bool TryParseValueProperty(
            string propertyString,
            List<string> propertyStringList,
            string propertyListFileLocation,
            bool isImplicit,
            bool isUnique,
            out MagicProperty valueProperty)
        {
            valueProperty = null;
            var match = valuePropertyRegex.Match(propertyString);
            if (match.Success)
            {
                var value = match.Groups["value"].Value;
                valueProperty = new MagicProperty { Value = int.Parse(value), Name = propertyString.Replace(value, "X"), IsImplicit = isImplicit };

                AddPropertyStringToList(valueProperty.Name, propertyStringList, propertyListFileLocation, isUnique);
                return true;
            }

            return false;
        }

        private void AddPropertyStringToList(string propertyString, List<string> propertyStringList, string propertyListFileLocation, bool isUnique)
        {
            if (!propertyStringList.Contains(propertyString))
            {
                // TODO: this is just to build list of non-unique properties first, can be removed once this is done
                if (isUnique)
                {
                    Console.WriteLine("Unknown property: '" + propertyString + "' on unique item - skipping");
                    return;
                }

                Console.WriteLine("Adding new property: '" + propertyString + "' to: " + propertyListFileLocation);
                propertyStringList.Add(propertyString);
                File.WriteAllLines(propertyListFileLocation, propertyStringList.ToArray());
            }
        }





        //        if (isCorruptedOrUnique)
        //        {
        //            if (corruptedImplictPropertiesStatic.Contains(propertyString) ||
        //                staticUniqueItemMagicProperties.Contains(propertyString))
        //            {
        //                var staticCorruptedOrUniqueProperty = new MagicProperty { Name = propertyString, IsImplicit = isImplicit };
        //                result.Add(staticCorruptedOrUniqueProperty);
        //                continue;
        //            }
        //        }


        //        var damageRangeMatch = damageRangeRegex.Match(propertyString);
        //        if (damageRangeMatch.Success)
        //        {
        //            var damageRange = damageRangeMatch.Groups["damageRange"].Value;
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
        //            var genericProperty = new MagicProperty { Value = int.Parse(value), Name = propertyString.Replace(value, "X"), IsImplicit = isImplicit };
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

        // used to build property name dictionary, can be commented out once the dictionary is built
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
