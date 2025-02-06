using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Core.Domain;
using api_be.Core.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.Request.StaffRequest
{
    public record CreateOrUpdateStaffRequest:UpdateBaseCommand, IBaseStaff
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
    }
}
