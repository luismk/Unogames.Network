# Unogames.Network

asynchronous socket server & client C#

 * [Examples](#examples)
    * [Example: TCP Socket Server](#example-tcp-socket-server)
    * [Example: TCP Socket Client](#example-socket-client)
    
    
    
    # Examples

## Example: TCP Socket Server
Here comes the example of the TCP chat server. It handles multiple TCP client
sessions and multicast received message from any session to all ones. Also it
is possible to send admin message directly from the server.

```c#
using System;
using System.Net;
using System.Net.Sockets;
using Unogames.Network.Config;
using Unogames.Network.ClientBase;
using Unogames.Network.SocketBase;
namespace Pangya_LoginServer
{
    public class LoginServer : TcpServer
    {
        public LoginServer(ServerConfig config, string name) : base(config, name) { }

        public override bool Disconnect(Session session)
        {
            if (!session.IsConnected)
                return false;
            
            session.Close();

            // Call the session disconnected handler in the server
            this.OnDisconnectedInternal(session);

            // Unregister session
            SessionRemove(session.ConnectionID);
            return true;
        }

        protected override Session CreateSession(Socket socket)
        {
            var player = new PlayerSession(socket) {Server = this, ConnectionID = NextConnectionID };
            NextConnectionID++;
            // Register the session
            Sessions.TryAdd(player.ConnectionID, player);

            // Call the session connected handler in the server
            this.OnConnectedInternal(player);

            return player;
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP server caught an error with code {error}");
        }
    }
}


    class Program
    {
        static void Main(string[] args)
        {
            var Config = new Unogames.Network.Config.ServerConfig();

            if (Config.Ip == null)
            {
                Console.Write("-- Please input target LoginServer IP: ");
                Config.Ip = Console.ReadLine();
                Console.WriteLine();
            }

            if (Config.Port == 0)
            {
                Console.Write("-- Please input target LoginServer Port: ");
                Config.Port = int.Parse(Console.ReadLine());
                Console.WriteLine();
            }

            var server = new ServerTest(Config, "PangYa_LoginServer");
            if (server.IsRunning == false)
            {
                Console.WriteLine("Use the commands: ");
                Console.WriteLine(Environment.NewLine);
                Console.WriteLine(" 'start' to start the server");
                Console.WriteLine(" 'stop' to stop the server");
                Console.WriteLine(" 'restart ' to restart the server");
                Console.WriteLine(Environment.NewLine);
            }
            for (; ; )
            {
                var command = Console.ReadLine().Split(new char[] { ' ' }, 2);
                switch (command[0].ToLower())
                {
                    case "": break;
                    case "stop":
                        {
                            Console.Write("Server stopping...");
                            server.Stop();
                            Console.WriteLine("Done!");
                        }
                        break;
                    case "start":
                        server.Start();
                        break;
                    case "cls":
                    case "clear":
                        {
                            Console.Clear();
                        }
                        break;
                    case "restart":
                        Console.Write("Server restarting...");
                        server.Restart();
                        Console.WriteLine("Done!");
                        break;
                    default:
                        Console.WriteLine("command not found");
                        break;
                }
            }
    }
}
```

## Example: Socket Client
Here comes the example of the TCP chat client. It connects to the TCP chat
server and allows to send message to it and receive new messages.

```c#
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unogames.Network.ClientBase;
using PangyaAPI.Helper.BinaryModels;
namespace Pangya_LoginServer
{
    public class PlayerSession : Session
    {
        public PlayerSession(Socket socket) : base(socket) {}
        
        //send a welcome message to the connected client
        protected override void OnConnected()
        {
            Console.WriteLine($"Player session with Id {RemoteEndPoint} connected!");

            // Send invite message
            string message = $"bem vindo ! {RemoteEndPoint}";
            SendAsync(message);
        }
        
        //a disconnection notice it will be here
        protected override void OnDisconnected()
        {
            Console.WriteLine($"Player session with Id {RemoteEndPoint} disconnected!");
        }
        
        //try to deal with the message received here
        protected override void OnReceived(byte[] message, long offset, long size)
        {
       
        }
        
        //error by client-connection
        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Player session caught an error with code {error}");
        }
    }
}
