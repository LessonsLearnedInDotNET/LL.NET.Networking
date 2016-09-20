using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace CommonLib
{
    public class Command_Handler
    {
        public delegate void CommandFunction(params string[] args);

        #region Variables
        private Dictionary<string, Tuple<CommandFunction, string>> _registeredCommands;
        public string Prompt;
        public static ConsoleColor BaseForegroundColor;
        #endregion

        #region Events
        public event EventHandler Closing;
        #endregion

        #region Constructor
        public Command_Handler()
        {
            _registeredCommands = new Dictionary<string, Tuple<CommandFunction, string>>();
            Prompt = "$: ";
            BaseForegroundColor = ConsoleColor.White;
        }
        #endregion

        #region User Methods
        /// <summary>
        /// Add a command to the list of available commands
        /// </summary>
        /// <param name="commandName">The name of the command (case insensitive)</param>
        /// <param name="funct">The function to execute when the command is entered</param>
        /// <param name="commandDesciption">A description of what the command does (optional)</param>
        public void RegisterCommand(string commandName, CommandFunction funct, string commandDesciption = "No command description available")
        {
            // Register all commands as upper case
            commandName = commandName.ToUpper();
            _registeredCommands.Add(commandName, Tuple.Create(funct, commandDesciption));
        }
        /// <summary>
        /// Begin parsing commands in a loop. Only stops when "EXIT" command is entered
        /// </summary>
        public void Start()
        {
            while (true)
            {
                // Print prompt
                WriteColor(Prompt, ConsoleColor.Cyan);

                // Get and format user input
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                string[] parts = ParseQuotes(input);
                string cmd = parts[0].ToUpper();
                string[] args = new string[parts.Length - 1];
                if (parts.Length > 1)
                    Array.Copy(parts, 1, args, 0, parts.Length - 1);

                // Interpret command
                Tuple<CommandFunction, string> cmdEntry;
                if (_registeredCommands.TryGetValue(cmd, out cmdEntry))
                {
                    try
                    {
                        cmdEntry.Item1.Invoke(args);
                    }
                    catch (Exception exp)
                    {
                        WriteColor("Ran into problem executing \"", ConsoleColor.Red);
                        Console.Write(cmd);
                        WriteColor("\"! Details below:\n{0}\n", ConsoleColor.Red, exp.Message);
                    }
                    continue;
                }
                if (cmd.Equals("HELP"))
                    ShowHelpText();
                else if (cmd.Equals("EXIT"))
                {
                    if (Closing != null)
                        Closing.Invoke(this, null);
                    break;
                }
                else
                {
                    Console.Write("Did not recognize command \"");
                    WriteColor(cmd, ConsoleColor.Red);
                    Console.Write("\".\nType \"");
                    WriteColor("HELP", ConsoleColor.Magenta);
                    Console.WriteLine("\" to show available commands.");
                }
            }
        }
        /// <summary>
        /// Clear the line and print the prompt
        /// </summary>
        public void PrintPrompt()
        {
            Console.WriteLine();
            WriteColor(Prompt, ConsoleColor.Cyan);
        }
        #endregion

        #region Helper Methods
        private void ShowHelpText()
        {
            // Show program name and version
            string name = Assembly.GetEntryAssembly().GetName().Name;
            string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            int letterNum = name.Length > version.Length ? name.Length : version.Length;
            string bufferchars = new string('-', letterNum);
            Console.WriteLine(bufferchars);
            WriteColorLine(name, ConsoleColor.Green);
            Console.WriteLine(version);
            Console.WriteLine("{0}\n", bufferchars);

            // Talk about help and exit commands
            WriteColor("HELP", ConsoleColor.Yellow);
            Console.WriteLine(": Displays this help text.");
            WriteColor("EXIT", ConsoleColor.Yellow);
            Console.WriteLine(": Quits the program.");
            Console.WriteLine();

            // Descibe other commands
            foreach (KeyValuePair<string, Tuple<CommandFunction, string>> cmdEntry in _registeredCommands)
            {
                WriteColor(cmdEntry.Key, ConsoleColor.Yellow);
                Console.WriteLine(": {0}", cmdEntry.Value.Item2);
            }

            Console.WriteLine();
        }
        private string[] ParseQuotes(string input)
        {
            // Remove all extra whitespace from string
            input = Regex.Replace(input, @"\s+", " ");
            List<string> newArgs = new List<string>();

            // Loop through args to search for quotes
            StringBuilder currentArg = new StringBuilder();
            bool inQuote = false;
            for (int i = 0; i < input.Length; i++)
            {
                // Check for quotes
                if (input[i] == '\"')
                {
                    // We've reached the end quote
                    if (inQuote)
                    {
                        newArgs.Add(currentArg.ToString());
                        currentArg.Clear();
                        inQuote = false;
                        continue;
                    }
                    // We're opening a quote
                    else
                    {
                        inQuote = true;
                        continue;
                    }
                }
                // Check for space breaks
                if (input[i] == ' ' && !inQuote)
                {
                    if (currentArg.Length > 0)
                        newArgs.Add(currentArg.ToString());
                    currentArg.Clear();
                    continue;
                }
                currentArg.Append(input[i]);
            }

            // Add the last word
            if (currentArg.Length > 0)
                newArgs.Add(currentArg.ToString());

            return newArgs.ToArray();
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Extract a dictionary of arguments sorted by switch.
        /// For example, if full command was "testcommand -s fast -t 0 -v true"
        /// Then the returned dictionary would be:
        /// 's':"fast"
        /// 't':"0"
        /// 'v':"true"
        /// </summary>
        /// <param name="args">The command arguments</param>
        /// <returns></returns>
        public static Dictionary<char, string> ExtractSwitchArgs(string[] args)
        {
            Dictionary<char, string> argDict = new Dictionary<char, string>();

            // Loop through args
            for (int i = 0; i < args.Length; i++)
            {
                // Check if argument is a switch
                if (args[i][0] != '-' || args[i].Length != 2)
                    continue;

                // No arguments left
                if (i + 1 >= args.Length)
                {
                    argDict.Add(args[i][1], string.Empty);
                }
                // Next argument is a switch
                else if (args[i + 1][0] == '-' && args[i + 1].Length == 2)
                {
                    argDict.Add(args[i][1], string.Empty);
                }
                // Next argument belongs with our switch
                else
                {
                    argDict.Add(args[i][1], args[i + 1]);
                    i++;
                }
            }

            return argDict;
        }
        /// <summary>
        /// Write a colored message to the console in the format of Console.Write()
        /// </summary>
        /// <param name="message">The message to write (can be formatted)</param>
        /// <param name="color">The color of the message</param>
        /// <param name="args">Arguments for the message if it's a formatted string</param>
        public static void WriteColor(string message, ConsoleColor color, params object[] args)
        {
            // Change console output color
            Console.ForegroundColor = color;

            // Write message
            Console.Write(message, args);

            // Restore console color
            Console.ForegroundColor = BaseForegroundColor;
        }
        /// <summary>
        /// Write a colored message to the console in the format of Console.WriteLine()
        /// </summary>
        /// <param name="message">The message to write (can be formatted)</param>
        /// <param name="color">The color of the message</param>
        /// <param name="args">Arguments for the message if it's a formatted string</param>
        public static void WriteColorLine(string message, ConsoleColor color, params object[] args)
        {
            // Change console output color
            Console.ForegroundColor = color;

            // Write message
            Console.WriteLine(message, args);

            // Restore console color
            Console.ForegroundColor = BaseForegroundColor;
        }
        #endregion
    }
}
