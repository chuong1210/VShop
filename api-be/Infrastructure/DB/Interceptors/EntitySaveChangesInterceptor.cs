using api_be.Infrastructure.DB.Common;
using api_be.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using api_be.Core.Entities;
using System.Threading;
using Azure;
using api_be.Core.Models.Common;

namespace api_be.Infrastructure.DB.Interceptors
{
    public class EntitySaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeService _dateTime;
        private readonly KafkaProducer<string, KafkaMessage<Product>> _producer;

        public EntitySaveChangesInterceptor(ICurrentUserService currentUserService, IDateTimeService dateTime, KafkaProducer<string, KafkaMessage<Product>> kafkaProducer)
        {
            _currentUserService = currentUserService;
            _dateTime = dateTime;
            _producer = kafkaProducer;
            
        }

        public  override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
             UpdateProducts(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            UpdateProducts(eventData.Context);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }



        private async void UpdateProducts(DbContext? context)
        {
            var entries = context.ChangeTracker.Entries<Product>().ToList(); // <--- snapshot

            var userId = _currentUserService.UserId;

            var allEntries = context.ChangeTracker.Entries().ToList(); // <--- snapshot toàn bộ entries

            foreach (var entry in allEntries)
            {
                if (entry.Entity is IAuditableEntity baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        baseEntity.CreatedAt = DateTime.UtcNow;
                        baseEntity.CreatedBy = userId.ToString();
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        baseEntity.UpdatedAt = DateTime.UtcNow;
                        baseEntity.UpdatedBy = userId.ToString();
                    }
                }
            }

            // Gửi Kafka sau khi đã cập nhật dữ liệu
            foreach (var entry in entries)
            {
                string operation = entry.State switch
                {
                    EntityState.Added => "Added",
                    EntityState.Modified => "Modified",
                    EntityState.Deleted => "Deleted",
                    _ => "Unknown"
                };

                var product = entry.Entity;
                var message = new KafkaMessage<Product>
                {
                    Data = product,
                    Operation = operation
                };
                await _producer.ProduceAsync(product.Id.ToString(), message);
            }
        }


        private async Task UpdateEntitiesProduct(DbContext? context)
        {
            if (context == null) return;

            foreach (var entry in context.ChangeTracker.Entries<Product>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    var product = entry.Entity;
                    var _product = new Product
                    {
                      Id=  product.Id,
                      Name=  product.Name,
                      Price=  product.Price,
                     Quantity=   product.Quantity,
                       Status= product.Status
                    };
                   var _operation = entry.State.ToString();

                    var message = new
                    {
                        Action = entry.State.ToString(),
                        Product = new
                        {
                            product.Id,
                            product.Name,
                            product.Price,
                            product.Quantity,
                            product.Status
                        }
                    };
                    var value = new KafkaMessage<Product>
                    {
                        Data = product,
                        Operation = _operation
                    };

                    await _producer.ProduceAsync(product.Id.ToString(), value);
                }
            }
        
    }
    public void UpdateEntities(DbContext? context)
        {
            if (context is null)
                return;
            

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is IAuditableEntity auditableEntity)
                {
                    auditableEntity.CreatedAt = _dateTime.Now;
                    auditableEntity.CreatedBy = _currentUserService.UserId.ToString();
                    auditableEntity.UpdatedAt = _dateTime.Now;
                    auditableEntity.UpdatedBy = _currentUserService.UserId.ToString();

                    if (entry.State == EntityState.Added)
                    {
                        auditableEntity.IsDeleted = false;
                    }
                    else if (entry.State == EntityState.Deleted && 
                        !CommonBusinessData.ImmediateDeleteTypes.Contains(entry.Entity.GetType()))
                    {
                        entry.State = EntityState.Unchanged;
                        auditableEntity.IsDeleted = true;
                    }
                }

                //if (entry.Entity is IHardDeleteEntity hardDeleteEntity && entry.State == EntityState.Added)
                //{
                //    hardDeleteEntity.CreatedAt = _dateTime.Now;
                //    hardDeleteEntity.CreatedBy = _currentUserService.UserId.ToString();
                //    hardDeleteEntity.UpdatedAt = _dateTime.Now;
                //    hardDeleteEntity.UpdatedBy = _currentUserService.UserId.ToString();
                //}
            }

        }
    }
}
