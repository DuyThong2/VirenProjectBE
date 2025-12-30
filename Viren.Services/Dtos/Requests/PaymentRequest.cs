using System.ComponentModel.DataAnnotations;

namespace Viren.Services.Dtos.Requests;

public class PaymentRequest
{
    [Required(ErrorMessage = "Vui lòng nhập OrderId!")]
    public Guid OrderId { get; set; }
}