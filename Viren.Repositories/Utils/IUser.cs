using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Repositories.Utils
{
    public interface IUser
    {
        string? Id { get; }
        string? Email { get; }
        IEnumerable<string> Roles { get; }
    }
}
