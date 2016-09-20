using CommonLib;
using System;
using System.Text;

namespace TestClient
{
    public class ClientRunner
    {
        private Command_Handler _handler;
        private TCP_Client _client;

        #region Constructor
        public ClientRunner()
        {
            // Initialize variables
            _handler = new Command_Handler();
            _client = new TCP_Client();

            // Wire up events
            _handler.Closing += _handler_Closing;
            _client.ConnectedToServer += _client_ConnectedToServer;
            _client.LostConnection += _client_DisconnectedFromServer;
            _client.MessageReceived += _client_MessageReceived;

            // Register commands
            _handler.RegisterCommand("connect", ConnectCommand, "Connect to server at specified address and port");
            _handler.RegisterCommand("disconnect", DisconnectCommand, "Disconnect from the server");
            _handler.RegisterCommand("send", SendCommand, "Send specified messages to server");
        }
        #endregion

        #region Event Handlers
        private void _client_MessageReceived(object sender, TCP_Message e)
        {
            Console.Write("Received ");
            Command_Handler.WriteColor(Encoding.ASCII.GetString(e.Message, 0, e.Length), ConsoleColor.Blue);
            Console.WriteLine(" from server.");
            _handler.PrintPrompt();
        }
        private void _client_DisconnectedFromServer(object sender, EventArgs e)
        {
            Command_Handler.WriteColorLine("Disconnected from server!", ConsoleColor.Red);
            _handler.PrintPrompt();
        }
        private void _client_ConnectedToServer(object sender, EventArgs e)
        {
            Command_Handler.WriteColorLine("Connected to server!", ConsoleColor.Green);
            _handler.PrintPrompt();
        }
        private void _handler_Closing(object sender, EventArgs e)
        {
            _client.Shutdown();
        }
        #endregion

        #region User Methods
        public void Start()
        {
            _handler.Start();
        }
        #endregion

        #region Command Definitions
        private void ConnectCommand(params string[] args)
        {
            // Check arguments
            if (args.Length != 2)
                throw new Exception("Must specify a port and ip address");

            // Extract arguments
            string address = args[0];
            int port = Convert.ToInt32(args[1]);

            // Attempt to connect to server
            _client.Connect(address, port);
        }
        private void DisconnectCommand(params string[] args)
        {
            _client.Shutdown();
        }
        private void SendCommand(params string[] args)
        {
            // Make sure we're connected
            if (!_client.Connected)
                throw new Exception("Can't send anything unless client is connected");

            // Send arguments in succession
            foreach (string message in args)
            {
                _client.Send(Encoding.ASCII.GetBytes(message));
            }
        }
        #endregion
    }
}
