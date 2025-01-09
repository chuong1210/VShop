﻿using api_be.In;
using api_be.Models.Common;
using static api_be.Domain.Entities.SupplierOrder;

namespace api_be.Models
{
    public record ImportGoodDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public DateTime? BookingDate { get; set; }

        public decimal? Total { get; set; }

        public string? Deliver { get; set; }

        public SupplierOrderStatus? Status { get; set; }

        // Khoá ngoại
        public int? ParentId { get; set; }

        public SupplierOrderDto? Parent { get; set; }

        // Nhân viên duyệt đơn hàng: Nhận hàng
        public int? ApproveStaffId { get; set; }

        public StaffDto? ApproveStaff { get; set; }

        // Nhân viên giao hàng: của người ta
        public string? ReceivingStaff { get; set; }

        public int? DistributorId { get; set; }

        public DistributorDto? Distributor { get; set; }

        public List<ProductImportGoodDto>? Details { get; set; }
    }
}
