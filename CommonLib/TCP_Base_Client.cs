using log4net;
using System;
using System.Net.Sockets;

namespace CommonLib
{
    public class TCP_Base_Client
    {
        #region Variables
        protected ILog _logger;
        protected TcpClient _client;
        protected NetworkStream _stream;

        private bool _run;
        private bool _isRunning;
        private object _runLock;
        private object _isRunningLock;

        protected bool Run
        {
            get { lock (_runLock) { return _run; } }
            set { lock (_runLock) { _run = value; } }
        }
        protected bool IsRunning
        {
            get { lock (_isRunningLock) { return _isRunning; } }
            set { lock (_isRunningLock) { _isRunning = value; } }
        }
        #endregion

        #region Properties
        public string Address { get; protected set; }
        public Guid ID { get; protected set; }
        #endregion

        #region Events
        public event EventHandler<TCP_Message> MessageReceived;
        public event EventHandler LostConnection;
        #endregion

        #region Constructor
        public TCP_Base_Client()
        {
            _isRunning = false;
            _run = false;
            _runLock = new object();
            _isRunningLock = new object();
            ID = Guid.NewGuid();
        }
        #endregion

        #region User Methods
        /// <summary>
        /// Stop communication
        /// </summary>
        public void Shutdown()
        {
            // Only shutdown if we haven't already done so
            if (!Run)
                return;

            // Say what's going on
            _logger.Debug("Disconnecting client from server...");

            // Stop communication loop
            Run = false;
            while (IsRunning)
                System.Threading.Thread.Sleep(100);

            // Clear the variables
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
        }
        /// <summary>
        /// Send data through connection
        /// </summary>
        /// <param name="data">A byte array of data to send (4 bytes will be added
        /// to the front to specify array size)</param>
        public virtual void Send(byte[] data)
        {
            // Make sure that we're connected
            if (!IsRunning)
                return;

            // Format the message
            byte[] finalArray = new byte[data.Length + 4];
            byte[] sizeBytes = BitConverter.GetBytes(data.Length);
            Buffer.BlockCopy(sizeBytes, 0, finalArray, 0, 4);
            Buffer.BlockCopy(data, 0, finalArray, 4, data.Length);

            // Send the message
            try
            {
                _stream.Write(finalArray, 0, finalArray.Length);
            }
            catch (Exception)
            {
                OnSendError();
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Listen for incoming data from the client
        /// </summary>
        protected void CommunicationLoop()
        {
            // Notify that loop is running
            IsRunning = true;

            while (Run)
            {
                // Check if incoming data is available
                if (_client.Available > 0)
                {
                    try
                    {
                        ReceiveMessage();
                    }
                    catch (Exception)
                    {
                        OnReceiveError();

                        // Escape from the loop
                        break;
                    }
                }

                System.Threading.Thread.Sleep(100);
            }

            // Notify that we have exited loop
            IsRunning = false;
        }
        protected virtual void ReceiveMessage()
        {
            // Wait for all data to arrive
            System.Threading.Thread.Sleep(100);

            // Get and unpack the data
            byte[] dataSize = new byte[4];
            _stream.Read(dataSize, 0, 4);
            int size = BitConverter.ToInt32(dataSize, 0);
            byte[] payload = new byte[size];
            _stream.Read(payload, 0, size);

            // Trigger the received event
            if (MessageReceived != null)
                MessageReceived.BeginInvoke(this, new TCP_Message(payload, Address, ID), null, null);
        }
        protected virtual void OnReceiveError()
        {
            // Say what's going on
            _logger.Debug("Encountered problem when receiving message from client!" +
                "Closing client's connection to server.");

            // Notify that we have disconnected
            if (LostConnection != null)
                LostConnection.BeginInvoke(this, new EventArgs(), null, null);

            // Shutdown
            _stream.Close();
            _client.Close();
        }
        protected virtual void OnSendError()
        {
            // Say what's going on
            _logger.Debug("Server lost connection to client while trying" +
                " to send a message to the client.");

            // Notify that we have disconnected
            if (LostConnection != null)
                LostConnection.BeginInvoke(this, new EventArgs(), null, null);

            // Shutdown
            Shutdown();
        }
        protected void InvokeLostConnection(object obj, EventArgs e)
        {
            if (LostConnection != null)
                LostConnection.BeginInvoke(obj, e, null, null);
        }
        #endregion
    }
}
