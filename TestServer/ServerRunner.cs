using CommonLib;
using System;
using System.Text;

namespace TestServer
{
    public class ServerRunner
    {
        private TCP_Server _server;
        private Command_Handler _handler;

        #region Constructor
        public ServerRunner()
        {
            // Initialize variables
            _server = new TCP_Server();
            _handler = new Command_Handler();

            // Wire events
            _server.MessageReceived += OnServerMessageReceived;
            _server.ClientConnected += OnServerClientConnected;
            _server.ClientDisconnected += OnServerClientDisconnected;
            _handler.Closing += OnHandlerClosing;

            // Register commands
            _handler.RegisterCommand("start", StartCommand, "Starts a server at specified port");
            _handler.RegisterCommand("stop", StopCommand, "Stops the server communication");
            _handler.RegisterCommand("send", SendCommand, "Sends the specified messages to all connected clients");
            _handler.RegisterCommand("info", AddressCommand, "Get the IP address of our server");
        }
        #endregion

        #region Command Definitions
        private void StartCommand(params string[] args)
        {
            // Get the port
            if (args.Length == 0)
                throw new Exception("Must specify a port number");
            int port = Convert.ToInt32(args[0]);

            // Start the server
            _server.Start(port);

            // Say that we've started the server
            Console.Write("Starting server at ");
            Command_Handler.WriteColor(_server.Address, ConsoleColor.Yellow);
            Console.Write(':');
            Command_Handler.WriteColorLine(args[0], ConsoleColor.DarkYellow);
        }
        private void StopCommand(params string[] args)
        {
            // Stop the server
            _server.Shutdown();
        }
        private void SendCommand(params string[] args)
        {
            if (_server.ActiveConnections < 1)
                throw new Exception("There aren't any connected clients to send information to!");
            if (!_server.Running)
                throw new Exception("Server isn't running!");

            foreach (string message in args)
            {
                _server.SendAll(Encoding.ASCII.GetBytes(message));
            }
        }
        private void AddressCommand(params string[] args)
        {
            if (!_server.Running)
                throw new Exception("Server isn't running! No address to retrieve!");

            Console.Write("The server is running at ");
            Command_Handler.WriteColorLine(_server.Address, ConsoleColor.Yellow);
        }
        #endregion

        #region Event Handlers
        private void OnServerClientDisconnected(object sender, string e)
        {
            Command_Handler.WriteColorLine("Client at {0} disconnected from server!", ConsoleColor.Yellow, e);
            _handler.PrintPrompt();
        }
        private void OnServerClientConnected(object sender, TCP_Server_Client e)
        {
            Command_Handler.WriteColor(e.Address, ConsoleColor.Cyan);
            Console.Write(':');
            Command_Handler.WriteColor(e.ID.ToString(), ConsoleColor.DarkCyan);
            Console.Write(" has connected to the server!");
            _handler.PrintPrompt();
        }
        private void OnServerMessageReceived(object sender, TCP_Message e)
        {
            Console.Write("Received ");
            Command_Handler.WriteColor(Encoding.ASCII.GetString(e.Message, 0, e.Length), ConsoleColor.Blue);
            Console.Write(" from ");
            Command_Handler.WriteColorLine(e.Address, ConsoleColor.Cyan);
            _handler.PrintPrompt();
        }
        private void OnHandlerClosing(object sender, EventArgs e)
        {
            _server.Shutdown();
        }
        #endregion

        #region User Methods
        public void Start()
        {
            _handler.Start();
        }
        #endregion
    }
}
