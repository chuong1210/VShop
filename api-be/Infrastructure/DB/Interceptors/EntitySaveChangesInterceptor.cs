using api_be.Infrastructure.DB.Common;
using api_be.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using api_be.Core.Entities;
using System.Threading;
using api_be.Core.Models.Common;
using Microsoft.Extensions.Logging;

namespace api_be.Infrastructure.DB.Interceptors
{
    public class EntitySaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeService _dateTime;
        private readonly KafkaProducer<string, KafkaMessage<Product>> _producer;
        private readonly ILogger<EntitySaveChangesInterceptor> _logger;

        // Dictionary để lưu trữ state của entities trước khi save
        private readonly Dictionary<Product, EntityState> _productStates = new();

        public EntitySaveChangesInterceptor(
            ICurrentUserService currentUserService,
            IDateTimeService dateTime,
            KafkaProducer<string, KafkaMessage<Product>> kafkaProducer,
            ILogger<EntitySaveChangesInterceptor> logger)
        {
            _currentUserService = currentUserService;
            _dateTime = dateTime;
            _producer = kafkaProducer;
            _logger = logger;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            CaptureProductStates(eventData.Context); // Lưu state trước khi save

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            CaptureProductStates(eventData.Context); // Lưu state trước khi save

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // Override SavedChanges - được gọi SAU KHI SaveChanges thành công
        public override int SavedChanges(
            SaveChangesCompletedEventData eventData,
            int result)
        {
            // Gửi Kafka messages SAU KHI save thành công
            SendKafkaMessages(eventData.Context).GetAwaiter().GetResult();

            return base.SavedChanges(eventData, result);
        }

        // Override SavedChangesAsync - được gọi SAU KHI SaveChangesAsync thành công
        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            // Gửi Kafka messages SAU KHI save thành công
            await SendKafkaMessages(eventData.Context);

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        // Lưu trữ state của Product entities trước khi save
        private void CaptureProductStates(DbContext? context)
        {
            if (context == null) return;

            _productStates.Clear();

            var productEntries = context.ChangeTracker.Entries<Product>()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in productEntries)
            {
                _productStates[entry.Entity] = entry.State;
            }

            _logger.LogInformation($"Captured {_productStates.Count} product state(s) before save");
        }

        // Gửi Kafka messages SAU KHI SaveChanges thành công
        private async Task SendKafkaMessages(DbContext? context)
        {
            if (context == null || _productStates.Count == 0)
            {
                return;
            }

            _logger.LogInformation($"Sending {_productStates.Count} Kafka message(s)");

            foreach (var kvp in _productStates)
            {
                var product = kvp.Key;
                var state = kvp.Value;

                try
                {
                    // Bây giờ product.Id đã có giá trị (đã được gán bởi database)
                    if (product.Id <= 0)
                    {
                        _logger.LogWarning($"Product ID is invalid: {product.Id}, skipping Kafka message");
                        continue;
                    }

                    var operation = state switch
                    {
                        EntityState.Added => "Added",
                        EntityState.Modified => "Modified",
                        EntityState.Deleted => "Deleted",
                        _ => "Unknown"
                    };

                    var message = new KafkaMessage<Product>
                    {
                        Data = product,
                        Operation = operation
                    };

                    await _producer.ProduceAsync(product.Id.ToString(), message);

                    _logger.LogInformation(
                        $"Kafka message sent successfully: Operation={operation}, ProductId={product.Id}, ProductName={product.Name}"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Failed to send Kafka message for Product ID {product.Id}: {ex.Message}"
                    );
                    // Không throw exception để không làm rollback transaction
                }
            }

            _productStates.Clear();
        }

        public void UpdateEntities(DbContext? context)
        {
            if (context is null)
                return;

            var userId = _currentUserService.UserId.ToString();
            var now = _dateTime.Now;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is IAuditableEntity auditableEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        auditableEntity.CreatedAt = now;
                        auditableEntity.CreatedBy = userId;
                        auditableEntity.UpdatedAt = now;
                        auditableEntity.UpdatedBy = userId;
                        auditableEntity.IsDeleted = false;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        auditableEntity.UpdatedAt = now;
                        auditableEntity.UpdatedBy = userId;
                    }
                    else if (entry.State == EntityState.Deleted &&
                        !CommonBusinessData.ImmediateDeleteTypes.Contains(entry.Entity.GetType()))
                    {
                        entry.State = EntityState.Unchanged;
                        auditableEntity.IsDeleted = true;
                        auditableEntity.UpdatedAt = now;
                        auditableEntity.UpdatedBy = userId;
                    }
                }
            }
        }
    }
}