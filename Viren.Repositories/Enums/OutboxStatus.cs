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
        Published = 1,
        Failed = 2
    }
}
