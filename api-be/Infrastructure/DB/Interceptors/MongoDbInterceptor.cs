using MongoDB.Driver;
using api_be.Core.Entities;
using api_be.Core.Domain.Interfaces;
using System;

namespace api_be.Infrastructure.DB.Interceptors
{
    public class MongoDbInterceptor
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeService _dateTimeService;

        public MongoDbInterceptor(ICurrentUserService currentUserService, IDateTimeService dateTimeService)
        {
            _currentUserService = currentUserService;
            _dateTimeService = dateTimeService;
        }

        public void BeforeInsert<TDocument>(TDocument document) where TDocument : class
        {
            if (document is IAuditableEntity auditableEntity)
            {
                auditableEntity.CreatedAt = _dateTimeService.Now;
                auditableEntity.CreatedBy = _currentUserService.UserId.ToString();
                auditableEntity.UpdatedAt = _dateTimeService.Now;
                auditableEntity.UpdatedBy = _currentUserService.UserId.ToString();
                auditableEntity.IsDeleted = false;
            }
        }

        public void BeforeUpdate<TDocument>(TDocument document) where TDocument : class
        {
            if (document is IAuditableEntity auditableEntity)
            {
                auditableEntity.UpdatedAt = _dateTimeService.Now;
                auditableEntity.UpdatedBy = _currentUserService.UserId.ToString();
            }
        }
    }
}
