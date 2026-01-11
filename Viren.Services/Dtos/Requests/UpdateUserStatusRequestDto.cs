using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests
{
    public class UpdateUserStatusRequestDto
    {
        public CommonStatus Status { get; set; }
    }
}
