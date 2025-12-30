
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests;


//Add-Migration InitPayment -StartupProject Viren.API -Project Viren.Repositories
//Remove-Migration -Project Viren.Repositories -StartupProject Viren.API
//Update-Database -StartupProject Viren.API -Project Viren.Repositories

public class UserRequestDto
{
    public string? Name { get; set; } = null;

    public string? PhoneNumber { get; set; } = null;

    public bool? Gender { get; set; } 

    public DateTime? BirthDate { get; set; }

    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }

    public string? FirstName { get; set; } = null;
    public string? LastName { get; set; } = null;
    public string? Address { get; set; } = null;

    public CommonStatus? Status { get; set; }
}




