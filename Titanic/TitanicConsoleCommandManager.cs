using Open.Nat;
using ShipwreckLib;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Titanic
{
    public class TitanicConsoleCommandManager : L1ghtUtils.General.CommandManager
    {
        public static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException!;
            }

            return stringBuilder.ToString();
        }

        [Command("get")]
        public async Task WritePorts(string? description, int startPort = -1, int endPort= -1, string protocolStr = "null")
        {
            switch (description)
            {
                case "all":
                    {
                        if(await TryInitDevices())
                        {
                            Console.WriteLine("All ports:");
                            Console.WriteLine(await Port.Map.GetPorts());
                        }
                    }
                    break;
                case "open":
                    {
                        if(await TryInitDevices())
                        {
                            Console.WriteLine("Open ports:");
                            Console.WriteLine(await Port.Map.GetOpenPorts());
                        }
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

        async Task<Boolean> TryInitDevices(int retrys = 12)
        {
            Console.WriteLine("Initializing devices...");

            for(int i = 0; i < retrys; i++)
            {
                try
                {
                    await Port.Map.InitializeAsync();
                    Console.WriteLine("Initialized");
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed attempt: " + (i + 1) + " due to error: " + FlattenException(e) + (i + 1 < retrys ? Environment.NewLine + "retrying..." : ""));
                }
            }
            Console.WriteLine("Initialize failed");
            return false;
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

        [Command("ip")]
        public void GetIP()
        {
            Console.WriteLine(Port.Map.GetLocalAddress().ToString());
        }

        private DelayedActionScheduler delayedActionScheduler = new DelayedActionScheduler();
        private System.Timers.Timer resetTimeoutTimer;

        /// <summary>
        /// runs open command every time IPAdress changes
        /// and prevents timeout
        /// </summary>
        [Command("openloop")]
        public void FollowChanges(int intervalMilliseconds = 0)
        {
            NetworkChange.NetworkAddressChanged += OpenLoop_NetworkAddressChanged;
            Console.WriteLine("Input exit in order to stop");
            resetTimeoutTimer = new System.Timers.Timer();
            if(intervalMilliseconds == 0)
                resetTimeoutTimer.Interval = (Port.Map.DefaultPortTimeout * 1000) / 2;
            else
                resetTimeoutTimer.Interval = intervalMilliseconds;
            resetTimeoutTimer.Elapsed += ResetPortsTimeout;
            resetTimeoutTimer.AutoReset = true;
            resetTimeoutTimer.Start();
            var command = Console.ReadLine();
            while (command != "exit")
                command = Console.ReadLine();
            resetTimeoutTimer.Stop();
            resetTimeoutTimer.Dispose();
            NetworkChange.NetworkAddressChanged -= OpenLoop_NetworkAddressChanged;

        }

        private async void ResetPortsTimeout(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Reseting ports timeout");
            if(await TryInitDevices())
            {
                await Port.Map.ResetPortsTimeout();
            }
            Console.WriteLine("Reset");
        }

        private IPAddress? lastIPAddress = null;

        private void OpenLoop_NetworkAddressChanged(object? sender, EventArgs e)
        {
            delayedActionScheduler.ScheduleAction(() =>
            {
                OpenLoop_NetworkAddressChangedAsync(sender, e).Wait();
            });
        }

        private async Task OpenLoop_NetworkAddressChangedAsync(object? sender, EventArgs e)
        {

            try
            {
                var ip = Port.Map.GetLocalAddress();
                if (lastIPAddress == null || ip.ToString() != lastIPAddress.ToString())
                {
                    Console.WriteLine("Netwrok address changed to: " + ip + " from: " + (lastIPAddress == null ? "null" : lastIPAddress));
                    if (await this.TryInitDevices(1))
                    {
                        await Port.Map.OpenPorts();
                        lastIPAddress = ip;
                    }
                    else
                        throw new Exception("Failed to init devices (likely due to no internet connection)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught on network address change: " + FlattenException(ex));
                lastIPAddress = null;
            }
        }

        [Command("timeout")]
        public async Task GetTimeout(bool reset = false)
        {
            int customCount = 0;
            var result = new StringBuilder();
            if(await this.TryInitDevices())
            {
                if(reset)
                {
                    await Port.Map.ResetPortsTimeout();
                }
                var openPorts = await Port.Map.GetOpenPorts();
                for (int i = 0; i < openPorts.Length; i++)
                {
                    var cur = openPorts[i].Port;
                    if (cur.Custom)
                    {
                        customCount++;
                        result.AppendLine(cur.ToString() + " " + cur.Mapping.Lifetime + " " + cur.Mapping.Expiration);
                    }
                }
            }
            Console.WriteLine(customCount + Environment.NewLine + result.ToString());
        }
    }
}
