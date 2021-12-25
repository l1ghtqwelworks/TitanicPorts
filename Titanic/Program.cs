﻿using Open.Nat;
using ShipwreckLib;
using System.Text;
using TitanicLib.Objects;

namespace Titanic
{
    class Program
    {
        static void Main(string[] args)
        {
            ExecuteArgs(String.Join(" ", args)).Wait();
        }

        static async Task ExecuteArgs(string args)
        {
            Console.WriteLine("Executing...");

            var commandManager = new TitanicConsoleCommandManager();
            var result = commandManager[command: args];
            if (result != null && result is Task)
                await (result as Task);

            //string command = args[0];
            //switch (command)
            //{
            //    case "get":
            //        {
            //            string target = args[1];
            //            switch (target)
            //            {
            //                case "all":
            //                    {
            //                        Console.WriteLine("All ports:");
            //                        Console.WriteLine(await Port.Map.GetPorts());
            //                    }
            //                    break;
            //                case "forwarded":
            //                    {
            //                        Console.WriteLine("Forwarded ports:");
            //                        Console.WriteLine(await Port.Map.GetForwardedPorts());
            //                    }
            //                    break;
            //                case "custom":
            //                    {
            //                        Console.WriteLine("Custom ports:");
            //                        Console.WriteLine(Port.Map.GetCustomPorts());
            //                    }
            //                    break;
            //            }
            //        }
            //        break;
            //    case "add":
            //        {
            //            var description = args[1];
            //            var startPort = int.Parse(args[2]);
            //            var endPort = int.Parse(args[3]);
            //            var protocol = Port.ProtocolParse(args[4]);
            //            Port.Map.AddMapping(description, startPort, endPort, protocol);
            //        }
            //        break;
            //    case "forward":
            //        {
            //            Console.WriteLine("Forwarding");
            //            await Port.Map.Forward();
            //            Console.WriteLine("Forwarded");
            //        }
            //        break;
            //}
        }
    }
}