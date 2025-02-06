using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Core.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.Request.StaffPossitionRequest
{
    public record CreateOrUpdateStaffPositionRequest : UpdateBaseCommand, IBaseStaffPosition
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Describes { get; set; }

        public List<int?>? Roles { get; set; }

    }
}
