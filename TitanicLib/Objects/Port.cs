using Open.Nat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
            return new Mapping(protocol, ip, privateport, publicport, 0, description);
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

        public readonly Mapping Mapping;

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

        public override string ToString() => Mapping.ToString();

        public static class Map
        {
            public const int DefaultTimeout = 10000;
            public static string HostIp = "8.8.8.8";
            public static int HostPort = 65530;
            public static String CustomPortsPath = @"C:\Users\Light\source\repos\Titanic\TitanicPorts.pdat";
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

            public static Port AddMapping(String description, int startPort, int endPort, Protocol protocol)
            {
                return AddMapping(new Mapping(protocol, startPort, endPort, CustomPortDescription.MarkCustom(description, protocol)));
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
                if(!File.Exists(CustomPortsPath))
                {
                    var result = new PortRange(0);
                    SaveCustomPorts(result);
                    return result;
                }
                return PortRange.Parse(File.ReadAllText(CustomPortsPath));
            }

            public static void SaveCustomPorts(PortRange ports)
            {
                File.WriteAllText(CustomPortsPath, ports.ToString());
            }


            public static async Task<PortRange> GetForwardedPorts()
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
            public static Port GetPort(Func<Port, bool> matchs)
            {
                var customs = GetCustomPorts();
                for(int i = 0; i < customs.Count; i++)
                {
                    var port = customs[i].Port;
                    if (matchs.Invoke(port)) return port;
                }
                return null;
            }

            public static async Task Forward()
            {
                var range = await GetForwardedPorts();
                var customs = GetCustomPorts();
                for(int i = 0; i < customs.Length; )
                {
                    var cur = customs[i];
                    if (range.Overlaps(cur) == -1)
                    {
                        Console.WriteLine("Opening port: " + cur);
                        using (var task = Device.CreatePortMapAsync(cur.Port.Mapping)) await task;
                    }
                    i += cur.Length + 1;
                }
            }

            static async Task Forward(PortSpan span, int index)
            {
                var customs = GetCustomPorts();
                for(int i = index + 1, length = index + span.Length; i <= length;)
                {
                    var cur = customs[i];
                    if (!cur.Port.Overlaps(span.Port))
                        using (var task = Forward(cur, i)) await task;
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

            static IPAddress GetLocalAddress()
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect(HostIp, HostPort);
                    return ((IPEndPoint)socket.LocalEndPoint).Address;
                }
            }
        }
    }
}
