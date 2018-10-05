using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCL
{
    namespace Network
    {
        public class Local
        {
            public static string GetIPV4Addrs()
            {
                string addrs = "";
                foreach (var addr in System.Net.Dns.GetHostEntry(string.Empty).AddressList)
                {
                    if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        addrs += addr + " ";
                }
                return addrs;
            }
        }
    }
}
