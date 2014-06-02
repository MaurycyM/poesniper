using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PoeSniper
{
    public class ShopThreadPriceParser
    {
        public Dictionary<int, string> ParseShopThreadPrices(string shopThreadBody)
        {
            var itemFragmentLocations = AllIndexesOf(shopThreadBody, @"id=""item-fragment-");

            //Console.WriteLine(itemFragmentLocations);

            IndexOfAll2(shopThreadBody, "");


            return null;
        }

        private IEnumerable<int> IndexOfAll2(string sourceString, string subString)
        {
            var pattern = Regex.Escape("~b/o") + @"[A-Za-z0-9_\., ]+";

            var foo = Regex.Matches(sourceString, pattern).Cast<Match>().Select(m => new { index = m.Index, offer = m.ToString() });

            return null;
        }

        private static int[] AllIndexesOf(string str, string substr)
        {
            if (string.IsNullOrWhiteSpace(str) ||
                string.IsNullOrWhiteSpace(substr))
            {
                throw new ArgumentException("String or substring is not specified.");
            }

            var indexes = new List<int>();
            int index = 0;

            while ((index = str.IndexOf(substr, index, StringComparison.Ordinal)) != -1)
            {
                indexes.Add(index++);
            }

            return indexes.ToArray();
        }
    }
}
