using PoeSniper.PoeSniper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoeSniper
{
    public class SocketsParser
    {
        private Dictionary<string, SocketColor> _socketColorMapping = new Dictionary<string, SocketColor>
        {
            { "S", SocketColor.Red },
            { "D", SocketColor.Green },
            { "I", SocketColor.Blue },
            { "G", SocketColor.White },
        };

        public List<GemSocket> ParseSockets(List<SocketJsonObject> sockets)
        {
            var result = new List<GemSocket>();
            foreach (var socket in sockets)
            {
                var resultSocket = new GemSocket { isLinked = socket.group == 0, Color = _socketColorMapping[socket.attr] };
                result.Add(resultSocket);
            }

            return result;
        }
    }
}
