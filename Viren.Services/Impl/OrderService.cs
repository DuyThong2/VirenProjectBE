using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl;

public class OrderService : IOrderService
{
    
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private readonly UserManager<User> _userManager;
    
    
    
}