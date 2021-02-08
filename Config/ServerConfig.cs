using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unogames.Network.Config
{
    public class ServerConfig
    {
        public string Ip { get; set; }

        public int Port { get; set; }

        public int BackLog { get; set; }

        public ServerConfig ServerLocal(int port)
        {
            Ip = "127.0.0.1";
            Port = port;
            BackLog = 32;
            return this;
        }
    }
}
