using api_be.Domain.Entities;
using api_be.Entities.Auth;
using api_be.Models;
using api_be.Models.Request;
using AutoMapper;
using Sieve.Models;

namespace api_be.Mapping
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
            CreateMap<Distributor, DistributorDto>().ReverseMap();

            CreateMap<Category, CategoryDto>().ReverseMap();

            CreateMap<Product, ProductDto>().ReverseMap();


            CreateMap<SupplierOrder, SupplierOrderDto>().ReverseMap();

            CreateMap<Payment, PaymentDto>().ReverseMap();


            CreateMap<StaffPosition, StaffPositionDto>().ReverseMap();

            CreateMap<Promotion, PromotionDto>().ReverseMap();


            CreateMap<Coupon, CouponDto>().ReverseMap();


            CreateMap<Order, OrderDto>().ReverseMap();
            CreateMap<Order, CartDto>().ReverseMap();
            CreateMap<DetailOrder, DetailOrderDto>().ReverseMap();
            CreateMap<DetailOrder, DetailCartDto>().ReverseMap();


            CreateMap<SupplierOrder, SupplierOrderDto>().ReverseMap();
            CreateMap<DetailSupplierOrder, DetailSupplierOrderDto>().ReverseMap();

            CreateMap<SupplierOrder, ImportGoodDto>().ReverseMap();

            CreateMap<Delivery, DeliveryDto>().ReverseMap();


            CreateMap<SieveModel, GetListUserRequest>().ReverseMap();

            CreateMap<User, CreateUserRequest>().ReverseMap();
            CreateMap<User, UpdateUserRequest>().ReverseMap();




        }
    }
}
