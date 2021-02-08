using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Unogames.Network.Config;
using Unogames.Network.ClientBase;
using Unogames.Network.Exceptions;
using Unogames.Network.Utils;
namespace Unogames.Network.SocketBase
{
    /// <summary>
    /// TCP server is used to connect, disconnect and manage TCP sessions
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public abstract class TcpServer
    {
        /// <summary>
        /// Configuration by Server
        /// </summary>
        public Listener ServerConfig { get; set; }
        /// <summary>
        /// Connection_Id
        /// </summary>
        public int NextConnectionID { get; set; }

        /// <summary>
        /// Number of sessions connected to the server
        /// </summary>
        public long ConnectedSessions { get { return Sessions.Count; } }
        /// <summary>
        /// Number of bytes pending sent by the server
        /// </summary>
        public long BytesPending { get { return _bytesPending; } }
        /// <summary>
        /// Number of bytes sent by the server
        /// </summary>
        public long BytesSent { get { return _bytesSent; } }
        /// <summary>
        /// Number of bytes received by the server
        /// </summary>
        public long BytesReceived { get { return _bytesReceived; } }
        
        // Server sessions
        protected readonly ConcurrentDictionary<int, Session> Sessions;

        #region Start/Stop server

        // Server acceptor
        private Socket Listener;
        private SocketAsyncEventArgs EventSocket;

        // Server statistic
        internal long _bytesPending;
        internal long _bytesSent;
        internal long _bytesReceived;

        /// <summary>
        /// Is the server started?
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// Is the server accepting new clients?
        /// </summary>
        public bool IsAccepting { get; private set; }


        public TcpServer(ServerConfig config, string ServerName)
        {
            Console.Title = $"Unogames.Network: {ServerName}";

            if (config == null)
                throw new UnogamesConfigurationException("failed to try to configure server");

            if (config.Port <= 0)
                throw new UnogamesConfigurationException($"{config.Port} is not a valid port.");

            if (config.Ip == null || config.Ip == "")
                throw new UnogamesConfigurationException($"Invalid host : {config.Ip}");

            ServerConfig = new Listener(config);
            Sessions = new ConcurrentDictionary<int, Session>();
        }

        /// <summary>
        /// Start the server
        /// </summary>
        /// <returns>'true' if the server was successfully started, 'false' if the server failed to start</returns>
        public virtual bool Start()
        {
            try
            {
                Debug.Assert(!IsRunning, "TCP server is already started!");
                if (IsRunning)
                    return false;

                // Setup acceptor event arg
                EventSocket = new SocketAsyncEventArgs();
                EventSocket.Completed += OnAsyncCompleted;

                // Create a new acceptor socket
                Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Bind the acceptor socket to the IP endpoint
                Listener.Bind(ServerConfig.EndPoint);

                // Start listen to the acceptor socket with the given accepting backlog size
                Listener.Listen(32);

                // Reset statistic
                _bytesPending = 0;
                _bytesSent = 0;
                _bytesReceived = 0;

                // Update the started flag
                IsRunning = true;

                // Call the server started handler
                OnStarted();

                // Perform the first server accept
                IsAccepting = true;
                StartAccept(EventSocket);
                return true;
            }
            catch
            {
                ConsoleWrite.Error($" failed to try the port => {ServerConfig.EndPoint.Port}");
                return false;
            }
        }

        public abstract bool Disconnect(Session session);

        /// <summary>
        /// Stop the server
        /// </summary>
        /// <returns>'true' if the server was successfully stopped, 'false' if the server is already stopped</returns>
        public virtual bool Stop()
        {
            Debug.Assert(IsRunning, "TCP server is not started!");
            if (!IsRunning)
                return false;

            // Stop accepting new clients
            IsAccepting = false;

            // Reset acceptor event arg
            EventSocket.Completed -= OnAsyncCompleted;

            // Close the acceptor socket
            Listener.Close();

            // Dispose the acceptor socket
            Listener.Dispose();

            // Dispose event arguments
            EventSocket.Dispose();

            // Disconnect all sessions
            DisconnectAll();

            // Update the started flag
            IsRunning = false;

            // Call the server stopped handler
            OnStopped();

            return true;
        }

        /// <summary>
        /// Restart the server
        /// </summary>
        /// <returns>'true' if the server was successfully restarted, 'false' if the server failed to restart</returns>
        public virtual bool Restart()
        {
            if (!Stop())
                return false;

            while (IsRunning)
                Thread.Yield();

            return Start();
        }

        #endregion

        #region Accepting clients

        /// <summary>
        /// Start accept a new client connection
        /// </summary>
        private void StartAccept(SocketAsyncEventArgs e)
        {
            // Socket must be cleared since the context object is being reused
            e.AcceptSocket = null;

            // Async accept a new client connection
            if (!Listener.AcceptAsync(e))
                ProcessAccept(e);
        }

        /// <summary>
        /// Process accepted client connection
        /// </summary>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // Create a new session to register
                CreateSession(e.AcceptSocket);
            }
            else
                SendError(e.SocketError);

