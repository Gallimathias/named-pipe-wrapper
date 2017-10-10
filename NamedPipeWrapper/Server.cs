using NamedPipeWrapper.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipeWrapper
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeServerStream"/> and provides multiple simultaneous client connection handling.
    /// </summary>
    /// <typeparam name="TRead">Reference type to read from the named pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to write to the named pipe</typeparam>
    public class Server<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// Invoked whenever a client connects to the server.
        /// </summary>
        public event ConnectionEventHandler<TRead, TWrite> ClientConnected;

        /// <summary>
        /// Invoked whenever a client disconnects from the server.
        /// </summary>
        public event ConnectionEventHandler<TRead, TWrite> ClientDisconnected;

        /// <summary>
        /// Invoked whenever a client sends a message to the server.
        /// </summary>
        public event ConnectionMessageEventHandler<TRead, TWrite> ClientMessage;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation.
        /// </summary>
        public event PipeExceptionEventHandler Error;

        private readonly string pipeName;
        private readonly PipeSecurity pipeSecurity;
        private readonly ConcurrentDictionary<int, NamedPipeConnection<TRead, TWrite>> connections;

        private int nextPipeId;

        private volatile bool shouldKeepRunning;
        private volatile bool isRunning;

        private CancellationTokenSource cancellationToken;

        /// <summary>
        /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the pipe to listen on</param>
        /// <param name="pipeSecurity">Access control role object for the pipe</param>
        public Server(string pipeName, PipeSecurity pipeSecurity)
        {
            connections = new ConcurrentDictionary<int, NamedPipeConnection<TRead, TWrite>>();
            this.pipeName = pipeName;
            this.pipeSecurity = pipeSecurity;
        }

        /// <summary>
        /// Begins listening for client connections in a separate background thread.
        /// This method returns immediately.
        /// </summary>
        public void Start()
        {
            shouldKeepRunning = true;
            cancellationToken = new CancellationTokenSource();
            BeginListening();
        }

        /// <summary>
        /// Sends a message to all connected clients asynchronously.
        /// This method returns immediately, possibly before the message has been sent to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void PushMessage(TWrite message)
        {
            foreach (var client in connections.Values)
            {
                client.PushMessage(message);
            }
        }

        /// <summary>
        /// push message to the given client.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clientName"></param>
        public void PushMessage(TWrite message, string clientName) =>
            connections.Values.FirstOrDefault(c => c.Name == clientName).PushMessage(message);

        /// <summary>
        /// Closes all open client connections and stops listening for new ones.
        /// </summary>
        public void Stop()
        {
            foreach (var client in connections.Values)
                client.Close();

            shouldKeepRunning = false;
            cancellationToken.Cancel();
            cancellationToken.Dispose();
            cancellationToken = null;
        }

        #region Private methods

        private void BeginListening()
        {
            if (shouldKeepRunning)
                Task.Run(() => WaitForConnections(pipeName, pipeSecurity, OnClientConnect), cancellationToken.Token);
        }

        private void WaitForConnections(string pipeName, PipeSecurity pipeSecurity,
            Func<NamedPipeServerStream, bool> callback)
        {
            isRunning = true;

            var connectionPipeName = GetNextConnectionPipeName(pipeName);

            var handshakePipe = PipeServerFactory.CreateAndConnectPipe(pipeName, pipeSecurity);
            var handshakeWrapper = new PipeStreamWrapper<string, string>(handshakePipe);
            var dataPipe = PipeServerFactory.CreatePipe(connectionPipeName, pipeSecurity);

            // Send the client the name of the data pipe to use               
            handshakeWrapper.WriteObject(connectionPipeName);
            handshakeWrapper.WaitForPipeDrain();
            handshakeWrapper.Close();

            // Wait for the client to connect to the data pipe                
            dataPipe.WaitForConnection();
            Task.Run(() => callback?.Invoke(dataPipe));

            isRunning = false;
            if (shouldKeepRunning)
                Task.Run(() => WaitForConnections(pipeName, pipeSecurity, OnClientConnect), cancellationToken.Token);
        }

        private bool OnClientConnect(NamedPipeServerStream dataPipe)
        {
            // Add the client's connection to the list of connections
            var connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
            connection.ReceiveMessage += ClientOnReceiveMessage;
            connection.Disconnected += ClientOnDisconnected;
            connection.Error += ConnectionOnError;
            connection.Open();

            var res = connections.TryAdd(connection.Id, connection);
            ClientOnConnected(connection);
            return res;
        }

        private void ClientOnConnected(NamedPipeConnection<TRead, TWrite> connection)
            => ClientConnected?.Invoke(connection);

        private void ClientOnReceiveMessage(NamedPipeConnection<TRead, TWrite> connection, TRead message)
            => ClientMessage?.Invoke(connection, message);

        private void ClientOnDisconnected(NamedPipeConnection<TRead, TWrite> connection)
        {
            if (connection == null)
                return;

            connections.TryRemove(connection.Id, out NamedPipeConnection<TRead, TWrite> tmpCon);

            ClientDisconnected?.Invoke(tmpCon);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        private void ConnectionOnError(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
            => OnError(exception);

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        /// <param name="exception"></param>
        private void OnError(Exception exception)
            => Error?.Invoke(exception);

        private string GetNextConnectionPipeName(string pipeName)
            => $"{pipeName}_{++nextPipeId}";

        private static void Cleanup(NamedPipeServerStream pipe)
        {
            pipe?.Close();
            pipe?.Dispose();
        }

        #endregion
    }
}
