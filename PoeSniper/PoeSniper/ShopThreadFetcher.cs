using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PoeSniper
{
    public class ShopThreadFetcher
    {
        private string _forumUri = @"http://www.pathofexile.com/forum/view-forum/509/orderby/post-time/order/asc/page/";

        public async Task<string[]> FetchShopThreads(int lastFetchPage, DateTime lastFetchDate)
        {
            var findShopsRegex = new Regex(@"<div><a href=""/forum/view-thread/(?<thread>\d+)", RegexOptions.Compiled | RegexOptions.Multiline);

            var threadRegex = new Regex(@"<td class=""thread"">.+<td class=""replies"">");
            var lastPostRegex = new Regex(@"<td class=""last_post"">.+</span></td>");

            var threadAddressRegex = new Regex(@"/forum/view-thread/(?<thread>\d+)");
            var lastPostDateRegex = new Regex(@"<span class=""post_date"">on (?<date>.+)</span>");
            
            bool done = false;
            var result = new List<string>();
            while (!done)
            {
                string forumBody = string.Empty;
                using (var client = new HttpClient())
                {
                    var httpResponse = await client.GetAsync(_forumUri + lastFetchPage);

                    Stream stream = null;
                    stream = await httpResponse.Content.ReadAsStreamAsync();

                    string value = string.Empty;
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        forumBody = reader.ReadToEnd();
                    }
                }

                var threadMatches = threadRegex.Matches(forumBody);
                var lastPostMatches = lastPostRegex.Matches(forumBody);

                Debug.Assert(
                    threadMatches.Count == lastPostMatches.Count,
                    "number of threads and last posts should be the same");

                if (threadMatches.Count == 0)
                {
                    done = true;
                    break;
                }

                var shopThreadIds = new List<string>();
                foreach (var threadMatch in threadMatches)
                {
                    var shopThreadId = threadAddressRegex.Match(threadMatch.ToString()).Groups["thread"].Value;
                    shopThreadIds.Add(shopThreadId);
                }

                var lastPostDates = new List<DateTime>();
                foreach (var lastPostMatch in lastPostMatches)
                {
                    var lastPostDate = DateTime.Parse(lastPostDateRegex.Match(lastPostMatch.ToString()).Groups["date"].Value);
                    lastPostDates.Add(lastPostDate);
                }

                var threads = shopThreadIds.Zip(lastPostDates, (t, d) => new Tuple<string, DateTime>(t, d));

                var filteredThreads = threads.Where(t => t.Item2 > lastFetchDate);
                result.AddRange(filteredThreads.Select(t => t.Item1));
                lastFetchPage++;
            }

            return result.ToArray();
        }
    }
}
