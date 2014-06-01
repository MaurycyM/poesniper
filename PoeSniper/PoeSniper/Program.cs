using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoeSniper
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var fetcher = new ShopThreadFetcher();
            var result = fetcher.FetchShopThreads(235, DateTime.Now.AddMinutes(-20));
            result.Wait();

            var itemFetcher = new ItemFetcher();
            foreach (var shopThreadId in result.Result)
            {
                var items = itemFetcher.FetchItems(@"http://www.pathofexile.com/forum/view-thread/" + shopThreadId);
                items.Wait();

            }

            Console.WriteLine(sw.Elapsed);
        }
    }
}
