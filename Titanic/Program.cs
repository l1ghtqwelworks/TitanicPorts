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
            ExecuteArgs(args).Wait();
        }

        static async Task Startup()
        {
            int timeout = 12;
            startpoint:
            try
            {
                await Port.Map.InitializeAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                timeout--;
                if (timeout > 0) goto startpoint;
                else
                {
                    Console.WriteLine("Failed");
                    return;
                }
            }
            Console.WriteLine("Started");
        }

        static async Task ExecuteArgs(string[] args)
        {
            Console.WriteLine("Sailing Titanic...");

            await Startup();

            string command = args[0];
            switch (command)
            {
                case "get":
                    {
                        string target = args[1];
                        switch (target)
                        {
                            case "all":
                                {
                                    Console.WriteLine("All ports:");
                                    Console.WriteLine((await Port.Map.GetPorts()).ToString());
                                }
                                break;
                        }
                    }
                    break;
                case "add":
                    {
                        var description = args[1];
                        var startPort = int.Parse(args[2]);
                        var endPort = int.Parse(args[3]);
                        var protocol = Port.ProtocolParse(args[4]);
                        string marked = CustomPortDescription.MarkCustom(description, protocol);
                        Console.WriteLine("marked: " + marked);
                        Console.WriteLine("valid: " + CustomPortDescription.IsValidCustomName(marked, protocol));
                        string unmarked = CustomPortDescription.UnmarkCustom(marked);
                        Console.WriteLine("unmarked: " + unmarked);
                        Console.WriteLine("valid: " + CustomPortDescription.IsValidCustomName(unmarked, protocol));
                        Port.Map.AddMapping(description, startPort, endPort, protocol);
                    }
                    break;
                case "forward":
                    {
                        Console.WriteLine("Forwarding");
                        await Port.Map.Forward();
                        Console.WriteLine("Forwarded");
                    }
                    break;
            }
        }
    }
}