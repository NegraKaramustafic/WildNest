using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class PaymentService : BaseCRUDService<Payment, PaymentResponse, PaymentSearchObject, PaymentInsertRequest, PaymentUpdateRequest>, IPaymentService
    {
        private readonly INotificationEventService _notificationEvents;

        public PaymentService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<PaymentInsertRequest> insertValidator,
            IValidator<PaymentUpdateRequest> updateValidator,
            INotificationEventService notificationEvents)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _notificationEvents = notificationEvents;
        }

        protected override Payment MapInsertRequestToEntity(PaymentInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.Status = PaymentStatus.Pending;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(PaymentUpdateRequest request, Payment entity)
        {
            var existingStatus = entity.Status;
            base.MapUpdateRequestToEntity(request, entity);
            entity.Status = existingStatus;
        }

        protected override IEnumerable<Payment> ApplyFilters(IEnumerable<Payment> query, PaymentSearchObject? search)
        {
            if (search != null)
            {
                if (search.Method.HasValue)
                {
                    query = query.Where(p => p.Method == search.Method.Value);
                }

                if (search.Status.HasValue)
                {
                    query = query.Where(p => p.Status == search.Status.Value);
                }
            }

            return query;
        }

        public async Task<PaymentResponse> CompleteAsync(int id)
        {
            var entity = await GetEntityOrThrowAsync(id);
            if (entity.Status != PaymentStatus.Pending)
            {
                throw new ClinetException("Cannot complete a payment in its current state.");
            }

            entity.Status = PaymentStatus.Completed;
            await _dbContext.SaveChangesAsync();

            var userId = await FindPaymentUserIdAsync(entity.Id);
            if (userId.HasValue)
            {
                await _notificationEvents.NotifyPaymentCompletedAsync(userId.Value, entity.Id, entity.Amount);
            }

            return _mapper.Map<PaymentResponse>(entity);
        }

        public async Task<PaymentResponse> FailAsync(int id)
        {
            var entity = await GetEntityOrThrowAsync(id);
            if (entity.Status != PaymentStatus.Pending)
            {
                throw new ClinetException("Cannot fail a payment in its current state.");
            }

            entity.Status = PaymentStatus.Failed;
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<PaymentResponse>(entity);
        }

        private async Task<Payment> GetEntityOrThrowAsync(int id)
        {
            var entity = await _dbContext.Payments.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Payment with id {id} not found.");
            }

            return entity;
        }

        private async Task<int?> FindPaymentUserIdAsync(int paymentId)
        {
            return await _dbContext.Tickets.Where(t => t.PaymentId == paymentId).Select(t => (int?)t.UserId).FirstOrDefaultAsync()
                ?? await _dbContext.Reservations.Where(r => r.PaymentId == paymentId).Select(r => (int?)r.UserId).FirstOrDefaultAsync()
                ?? await _dbContext.Adoptions.Where(a => a.PaymentId == paymentId).Select(a => (int?)a.UserId).FirstOrDefaultAsync()
                ?? await _dbContext.Donations.Where(d => d.PaymentId == paymentId).Select(d => (int?)d.UserId).FirstOrDefaultAsync();
        }
    }
}
