using System.ComponentModel.DataAnnotations;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests;

public sealed class UpdateOrderStatusRequestDto
{
    [Required(ErrorMessage = "Vui lòng chọn trạng thái đơn hàng!")]
    public OrderStatus TargetStatus { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại thanh toán!")]
    public PaymentType PaymentType { get; set; }
}
