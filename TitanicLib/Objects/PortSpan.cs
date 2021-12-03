using System;
using System.Collections.Generic;
using System.Text;

namespace ShipwreckLib
{
    public class PortSpan : IPort
    {
        public readonly Port Port;
        public int Length { get; internal set; } = 0;

        public int PortEnd { get; private set; }
        public int PortStart { get; private set; }

        public PortSpan(Port Port)
        {
            this.Port = Port;
            PortEnd = Port.PortEnd;
            PortStart = Port.PortStart;
        }

        internal void Stretch(PortSpan span)
        {
            if (span.PortStart < PortStart) PortStart = span.PortStart;
            if (span.PortEnd > PortEnd) PortEnd = span.PortEnd;
        }

        public bool Contains(IPort value)
            => PortStart <= value.PortStart && PortEnd >= value.PortEnd;

        public bool Overlaps(IPort value)
            => PortEnd >= value.PortStart && PortStart <= value.PortEnd;

        public override string ToString() => Length + " " + PortEnd + "-" + PortStart + " " + Port.ToString();

        // 6 25568-25560 Tcp 25568 --> 255.255.255.255:25560 (xd)
        public static PortSpan Parse(string value)
        {
            int index = value.IndexOf(' '),
                length = int.Parse(value.Substring(0, index)),
                next = value.IndexOf('-', index += 1),
                portend = int.Parse(value.Substring(index, next - index));
            next = value.IndexOf(' ', index = next + 1);
            var portstart = int.Parse(value.Substring(index, next - index));
            return new PortSpan(Port.Parse(value.Substring(next + 1)))
            {
                PortStart = portstart,
                PortEnd = portend,
                Length = length
            };
        }

        public static PortSpan Copy(PortSpan value)
            => new PortSpan(value.Port)
            {
                Length = value.Length,
                PortStart = value.PortStart,
                PortEnd = value.PortEnd
            };
    }
}
