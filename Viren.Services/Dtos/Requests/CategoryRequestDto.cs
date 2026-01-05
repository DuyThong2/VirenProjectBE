using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests
{
    public class CategoryRequestDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string Name { get; set; } = string.Empty;
        public string? Thumbnail { get; set; }
        public string? Header { get; set; }
        public string? Description { get; set; }
        public CommonStatus Status { get; set; }
    }
}
