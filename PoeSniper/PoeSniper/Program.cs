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
            var forumParser = new ForumParser();
            var task = forumParser.UpdateForums();
            task.Wait();
            //var result = forumParser.GetNewShopThreads(235, DateTime.Now.AddMinutes(-5));
            //result.Wait();

            //var threadParser = new ThreadParser();
            //foreach (var shopThread in result.Result)
            //{
            //    var items = threadParser.ParseThread(@"http://www.pathofexile.com/forum/view-thread/" + shopThread.Item1, shopThread.Item2);
            //    items.Wait();

            //}

            Console.WriteLine(sw.Elapsed);
        }
    }
}
