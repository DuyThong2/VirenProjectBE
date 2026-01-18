using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Repositories.Enums
{
    public enum OutboxStatus
    {
        Pending = 0,
        Processing = 1,   
        Published = 2,
        Failed = 3
    }

}
