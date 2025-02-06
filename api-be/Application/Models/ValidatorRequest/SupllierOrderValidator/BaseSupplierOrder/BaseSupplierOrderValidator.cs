using api_be.Core.Domain.Interfaces;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.ValidatorRequest.SupllierOrderValidator.BaseSupplierOrder
{
    public class BaseSupplierOrderValidator : AbstractValidator<IBaseSupplierOrder>
    {
        public BaseSupplierOrderValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            RuleFor(x => x.DistributorId)
                .MustAsync(async (distributorId, token) =>
                {
                    return distributorId == null ||
                           await pContext.Distributors.AnyAsync(x => x.Id == distributorId);
                }).WithMessage(ValidatorTransform.NotExists(Modules.SupplierOrder.DistributorId));

            RuleFor(x => x.Details)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.SupplierOrder.DetailSupplierOrder))
                .MustAsync(async (details, token) =>
                {
                    if (details != null)
                    {
                        foreach (var detail in details)
                        {
                            if (details.Count(x => x.ProductId == detail.ProductId) > 1)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }).WithMessage("Id của sản phẩm trong details bị trùng lặp!");

            RuleFor(x => x.Details)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.SupplierOrder.DetailSupplierOrder))
                .MustAsync(async (details, token) =>
                {
                    if (details != null)
                    {
                        foreach (var detail in details)
                        {
                            if (detail.Price <= 0 || detail.Quantity <= 0)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }).WithMessage("Giá và số lượng nhập phải lớn hơn 0!");

            RuleFor(x => x.Details)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.SupplierOrder.DetailSupplierOrder))
                .MustAsync(async (details, token) =>
                {
                    if (details != null)
                    {
                        foreach (var detail in details)
                        {
                            if (!await pContext.Products.AnyAsync(x => x.Id == detail.ProductId &&
                                                    x.Type == Core.Entities.Product.ProductType.Option &&
                                                    x.Price >= detail.Price &&
                                                    x.IsDeleted == false))
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }).WithMessage("Giá nhập không được lớn hơn giá bán của sản phẩm!");
        }
    }
    }
