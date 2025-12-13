using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Services.Configs
{
    public class PayOsSetings
    {
        public string ClientId { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ChecksumKey { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
        public long ExpirationSeconds { get; set; }
    }
}
