using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;

namespace Viren.Repositories.Interfaces
{
    public interface ITokenRepository
    {
        (string, int) GenerateJwtToken(User user, string role);

    }
}
