using System;
using System.Collections.Generic;
using System.Text;

using System;

namespace BigBrems.Models
{
    public class MeasurementData
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string ChannelName { get; set; } // <--- Was "Status", now "ChannelName"
        public string Unit { get; set; }
    }
}