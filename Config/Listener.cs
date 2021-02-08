using System.Net;
namespace Unogames.Network.Config
{
    public class Listener
    {
        public Listener(ServerConfig config)
        {
            IPAddress ipAddress;
            switch (config.Ip.ToLower())
            {
                case "any":
                    ipAddress = IPAddress.Any;
                    break;

                case "ipv6any":
                    ipAddress = IPAddress.IPv6Any;
                    break;

                default:
                    ipAddress = IPAddress.Parse(config.Ip);
                    break;
            }

            EndPoint = new IPEndPoint(ipAddress, config.Port);
            BackLog = config.BackLog <= 0 ? DefaultBackLog : config.BackLog;
        }

        public IPEndPoint EndPoint { get; set; }

        public int BackLog { get; set; }

        public const int DefaultBackLog = 20;
    }
}
