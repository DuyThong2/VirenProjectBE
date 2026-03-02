using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Services.Configs
{
    public interface IMessageBrokerSettings
    {
        string Host { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
    }


}
