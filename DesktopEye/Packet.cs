using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopEye
{
    public class Packet
    {
        public string Time = DateTime.UtcNow.Ticks.ToString();
        public string Category = string.Empty;
        public string Request = string.Empty;
        public string StatusCode = string.Empty;
        public string Values = string.Empty;
        public string RawData = string.Empty;

        public Packet() { }
    }
}
