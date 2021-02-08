# Unogames.Network

asynchronous socket server & client C#

 * [Examples](#examples)
    * [Example: TCP Socket server](#example-tcp-socket-server)
    * [Example: TCP Socket client](#example-tcp-socket-client)
    
    
    
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
            server.Start();
            for (; ; )
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                    continue;
                }

                // Multicast admin message to all sessions
                line = "(admin) " + line;
                server.SendAll(line);
            }
            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
    }
}
```

## Example: Create Socket Client
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
        public PangyaBinaryWriter Response { get; set; }
        public PlayerSession(Socket socket) : base(socket) { Response = new PangyaBinaryWriter(); }

        protected override void OnConnected()
        {
            Console.WriteLine($"Player session with Id {RemoteEndPoint} connected!");

            // Send invite message
            byte[] message = new byte[]
                {0x00, 0x0b, 0x00, 0x00, 0x00, 0x00, 1, 0x00, 0x00, 0x00, 0x75, 0x27, 0x00, 0x00};
            SendAsync(message);
        }

        public async Task SendResponse()
        {
            var data = Response.GetBytes();
            PangyaAPI.Crypt.Cryptor.Server_Packet(ref data, 1, 0);
            Response.Clear();
            await SendPacket(data);
        }

        Task<int> SendToAsync(byte[] buffer)
        {
            return Task.Factory.FromAsync(
                Socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, Socket),
                Socket.EndSend);
        }

        public async Task SendPacket(byte[] message)
        {
            await SendToAsync(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Player session with Id {RemoteEndPoint} disconnected!");
        }

        protected override void OnReceived(byte[] message, long offset, long size)
        {
            Task.Run(() =>
            {
                try
                {
                    var Id = BitConverter.ToInt16(new byte[] { message[5], message[6] }, 0);
                    switch (Id)
                    {
                        case 0x01:
                            {
                                #pragma warning disable 4014
                                Login();
                                #pragma warning restore 4014

                            }
                            break;
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                }
            });

        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Player session caught an error with code {error}");
        }


        public async Task Login()
        {
            Response.WriteUInt16(0x0010);
            Response.WritePStr("7430F52");//chave de autenficacao
            await SendResponse();

            Response.Write(new byte[] { 0x06, 0x00 });
            Response.WriteStr("Pangya 1!", 64);
            Response.WriteStr("Pangya 2!", 64);
            Response.WriteStr("Pangya 3!", 64);
            Response.WriteStr("Pangya 4!", 64);
            Response.WriteStr("Pangya 5!", 64);
            Response.WriteStr("Pangya 6!", 64);
            Response.WriteStr("Pangya 7!", 64);
            Response.WriteStr("Pangya 8!", 64);
            Response.WriteStr("Pangya 9!", 64);
            await SendResponse();

            Response.Write(new byte[] { 0x02, 0x00 });
            Response.WriteByte((byte)1);//count servers 
            Response.WriteStr("PangYa S7", 40);
            Response.WriteInt32(20201);//serverID
            Response.WriteInt32(2000);//max user
            Response.WriteInt32(1);//players online
            Response.WriteStr("127.0.0.1", 18);//ip server
            Response.WriteInt32(7997);//port 
            Response.WriteInt32(2048);//property
            Response.WriteUInt32(0); // Angelic Number
            Response.WriteUInt16((ushort)0);//Flag event
            Response.WriteUInt16(0);//unknown
            Response.WriteInt32(100);//pang rate?
            Response.WriteUInt16(0);//Icon Server    
            await SendResponse();
        }
    }
}
