using api_be.Domain.Interfaces;
using api_be.Models.Common;
using Core.Application.Features.Base.Records;
using static api_be.Domain.Entities.Staff;

namespace api_be.Models
{
    public record StaffDto : BaseDto, IBaseStaff
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? Avatar { get; set; }

        public string? IdCard { get; set; }

        public CardImage? IdCardImage { get; set; }

        public int? PositionId { get; set; }

        public StaffPositionDto? Position { get; set; }
    }
}
