
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests;

public class UserRequestDto
{
    public string? Name { get; set; } = null;

    public string? PhoneNumber { get; set; } = null;

    public bool? Gender { get; set; } // nullable để "không gửi" thì không đổi

    public DateTime? BirthDate { get; set; }

    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }

    public string? FirstName { get; set; } = null;
    public string? LastName { get; set; } = null;
    public string? Address { get; set; } = null;

    public CommonStatus? Status { get; set; } // admin mới nên đổi status
}