            // Accept the next client connection
            if (IsAccepting)
                StartAccept(e);
        }

        /// <summary>
        /// This method is the callback method associated with Socket.AcceptAsync()
        /// operations and is invoked when an accept operation is complete
        /// </summary>
        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        #endregion

        #region Session factory

        /// <summary>
        /// Create TCP session factory method
        /// </summary>
        /// <returns>TCP session</returns>
        protected virtual Session CreateSession(Socket socket) { return new Session(socket) { Server = this}; }

        #endregion

        #region Session management

        /// <summary>
        /// Disconnect all connected sessions
        /// </summary>
        /// <returns>'true' if all sessions were successfully disconnected, 'false' if the server is not started</returns>
        public virtual bool DisconnectAll()
        {
            if (!IsRunning)
                return false;

            // Disconnect all sessions
            foreach (var session in Sessions.Values)
                session.Close();

            return true;
        }

        /// <summary>
        /// Find a session with a given Id
        /// </summary>
        /// <param name="id">Session Id</param>
        /// <returns>Session with a given Id or null if the session it not connected</returns>
        public Session FindSession(int id)
        {
            // Try to find the required session
            return Sessions.TryGetValue(id, out Session result) ? result : null;
        }

        /// <summary>
        /// Unregister session by Id
        /// </summary>
        /// <param name="id">Session Id</param>
        public void SessionRemove(int id)
        {
            // Unregister session by Id
            Sessions.TryRemove(id, out Session temp);
            if (temp.IsConnected) { }
        }

        #endregion

        #region Multicasting

        /// <summary>
        /// Multicast data to all connected sessions
        /// </summary>
        /// <param name="buffer">Buffer to multicast</param>
        /// <returns>'true' if the data was successfully multicasted, 'false' if the data was not multicasted</returns>
        public virtual bool SendAll(byte[] buffer) { return SendAll(buffer, 0, buffer.Length); }

        /// <summary>
        /// Multicast data to all connected clients
        /// </summary>
        /// <param name="buffer">Buffer to multicast</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>'true' if the data was successfully multicasted, 'false' if the data was not multicasted</returns>
        public virtual bool SendAll(byte[] buffer, long offset, long size)
        {
            if (!IsRunning)
                return false;

            if (size == 0)
                return true;

            // Multicast data to all sessions
            foreach (var session in Sessions.Values)
                session.SendAsync(buffer, offset, size);

            return true;
        }

        /// <summary>
        /// Multicast text to all connected clients
        /// </summary>
        /// <param name="text">Text string to multicast</param>
        /// <returns>'true' if the text was successfully multicasted, 'false' if the text was not multicasted</returns>
        public virtual bool SendAll(string text) { return SendAll(Encoding.UTF8.GetBytes(text)); }

        #endregion

        #region Server handlers

        /// <summary>
        /// Handle server started notification
        /// </summary>
        protected virtual void OnStarted() { ConsoleWrite.WriteLine($"[SERVER]: Started in {ServerConfig.EndPoint}"); }
        /// <summary>
        /// Handle server stopped notification
        /// </summary>
        protected virtual void OnStopped() { ConsoleWrite.WriteLine($"[SERVER]: Stopped in {ServerConfig.EndPoint}"); }

        /// <summary>
        /// Handle session connected notification
        /// </summary>
        /// <param name="session">Connected session</param>
        protected virtual void OnConnected(Session session) { }
        /// <summary>
        /// Handle session disconnected notification
        /// </summary>
        /// <param name="session">Disconnected session</param>
        protected virtual void OnDisconnected(Session session) { }

        /// <summary>
        /// Handle error notification
        /// </summary>
        /// <param name="error">Socket error code</param>
        protected virtual void OnError(SocketError error) { }

        public void OnConnectedInternal(Session session) { OnConnected(session); }
        public void OnDisconnectedInternal(Session session) { OnDisconnected(session); }

        #endregion

        #region Error handling

        /// <summary>
        /// Send error notification
        /// </summary>
        /// <param name="error">Socket error code</param>
        private void SendError(SocketError error)
        {
            // Skip disconnect errors
            if ((error == SocketError.ConnectionAborted) ||
                (error == SocketError.ConnectionRefused) ||
                (error == SocketError.ConnectionReset) ||
                (error == SocketError.OperationAborted) ||
                (error == SocketError.Shutdown))
                return;

            OnError(error);
        }

        #endregion
    }
}
