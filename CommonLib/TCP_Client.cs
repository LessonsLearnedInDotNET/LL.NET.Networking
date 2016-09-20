using log4net;
using System;
using System.Net.Sockets;
using System.Reflection;

namespace CommonLib
{
    public class TCP_Client : TCP_Base_Client
    {
        #region Variables
        private string _lastIPAddress;
        private int _lastPort;
        #endregion

        #region Properties
        public bool Connected
        {
            get { return IsRunning; }
        }
        #endregion

        #region Events
        public event EventHandler ConnectedToServer;
        #endregion

        #region Constructor(s)
        public TCP_Client() : base()
        {
            _client = new TcpClient();
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion

        #region User Methods
        /// <summary>
        /// Connect to TCP server at specified address and port
        /// </summary>
        /// <param name="ip_address">The IPV4 address to connect to</param>
        /// <param name="port">The port to connect to</param>
        public void Connect(string ip_address, int port)
        {
            // Make sure we're not already running
            if (Run)
                return;

            Run = true;
            _lastIPAddress = ip_address;
            _lastPort = port;
            TcpClient client = new TcpClient();
            client.BeginConnect(ip_address, port, OnClientConnected, client);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Complete the asynchronous connect
        /// </summary>
        /// <param name="ar">Unused parameter (Set to null)</param>
        private void OnClientConnected(IAsyncResult ar)
        {
            // Get the client that triggered the event
            TcpClient client = (TcpClient)ar.AsyncState;
            try
            {
                // Finish connecting and trigger events
                client.EndConnect(ar);
                if (ConnectedToServer != null)
                    ConnectedToServer.BeginInvoke(this, null, null, null);

                // Say what's going on
                _logger.Debug("Connected to server!");

                // Start listening loop
                _client = client;
                _stream = _client.GetStream();
                System.Threading.Thread commThread = new System.Threading.Thread(CommunicationLoop);
                commThread.Start();
            }
            catch (Exception)
            {
                // Make sure the client is disposed of
                client.Close();
                client = null;

                // Make sure we're still trying to connect
                if (!Run)
                    return;

                // Wait and attempt to reconnect
                System.Threading.Thread.Sleep(500);
                TcpClient anotherClient = new TcpClient();
                anotherClient.BeginConnect(_lastIPAddress, _lastPort, OnClientConnected, anotherClient);
            }
        }
        #endregion

        #region Helper Methods
        protected override void OnReceiveError()
        {
            // Notify that we have disconnected
            InvokeLostConnection(this, new EventArgs());

            // Say what's going on
            _logger.Debug(
                "Problem receiving server message, " +
                "shutting down client and starting reconnection loop.");

            // Shutdown and try to reconnect
            _stream.Close();
            _client.Close();
            TcpClient client = new TcpClient();
            client.BeginConnect(_lastIPAddress, _lastPort, OnClientConnected, client);
        }
        protected override void OnSendError()
        {
            // Notify that we have disconnected
            InvokeLostConnection(this, new EventArgs());

            // Say what's going on
            _logger.Debug("Problem sending message to server. " +
                "Shutting down client and starting reconnection loop.");

            // Shutdown and try to reconnect
            Shutdown();
            TcpClient client = new TcpClient();
            Run = true;
            client.BeginConnect(_lastIPAddress, _lastPort, OnClientConnected, client);
        }
        #endregion
    }
}
