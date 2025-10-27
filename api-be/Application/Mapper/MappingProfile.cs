using api_be.Core.Entities;
using api_be.Core.Entities.Auth;
using api_be.Application.Models.Request;
using AutoMapper;
using Sieve.Models;
using api_be.Application.Responses;
using api_be.Application.Models.Request.RoleRequest;
using api_be.Application.Models.Request.CouponRequest;
using api_be.Application.Models.Request.PromotionRequest;
using api_be.Application.Models.Request.DeliveryRequest;
using api_be.Application.Models.ValidatorRequest.RoleValidator;
using api_be.Application.Models.Request.PaymentRequest;
using api_be.Application.Models.Request.DistributorRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.StaffValidator;
using api_be.Application.Models.Request.StaffRequest;
using api_be.Application.Models.Request.SupplierOrderRequest;
using api_be.Application.Models.Request.ImportGoodRequest;
using api_be.Application.Models.Request.StaffPossitionRequest;
using api_be.Application.Responses.PaymentResponse;
namespace api_be.Application.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Ánh xạ từ RegisterAccountRequest sang User
            CreateMap<RegisterAccountRequest, User>()
                .ForMember(dest => dest.Password, opt => opt.Ignore()) // Không ánh xạ mật khẩu trực tiếp
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => User.UserType.User)); // Gán loại người dùng mặc định là "User"


            CreateMap<User, UserDto>()
          .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.Name)))
          .ReverseMap()
          .ForMember(dest => dest.UserRoles, opt => opt.Ignore());
            CreateMap<Customer, CustomerDto>()
     .ReverseMap(); // Nếu cần ánh xạ ngược từ CustomerDto về Customer




            //            CreateMap<User, UserDto>()
            //    .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
            //        src.UserRoles != null ? src.UserRoles.Select(ur => ur.Role.Name) : new List<string>())).ReverseMap()
            //;

            CreateMap<CreateProductRequest, Product>().ReverseMap();
            CreateMap<CreateOrUpdateRoleRequest, Role>().ReverseMap();
            CreateMap<CreateOrUpdateCopuponRequest, Coupon>().ReverseMap();
            CreateMap<CreateOrUpdatePromotionRequest, Promotion>().ReverseMap();
            CreateMap<CreateOrUpdateDeliveryRequest, Delivery>().ReverseMap();
            CreateMap<CreateOrUpdatePaymentRequest, Payment>().ReverseMap();
            CreateMap<CreateOrUpdateDistributorRequest, Distributor>().ReverseMap();
            CreateMap<MessageRequest, Message>().ReverseMap();
            CreateMap<CreateOrUpdateStaffRequest, Staff>().ReverseMap();
            CreateMap<CreateOrUpdateSupplierOrderRequest, SupplierOrder>().ReverseMap();
            CreateMap<CreateImportGoodsRequest, SupplierOrder>().ReverseMap();
            CreateMap<UpdateImportGoodsRequest, SupplierOrder>().ReverseMap();
            CreateMap<CreateOrUpdateStaffPositionRequest, StaffPosition>().ReverseMap();
            CreateMap<CreateOrUpdateStaffRequest, Staff>().ReverseMap();







            CreateMap<Distributor, DistributorDto>().ReverseMap();

            CreateMap<Category, CategoryDto>().ReverseMap();

            CreateMap<Product, ProductDto>().ReverseMap();



            CreateMap<SupplierOrder, SupplierOrderDto>().ReverseMap();

            CreateMap<Payment, PaymentDto>().ReverseMap();


            CreateMap<Staff, StaffDto>().ReverseMap();

            CreateMap<StaffPosition, StaffPositionDto>().ReverseMap();

            CreateMap<Promotion, PromotionDto>().ReverseMap();


            CreateMap<Coupon, CouponDto>().ReverseMap();
            CreateMap<Permission, PermissionDto>().ReverseMap();


            CreateMap<Order, OrderDto>().ReverseMap();
            CreateMap<Order, CartDto>().ReverseMap();
            CreateMap<DetailOrder, DetailOrderDto>().ReverseMap();
            CreateMap<DetailOrder, DetailCartDto>().ReverseMap();


            CreateMap<SupplierOrder, SupplierOrderDto>().ReverseMap();
            CreateMap<DetailSupplierOrder, DetailSupplierOrderDto>().ReverseMap();

            CreateMap<SupplierOrder, ImportGoodDto>().ReverseMap();

            CreateMap<Delivery, DeliveryDto>().ReverseMap();


            CreateMap<SieveModel, GetListUserRequest>().ReverseMap();
            CreateMap<SieveModel, ListBaseCommand>().ReverseMap();


            CreateMap<User, CreateUserRequest>().ReverseMap();

            CreateMap<User, UpdateUserRequest>().ReverseMap();


            CreateMap<Message, MessageRequest>().ReverseMap();
            CreateMap<Message, MessageDto>()   .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString())).ReverseMap();




        }
    }
}
