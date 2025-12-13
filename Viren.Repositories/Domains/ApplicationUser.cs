using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Repositories.Domains
{
    public class ApplicationUser : IdentityUser
    {
        public Guid UserId { get; set; }
        public string? FirstName { get; set; } = null;
        public string? LastName { get; set; } = null;
        public string? Address { get; set; } = null;

        //public DateTimeOffset PremiumUntil { get; set; } = DateTimeOffset.MinValue;
        
        public bool Status { get; set; }
    }
}
