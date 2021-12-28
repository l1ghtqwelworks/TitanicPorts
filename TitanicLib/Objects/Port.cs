using Open.Nat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TitanicLib.Objects;

namespace ShipwreckLib
{
    public class Port : IPort
    {
        public delegate void ChangedDel(Port sender);
        public static event ChangedDel Added;
        public static event ChangedDel Removed;
        public static event ChangedDel Modified;

        // Tcp 25566 --> 10.0.0.1:25544 (Xd)
        public static Mapping MappingParse(string value)
        {
            var index = value.IndexOf(' ', 0);
            var protocol = ProtocolParse(value.Substring(0, index));
            int next = value.IndexOf(' ', index += 1),
                publicport = int.Parse(value.Substring(index, next - index));
            next = value.IndexOf(':', index = next + 5);
            var ip = IPAddress.Parse(value.Substring(index, next - index));
            next = value.IndexOf(' ', index = next + 1);
            var privateport = int.Parse(value.Substring(index, next - index));
            var description = value.Substring(next += 2, value.Length - next - 1);
            return new Mapping(protocol, ip, privateport, publicport, Map.DefaultPortTimeout, description);
        }
        public static Port Parse(string value) => new Port(MappingParse(value));

        public static Protocol ProtocolParse(string value)
        {
            var lowerValue = value.ToLower();
            if (lowerValue == Protocol.Udp.ToString().ToLower()) return Protocol.Udp;
            else if (lowerValue == Protocol.Tcp.ToString().ToLower()) return Protocol.Tcp;
            throw new Exception("No such protocol as: " + value);
        }

