﻿
using api_be.Common.Constants;
using api_be.Common.Interfaces;
using System.Security.Claims;


namespace api_be.DB.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

        public int? UserId
        {
            get
            {
                string userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(CONSTANT_CLAIM_TYPES.Uid);
                if (userIdString != null && int.TryParse(userIdString, out int userId))
                {
                    return userId;
                }
                return null;
            }
        }

        public string? Type => _httpContextAccessor.HttpContext?.User?.FindFirstValue(CONSTANT_CLAIM_TYPES.Type);

        public int? StaffId => int.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(CONSTANT_CLAIM_TYPES.Staff));

        public int? CustomerId => int.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(CONSTANT_CLAIM_TYPES.Customer));
    }
}
