using Newtonsoft.Json;
using PoeSniper.PoeSniper.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PoeSniper
{
    public class GetNewShopThreadsResult
    {
        public int LastFetchPage { get; set; }
        public DateTime LastShopThreadDate { get; set; }
        public List<Tuple<string, DateTime>> NewShopThreads { get; set; }
    }

    public class ForumParser
    {
        private Dictionary<string, string> _forumsDictionary;
        private List<Forum> _forums;
        private const string forumsFileLocation = @"Data\forums.dat";

        private Regex _findShopsRegex = new Regex(@"<div><a href=""/forum/view-thread/(?<thread>\d+)", RegexOptions.Compiled);
        private Regex _threadRegex = new Regex(@"<td class=""thread"">.+<td class=""replies"">", RegexOptions.Compiled);
        private Regex _lastPostRegex = new Regex(@"<td class=""last_post"">.+</span></td>", RegexOptions.Compiled);
        private Regex _threadAddressRegex = new Regex(@"/forum/view-thread/(?<thread>\d+)", RegexOptions.Compiled);
        private Regex _lastPostDateRegex = new Regex(@"<span class=""post_date"">on (?<date>.+)</span>", RegexOptions.Compiled);

        private const string _threadTemplateUrl = @"http://www.pathofexile.com/forum/view-thread/";

        private ThreadParser _threadParser;

        public ForumParser()
        {
            InitializeForums();
            _threadParser = new ThreadParser();
        }

        private void InitializeForums() 
        {
            var forumsString = File.ReadAllText(forumsFileLocation);
            _forumsDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(forumsString);
            using (var context = new PoeSniperContext())
            {
                foreach (var forumDictionaryEntry in _forumsDictionary)
                {
                    if (!context.Forums.Select(f => f.Url).Contains(forumDictionaryEntry.Key))
                    {
                        var newForum = new Forum
                        {
                            Url = forumDictionaryEntry.Key,
                            LastShopThreadPage = 1,
                            LastShopThreadDate = DateTime.Now.AddDays(-7), // only process week old entries or newer
                            League = (League)Enum.Parse(typeof(League), forumDictionaryEntry.Value),
                            ShopThreads = new List<ShopThread>(),
                        };

                        Console.WriteLine("Created new forum: " + newForum.Url + "League: " + newForum.League);
                        context.Forums.Add(newForum);
                    }
                }

                context.SaveChanges();

                _forums = context.Forums.ToList();
            }
        }

        public async Task UpdateForums()
        {
            foreach (var forum in _forums)
            {
                var getNewShopThreadsResult = await GetNewShopThreads(forum);
                foreach (var shopThread in getNewShopThreadsResult.NewShopThreads)
                {
                    await _threadParser.ParseThread(forum, _threadTemplateUrl + shopThread.Item1, shopThread.Item2);
                }

                forum.LastShopThreadPage = getNewShopThreadsResult.LastFetchPage;
                forum.LastShopThreadDate = getNewShopThreadsResult.LastShopThreadDate;
                using (var context = new PoeSniperContext())
                {
                    context.Entry<Forum>(forum).State = EntityState.Modified;
                    context.SaveChanges();
                }
            }
        }

        public async Task<GetNewShopThreadsResult> GetNewShopThreads(Forum forum)
        {
            var forumUrl = forum.Url + @"/orderby/post-time/order/asc/page/";

            // scan few pages earlier than last time in case some posts get lost due to timing issues when people update them
            // we look at the last post date so we will not process the same thread twice, unless it was updated
            var lastFetchPage = Math.Max(forum.LastShopThreadPage - 2, 1); 
            var lastShopThreadDate = forum.LastShopThreadDate;
            
            bool done = false;
            var newShopThreads = new List<Tuple<string, DateTime>>();
            while (!done)
            {
                string forumBody = string.Empty;
                using (var client = new HttpClient())
                {
                    var httpResponse = await client.GetAsync(forumUrl + lastFetchPage);

                    Stream stream = null;
                    stream = await httpResponse.Content.ReadAsStreamAsync();

                    string value = string.Empty;
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        forumBody = reader.ReadToEnd();
                    }
                }

                var threadMatches = _threadRegex.Matches(forumBody);
                var lastPostMatches = _lastPostRegex.Matches(forumBody);

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
                    var shopThreadId = _threadAddressRegex.Match(threadMatch.ToString()).Groups["thread"].Value;
                    shopThreadIds.Add(shopThreadId);
                }

                var lastPostDates = new List<DateTime>();
                foreach (var lastPostMatch in lastPostMatches)
                {
                    var lastPostDate = DateTime.Parse(_lastPostDateRegex.Match(lastPostMatch.ToString()).Groups["date"].Value);
                    lastPostDates.Add(lastPostDate);
                }

                var threads = shopThreadIds.Zip(lastPostDates, (t, d) => new Tuple<string, DateTime>(t, d));

                // TODO: if two threads are updated at the same time we may lose the information about one of them
                var filteredThreads = threads.Where(t => t.Item2 > lastShopThreadDate);
                newShopThreads.AddRange(filteredThreads);
                lastFetchPage++;

                lastShopThreadDate = lastShopThreadDate > lastPostDates.Last() ? lastShopThreadDate : lastPostDates.Last();

                // TODO: LOG
                Console.WriteLine("So far discovered " + newShopThreads.Count() + " threads. Page: " + lastFetchPage);
            }

            var result = new GetNewShopThreadsResult
            {
                LastFetchPage = lastFetchPage,
                LastShopThreadDate = lastShopThreadDate,
                NewShopThreads = newShopThreads,
            };

            return result;
        }
    }
}
