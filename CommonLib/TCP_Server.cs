using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace CommonLib
{
    public class TCP_Server
    {
        #region Variables
        private object _serverLock;
        private TcpListener _server;
        private List<TCP_Server_Client> _clients;
        private object _clientsLock;
        private bool _waiting;
        private object _waitingLock;
        private bool _run;
        private object _runLock;
        private bool _isRunning;
        private object _isRunningLock;
        #endregion

        #region Properties
        public int ActiveConnections
        {
            get { return Clients.Count; }
        }
        public bool Running
        {
            get { return Run; }
        }
        public string Address
        {
            get;
            set;
        }
        public List<TCP_Server_Client> Clients
        {
            get { lock (_clientsLock) { return _clients; } }
            set { lock (_clientsLock) { _clients = value; } }
        }

        private bool Waiting
        {
            get { lock (_waitingLock) { return _waiting; } }
            set { lock (_waitingLock) { _waiting = value; } }
        }
        private bool Run
        {
            get { lock (_runLock) { return _run; } }
            set { lock (_runLock) { _run = value; } }
        }
        private bool IsRunning
        {
            get { lock (_isRunningLock) { return _isRunning; } }
            set { lock (_isRunningLock) { _isRunning = value; } }
        }
        #endregion

        #region Events
        public event EventHandler<TCP_Message> MessageReceived;
        public event EventHandler<TCP_Server_Client> ClientConnected;
        public event EventHandler<string> ClientDisconnected;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for generic TCP server class
        /// </summary>
        public TCP_Server()
        {
            // Perpare client list
            _clients = new List<TCP_Server_Client>();

            // Initialize underlying property values
            _serverLock = new object();
            _clientsLock = new object();
            _waiting = false;
            _waitingLock = new object();
            _run = false;
            _runLock = new object();
            _isRunning = false;
            _isRunningLock = new object();
        }
        #endregion

        #region User Methods
        /// <summary>
        /// Start a generic TCP server on the specified port
        /// </summary>
        /// <param name="port">The port to which we'll listen for TCP connections</param>
        public void Start(int port)
        {
            // Make sure we're not already running
            if (Run)
                return;

            // Start listening for incoming TCP connections
            Address = GetLocalIPAddress();
            lock (_serverLock)
            {
                _server = new TcpListener(IPAddress.Parse(Address), port);
                _server.Start();
            }
            Run = true;
            Waiting = false;
            System.Threading.Thread conThread = new System.Threading.Thread(ConnectionLoop);
            conThread.Start();
        }
        /// <summary>
        /// Shut down the TCP server and stop communication with clients
        /// </summary>
        public void Shutdown()
        {
            // Make sure we haven't already stopped
            if (!Run)
                return;

            // Stop listening for connections
            Run = false;
            while (IsRunning)
                System.Threading.Thread.Sleep(100);
            lock (_serverLock)
            {
                _server.Stop();
                _server = null;
            }

            // Shutdown any connected clients
            foreach (TCP_Server_Client client in Clients)
            {
                client.Shutdown();
            }
            Clients.Clear();
        }
        /// <summary>
        /// Send a message to all connected clients except for specified client
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="clientAddressToExclude">The client address not to send data to</param>
        /// <param name="id">Id of the client not to send data to</param>
        public void SendAllBut(byte[] data, string clientAddressToExclude, Guid id)
        {
            // Loop through connected clients
            foreach (TCP_Server_Client client in Clients.ToArray())
            {
                // Don't send message to exclusion address
                if (client.Address == clientAddressToExclude)
                    continue;
                client.Send(data);
            }
        }
        /// <summary>
        /// Send a message to a specified client
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="clientAddress">Address of client to send data to</param>
        /// <param name="id">Id of the client to send data to</param>
        public void SendTo(byte[] data, string clientAddress, Guid id)
        {
            // Find the specified client
            TCP_Server_Client client = Clients.FirstOrDefault(c => c.Address.Equals(clientAddress)
            && c.ID.Equals(id));
            if (client == null)
                return;

            // Send the data (formatting is handled by the underlying client class)
            client.Send(data);
        }
        /// <summary>
        /// Send a message to all connected clients
        /// </summary>
        /// <param name="data">Data to send</param>
        public void SendAll(byte[] data)
        {
            // Loop through all connected clients
            foreach (TCP_Server_Client client in Clients.ToArray())
            {
                // Send the data (formatting is handled by underlying client class)
                client.Send(data);
            }
        }
        public void DisconnectClient(string clientAddress, Guid id)
        {
            // Search for specified client
            TCP_Server_Client target = null;
            foreach (TCP_Server_Client client in Clients)
            {
                if (client.Address.Equals(clientAddress) && client.ID.Equals(id))
                {
                    target = client;
                    break;
                }
            }

            // Make sure client was found
            if (target == null)
                return;

            // Disconnect client and remove from list
            target.Shutdown();
            Clients.Remove(target);
        }
        #endregion

        #region Event Handlers
        private void OnClientConnected(IAsyncResult ar)
        {
            TcpClient client = null;
            lock (_serverLock)
            {
                // The server wasn't started but we're trying to shut down
                if (_server == null)
                    return;

                // Get client
                client = _server.EndAcceptTcpClient(ar);
            }

            // Create and initialize client wrapper
            TCP_Server_Client sClient = new TCP_Server_Client();
            sClient.MessageReceived += OnMessageReceivedFromClient;
            sClient.LostConnection += OnClientDisconnectedFromServer;
            sClient.Connect(client);

            // Notify that a new client has connected
            if (ClientConnected != null)
                ClientConnected.BeginInvoke(this, sClient, null, null);

            // Add client to list
            Clients.Add(sClient);

            // Start accepting new clients
            Waiting = false;
        }
        private void OnMessageReceivedFromClient(object sender, TCP_Message e)
        {
            if (MessageReceived != null)
                MessageReceived.BeginInvoke(this, e, null, null);
        }
        private void OnClientDisconnectedFromServer(object sender, EventArgs e)
        {
            TCP_Server_Client client = sender as TCP_Server_Client;
            client.MessageReceived -= OnMessageReceivedFromClient;
            client.LostConnection -= OnClientDisconnectedFromServer;
            if (ClientDisconnected != null)
                ClientDisconnected.BeginInvoke(this, client.Address, null, null);
            Clients.Remove(client);
        }
        #endregion

        #region Helper Methods
        private void ConnectionLoop()
        {
            // Wait for program to stop or client to have connected
            while (Run)
            {
                if (!Waiting)
                {
                    lock (_serverLock)
                    {
                        _server.BeginAcceptSocket(OnClientConnected, null);
                    }
                    Waiting = true;
                }
                System.Threading.Thread.Sleep(1000);
            }
        }
        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return string.Empty;
        }
        #endregion
    }
}
