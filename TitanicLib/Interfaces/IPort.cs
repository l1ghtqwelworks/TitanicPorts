using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ShipwreckLib
{
    public interface IPort
    {
        int PortStart { get; }
        int PortEnd { get; }
    }
}
