using log4net;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace CommonLib
{
    public class TCP_Server_Client : TCP_Base_Client
    {
        #region Constructor
        public TCP_Server_Client() : base()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion

        #region User Methods
        /// <summary>
        /// Start communication with the client
        /// </summary>
        /// <param name="client">The TCP client acquired from the server's TCP listener</param>
        public void Connect(TcpClient client)
        {
            // Say what's going on
            _logger.Debug("Connecting client to server.");

            // Prepare the client
            _client = client;
            _stream = client.GetStream();
            Address = ((IPEndPoint)_client.Client.RemoteEndPoint).Address.ToString();

            // Begin communication loop
            Run = true;
            System.Threading.Thread commThread = new System.Threading.Thread(CommunicationLoop);
            commThread.Start();
        }

        #endregion
    }
}
