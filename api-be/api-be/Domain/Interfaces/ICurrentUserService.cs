﻿namespace api_be.Domain.Interfaces
{
    public interface ICurrentUserService
    {
        public int? UserId { get; }

        public string? Type { get; }

        public int? StaffId { get; }

        public int? CustomerId { get; }
    }
}
