﻿
using api_be.Common.Interfaces;
using api_be.Auth;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace api_be.DB.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;

        public PermissionService(ISupermarketDbContext pContext, IMapper pMapper)
        {
            _context = pContext;
            _mapper = pMapper;
        }

        public async Task Create(List<string> pPermissions)
        {
            foreach (var permission in pPermissions)
            {
                var per = await _context.Permissions
                    .FirstOrDefaultAsync(x => x.Name == permission);

                if (per == null)
                {
                    var newPer = new Permission { Name = permission };
                    var newPermission = await _context.Permissions.AddAsync(newPer);
                    await _context.SaveChangesAsync(default(CancellationToken));
                }
            }
        }
    }
}