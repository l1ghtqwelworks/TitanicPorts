using ShipwreckLib;

namespace Titanic
{
    public class TitanicConsoleCommandManager : L1ghtUtils.General.CommandManager
    {
        [Command("get")]
        public async Task WritePorts(string? description, int startPort = -1, int endPort= -1, string protocolStr = "null")
        {
            switch (description)
            {
                case "all":
                    {
                        Console.WriteLine("All ports:");
                        Console.WriteLine(await Port.Map.GetPorts());
                    }
                    break;
                case "open":
                    {
                        Console.WriteLine("Open ports:");
                        Console.WriteLine(await Port.Map.GetOpenPorts());
                    }
                    break;
                case "custom":
                    {
                        Console.WriteLine("Custom ports:");
                        Console.WriteLine(Port.Map.GetCustomPorts());
                    }
                    break;
                default:
                    {
                        var port = Port.Map.GetCustomPort(
                            description == "null" ? null : description, 
                            startPort == -1 ? null : startPort, 
                            endPort == -1 ? null : endPort,
                            protocolStr == "null" ? null : Port.ProtocolParse(protocolStr));
                        if (port == null)
                            Console.WriteLine("No such port found");
                        else
                            Console.WriteLine("Found port: " + port);
                    }
                    break;
            }
        }

        [Command("create")]
        public void CreatePort(string description, int startPort, int endPort, string protocolStr)
        {
            var protocol = Port.ProtocolParse(protocolStr);
            var port = Port.Map.AddMapping(description, startPort, endPort, protocol);
            Console.WriteLine("Created port: " + port);
        }

        [Command("remove")]
        public void RemovePort(string? description, int startPort = -1, int endPort = -1, string protocolStr = "null")
        {
            var port = Port.Map.RemoveMapping(
                            description == "null" ? null : description,
                            startPort == -1 ? null : startPort,
                            endPort == -1 ? null : endPort,
                            protocolStr == "null" ? null : Port.ProtocolParse(protocolStr));
            if (port == null)
                Console.WriteLine("No such port found");
            else
                Console.WriteLine("Removed port: " + port);
        }

        async Task<Boolean> TryInitDevices()
        {
            if (Port.Map.IsInitialized)
                return true;

            Console.WriteLine("Initializing devices...");

            for(int i = 0; i < 12; i++)
            {
                try
                {
                    await Port.Map.InitializeAsync();
                    Console.WriteLine("Initalized");
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed attempt: " + (i + 1) + " due to error: ", e, "retrying...");
                }
            }
            Console.WriteLine("Initalize failed");
            return false;
            //int timeout = 12;
            //startpoint:
            //try
            //{
            //    await port.map.initializeasync();
            //}
            //catch (exception e)
            //{
            //    console.writeline(e);
            //    timeout--;
            //    if (timeout > 0) goto startpoint;
            //    else
            //    {
            //        console.writeline("failed");
            //        return;
            //    }
            //}
            //console.writeline("started");
        }

        [Command("open")]
        public async Task OpenPorts()
        {
            if(await TryInitDevices())
            {
                Console.WriteLine("Opening ports...");
                await Port.Map.OpenPorts();
            }
        }

        [Command("close")]
        public async Task ClosePorts()
        {
            if(await TryInitDevices())
            {
                Console.WriteLine("Closing ports...");
                await Port.Map.ClosePorts();
            }
        }
    }
}