        public static bool isValidProtocol(string value)
        {
            try
            {
                ProtocolParse(value);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        //public static bool TryCustomParse(string description, out string result, out int protocol)
        //{
        //    int index;
        //    if ((index = description.IndexOf(StartKey)) != -1)
        //    {
        //        index += StartKey.Length;
        //        protocol = int.Parse(description[index].ToString());
        //        index += 3;
        //        result = description.Substring(index, description.LastIndexOf(EndChar) - index);
        //        return true;
        //    }
        //    else
        //    {
        //        protocol = -1;
        //        result = description;
        //        return false;
        //    }
        //}

        public Mapping Mapping { get; private set; }

        public Protocol Protocol => Mapping.Protocol;
        public int Lifetime => Mapping.Lifetime;
        public int PortEnd => Mapping.PublicPort;
        public int PortStart => Mapping.PrivatePort;
        public IPAddress PrivateIP => Mapping.PrivateIP;
        public readonly string Description;

        public readonly bool Custom = false;
        public readonly int Coverage;

        internal Port(Mapping Mapping)
        {
            this.Mapping = Mapping;
            if (Custom = CustomPortDescription.IsValidCustomName(Mapping.Description, Mapping.Protocol))
                Description = CustomPortDescription.UnmarkCustom(Mapping.Description);
            else
                Description = Mapping.Description;
            Coverage = PortEnd - PortStart;
        }

        public bool Contains(IPort value)
            => PortStart <= value.PortStart && PortEnd >= value.PortEnd;

        public bool Overlaps(IPort value)
            => PortEnd >= value.PortStart && PortStart <= value.PortEnd;

        public bool Matchs(Port value)
            =>
            value.Mapping.Description == this.Mapping.Description &&
            value.Mapping.PublicPort == this.Mapping.PublicPort &&
            value.Mapping.PrivatePort == this.Mapping.PrivatePort &&
            value.Mapping.Protocol == this.Mapping.Protocol;

        public override string ToString() => Mapping.ToString();

        public static class Map
        {
            public const int DefaultTimeout = 10000;
            public static string HostIp = "8.8.8.8";
            public static int HostPort = 65530;
            public const int DefaultPortTimeout = 86400;
            public static String GetCustomPortsPath()
            { 
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "TitanicPorts.pdat");
            }
            public static bool IsInitialized { get; private set; } = false;
            public static IPAddress IpV4 { get; private set; }
            public static NatDevice Device { get; private set; }
            public static async Task InitializeAsync(int timeout = DefaultTimeout)
            {
                using (var task = SetupDevices(timeout)) await task;
                IsInitialized = true;
            }
            public static async Task SetupDevices(int timeout = DefaultTimeout)
            {
                IpV4 = GetLocalAddress();
                var discoverer = new NatDiscoverer();
                using (var cts = new CancellationTokenSource(timeout))
                    Device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            }

            public static Port? GetCustomPort(string? description, int? startPort, int? endPort, Protocol? protocol)
            {
                var customs = GetCustomPorts();
                return customs.Find((value) => MatchPort(description, startPort, endPort, protocol, value));
            }

            public static Port? RemoveMapping(string? description, int? startPort, int? endPort, Protocol? protocol)
            {
                var customs = GetCustomPorts();
                var targetIndex = customs.FindIndex((value) => MatchPort(description, startPort, endPort, protocol, value));
                if (targetIndex == -1)
                    return null;
                var port = customs[targetIndex].Port;
                SaveCustomPorts(customs.Remove(targetIndex));
                return port;
            }

            public static bool MatchPort(string? description, int? startPort, int? endPort, Protocol? protocol, Port value)
            {
                return
                    (description == null || value.Description == description) &&
                    (startPort == null || value.PortStart == startPort) &&
                    (endPort == null || value.PortEnd == endPort) &&
                    (protocol == null || value.Protocol == protocol);
            }

            public static Port AddMapping(string description, int startPort, int endPort, Protocol protocol)
            {
                return AddMapping(new Mapping(protocol, startPort, endPort, DefaultPortTimeout, CustomPortDescription.MarkCustom(description, protocol)));
            }
            internal static Port AddMapping(Mapping mapping)
            {
                var port = new Port(mapping);
                var customs = GetCustomPorts();
                //int currentIndex;
                //if ((currentIndex = customs.IndexOf(mapping.Description)) != -1)
                //    throw new Exception("Mapping description already in use by: " + customs[currentIndex]);
                var result = new PortRange(customs.Count + 1);
                PortRange.CopyTo(customs, result);
                result.Add(port);
                SaveCustomPorts(result);
                return port;
            }

            public static PortRange GetCustomPorts()
            {
                if(!File.Exists(GetCustomPortsPath()))
                {
                    var result = new PortRange(0);
                    SaveCustomPorts(result);
                    return result;
                }
                return PortRange.Parse(File.ReadAllText(GetCustomPortsPath()));
            }

            public static void SaveCustomPorts(PortRange ports)
            {
                File.WriteAllText(GetCustomPortsPath(), ports.ToString());
            }

            public static async Task ResetPortsTimeout()
            {
                using(var openPortTask = GetOpenPorts())
                {
                    using(var cleanPortTask = CleanUnusedCustomPorts(await openPortTask, GetCustomPorts()))
                    {
                        var range = await cleanPortTask;
                        for (int i = 0; i < range.Length; i++)
                        {
                            var cur = range[i].Port;
                            if (cur.Custom)
                            {
                                using (var task = ResetPortTimeout(cur)) await task;
                            }
                        }
                    }
                }
            }

            public static async Task ResetPortTimeout(Port port)
            {
                using (var task = Device.DeletePortMapAsync(port.Mapping)) await task;
                port.Mapping = new Mapping(port.Protocol, port.Mapping.PrivatePort, port.Mapping.PublicPort, DefaultPortTimeout, port.Mapping.Description);
                using (var task = Device.CreatePortMapAsync(port.Mapping)) await task;
            }

            public static async Task<PortRange> GetOpenPorts()
            {
                using (var task = Device.GetAllMappingsAsync()) return PortRange.From(await task, out _);
            }
            public static async Task<PortRange> GetPorts()
            {
                int count = 0;
                Port[] open;
                using (var task = Device.GetAllMappingsAsync())
                {
                    var mappings = await task;
                    open = new Port[mappings.Count()];
                    foreach (var mapping in mappings)
                    {
                        var cur = new Port(mapping);
                        if (!cur.Custom) open[count++] = cur;
                    }
                }
                var customs = GetCustomPorts();
                var result = new PortRange(count + customs.Count);
                PortRange.CopyTo(customs, result);
                for (int i = 0; i < count; i++)
                    result.Add(open[i]);
                return result;
            }
            public static Stack<Port> FilterPorts(IEnumerable<Mapping> mappings)
            {
                var result = new Stack<Port>();
                foreach (var mapping in mappings)
                {
                    var port = new Port(mapping);
                    if (!port.Custom) result.Push(port);
                }
                return result;
            }

            public static Task OpenPort(Port value) 
            {
                return Device.CreatePortMapAsync(EnforceMappingIp(value.Mapping, IpV4));
            }

            public static Mapping EnforceMappingIp(Mapping mapping, IPAddress ip)
            {
                return new Mapping(mapping.Protocol, ip, mapping.PrivatePort, mapping.PublicPort, mapping.Lifetime, mapping.Description);
            } 

            public static async Task OpenPorts()
            {
                var openPorts = await GetOpenPorts();
                var customs = GetCustomPorts();

                var range = await CleanUnusedCustomPorts(openPorts, customs);
                for (int i = 0; i < customs.Length; )
                {
                    var cur = customs[i];
                    if (range.Overlaps(cur) == -1)
                    {
                        Console.WriteLine("Opening port: " + cur);
                        using (var task = OpenPort(cur.Port)) await task;
                    }
                    i += cur.Length + 1;
                }
            }

            public static async Task<PortRange> CleanUnusedCustomPorts(PortRange openPorts, PortRange customPorts)
            {
                var result = openPorts;
                for(int i = 0; i < result.Count; i++)
                {
                    var cur = result[i].Port;
                    if(cur.Custom) {
                        string? ruleBroken = null;
                        if (customPorts.Find((value) => value.Matchs(cur)) == null)
                            ruleBroken = "Port not registered as a custom port in TitanicPorts.pdat";
                        if (cur.Mapping.PrivateIP.ToString() != IpV4.ToString())
                            ruleBroken = "Port ip no longer matches machine ip (" + IpV4 + ")";
                        if(ruleBroken != null)
                        {
                            Console.WriteLine("Clearing port: " + cur + " since: " + ruleBroken);
                            using (var task = Device.DeletePortMapAsync(cur.Mapping)) await task;
                            result = result.Remove(i);
                            i--;
                        }
                    }
                }
                return result;
            }

            public static async Task<PortRange> ClosePorts()
            {
                var result = await GetOpenPorts();
                for (int i = 0; i < result.Count; i++)
                {
                    var cur = result[i].Port;
                    if (cur.Custom)
                    {
                        using (var task = Device.DeletePortMapAsync(cur.Mapping)) await task;
                        result = result.Remove(i);
                        i--;
                    }
                }
                return result;
            }

            static async Task OpenPortsSpan(PortSpan span, int index)
            {
                var customs = GetCustomPorts();
                for(int i = index + 1, length = index + span.Length; i <= length;)
                {
                    var cur = customs[i];
                    if (!cur.Port.Overlaps(span.Port))
                        using (var task = OpenPortsSpan(cur, i)) await task;
                    i += cur.Length + 1;
                }
            }

            public static async Task<bool> IsOpen(Mapping value)
            {
                using (var task = Device.GetAllMappingsAsync())
                {
                    var mappings = await task;
                    foreach (var mapping in mappings) if (mapping == value) return true;
                }
                return false;
            }

            public static IPAddress GetLocalAddress()
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect(HostIp, HostPort);
                    return ((IPEndPoint)socket.LocalEndPoint!).Address;
                }
            }
        }
    }
}
