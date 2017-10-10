using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeWrapper
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="TReadWrite">Reference type to read from and write to the named pipe</typeparam>
    public class NamedPipeClient<TReadWrite> : NamedPipeClient<TReadWrite, TReadWrite> where TReadWrite : class
    {
        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the <see cref="NamedPipeServer{TReadWrite}"/> specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">server name default is local.</param>
        public NamedPipeClient(string pipeName, string serverName = ".") : base(pipeName, serverName)
        {
        }
    }

    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="TRead">Reference type to read from the named pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to write to the named pipe</typeparam>
    public class NamedPipeClient<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// Gets or sets whether the client should attempt to reconnect when the pipe breaks
        /// due to an error or the other end terminating the connection.
        /// Default value is <c>true</c>.
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Invoked whenever a message is received from the server.
        /// </summary>
        public event ConnectionMessageEventHandler<TRead, TWrite> ServerMessage;

        /// <summary>
        /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
        /// </summary>
        public event ConnectionEventHandler<TRead, TWrite> Disconnected;

        /// <summary>
        /// Invoked when the client connects to the server (e.g., the pipe is opened).
        /// </summary>
        public event ConnectionEventHandler<TRead, TWrite> Connected;
        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
        /// </summary>
        public event PipeExceptionEventHandler Error;

        private readonly string pipeName;
        private NamedPipeConnection<TRead, TWrite> connection;

        private readonly AutoResetEvent connected;
        private readonly AutoResetEvent disconnected;

        private volatile bool closedExplicitly;
        /// <summary>
        /// the server name, which client will connect to.
        /// </summary>
        private string ServerName { get; set; }

        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the <see cref="Server{TRead, TWrite}"/> specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">the Name of the server, default is  local machine</param>
        public NamedPipeClient(string pipeName, string serverName)
        {
            this.pipeName = pipeName;
            ServerName = serverName;
            AutoReconnect = true;
            connected = new AutoResetEvent(false);
            disconnected = new AutoResetEvent(false);
        }

        /// <summary>
        /// Connects to the named pipe server asynchronously.
        /// This method returns immediately, possibly before the connection has been established.
        /// </summary>
        public void Start()
        {
            closedExplicitly = false;
            BeginListening();
        }

        /// <summary>
        ///     Sends a message to the server over a named pipe.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public void PushMessage(TWrite message)
        {
            if (connection != null)
                connection.PushMessage(message);
        }

        /// <summary>
        /// Closes the named pipe.
        /// </summary>
        public void Stop()
        {
            closedExplicitly = true;
            if (connection != null)
                connection.Close();
        }

        #region Wait for connection/disconnection

        public void WaitForConnection()
        {
            connected.WaitOne();
        }
        public void WaitForConnection(int millisecondsTimeout)
        {
            connected.WaitOne(millisecondsTimeout);
        }
        public void WaitForConnection(TimeSpan timeout)
        {
            connected.WaitOne(timeout);
        }

        public void WaitForDisconnection()
        {
            disconnected.WaitOne();
        }
        public void WaitForDisconnection(int millisecondsTimeout)
        {
            disconnected.WaitOne(millisecondsTimeout);
        }
        public void WaitForDisconnection(TimeSpan timeout)
        {
            disconnected.WaitOne(timeout);
        }

        #endregion

        #region Private methods

        private void BeginListening()
        {
            // Get the name of the data pipe that should be used from now on by this NamedPipeClient
            var handshake = PipeClientFactory.Connect<string, string>(pipeName, ServerName);
            var dataPipeName = handshake.ReadObject();
            handshake.Close();

            // Connect to the actual data pipe
            var dataPipe = PipeClientFactory.CreateAndConnectPipe(dataPipeName, ServerName);

            // Create a Connection object for the data pipe
            connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
            connection.Disconnected += OnDisconnected;
            connection.ReceiveMessage += OnReceiveMessage;
            connection.Error += ConnectionOnError;
            connection.Open();
            connected.Set();
            Connected?.Invoke(connection);
        }

        private void OnDisconnected(NamedPipeConnection<TRead, TWrite> connection)
        {
            if (Disconnected != null)
                Disconnected(connection);

            disconnected.Set();

            // Reconnect
            if (AutoReconnect && !closedExplicitly)
                Start();
        }

        private void OnReceiveMessage(NamedPipeConnection<TRead, TWrite> connection, TRead message)
        {
            if (ServerMessage != null)
                ServerMessage(connection, message);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        private void ConnectionOnError(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
        {
            OnError(exception);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        /// <param name="exception"></param>
        private void OnError(Exception exception)
        {
            if (Error != null)
                Error(exception);
        }

        #endregion
    }
}
