using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class ActivityTypeService : BaseCRUDService<ActivityType, ActivityTypeResponse, ActivityTypeSearchObject, ActivityTypeInsertRequest, ActivityTypeUpdateRequest>, IActivityTypeService
    {
        public ActivityTypeService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ActivityTypeInsertRequest> insertValidator,
            IValidator<ActivityTypeUpdateRequest> updateValidator,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
        }

        protected override string NotFoundMessage => "Tip aktivnosti nije pronađen.";

        protected override IQueryable<ActivityType> ApplyFilters(IQueryable<ActivityType> query, ActivityTypeSearchObject? search)
        {
            if (search != null && !string.IsNullOrWhiteSpace(search.Name))
            {
                var name = search.Name.Trim();
                query = query.Where(type => type.Name.Contains(name));
            }

            return query;
        }

        protected override IQueryable<ActivityType> ApplyDefaultOrder(IQueryable<ActivityType> query)
        {
            return query.OrderBy(type => type.Name).ThenBy(type => type.Id);
        }

        public override async Task<PageResult<ActivityTypeResponse>> GetAllAsync(ActivityTypeSearchObject? search = null)
        {
            var page = await base.GetAllAsync(search);
            await AttachCountsAsync(page.Items);
            return page;
        }

        public override async Task<ActivityTypeResponse> GetByIdAsync(int id)
        {
            var item = await base.GetByIdAsync(id);
            await AttachCountsAsync(new List<ActivityTypeResponse> { item });
            return item;
        }

        public override async Task<ActivityTypeResponse> InsertAsync(ActivityTypeInsertRequest request)
        {
            Normalize(request);
            await ValidateAsync(request);
            await EnsureUniqueNameAsync(request.Name, null);

            try
            {
                var created = await base.InsertAsync(request);
                return await GetByIdAsync(created.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueNameViolation(ex))
            {
                throw DuplicateName(request.Name);
            }
        }

        public override async Task<ActivityTypeResponse> UpdateAsync(int id, ActivityTypeUpdateRequest request)
        {
            Normalize(request);
            await ValidateAsync(request);
            await EnsureUniqueNameAsync(request.Name, id);

            try
            {
                var updated = await base.UpdateAsync(id, request);
                return await GetByIdAsync(updated.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueNameViolation(ex))
            {
                throw DuplicateName(request.Name);
            }
        }

        public override async Task DeleteAsync(int id)
        {
            var inUse = await _dbContext.Activities.AnyAsync(activity => activity.ActivityTypeId == id);
            if (inUse)
            {
                throw new ClientException("Tip aktivnosti koriste aktivnosti i ne može se obrisati.");
            }

            await base.DeleteAsync(id);
        }

        private async Task ValidateAsync(ActivityTypeInsertRequest request)
        {
            var result = await _insertValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private async Task ValidateAsync(ActivityTypeUpdateRequest request)
        {
            var result = await _updateValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private async Task EnsureUniqueNameAsync(string name, int? excludeId)
        {
            var normalized = name.Trim().ToLower();
            var exists = await _dbContext.ActivityTypes.AnyAsync(type =>
                type.Name.ToLower() == normalized &&
                (excludeId == null || type.Id != excludeId.Value));

            if (exists)
            {
                throw DuplicateName(name);
            }
        }

        private async Task AttachCountsAsync(IReadOnlyList<ActivityTypeResponse> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            var ids = items.Select(item => item.Id).ToList();
            var counts = await _dbContext.Activities
                .Where(activity => ids.Contains(activity.ActivityTypeId))
                .GroupBy(activity => activity.ActivityTypeId)
                .Select(group => new { Id = group.Key, Count = group.Count() })
                .ToListAsync();

            var map = counts.ToDictionary(item => item.Id, item => item.Count);
            foreach (var item in items)
            {
                item.ActivityCount = map.TryGetValue(item.Id, out var count) ? count : 0;
            }
        }

        private static void Normalize(ActivityTypeInsertRequest request)
        {
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.Description = NormalizeDescription(request.Description);
        }

        private static void Normalize(ActivityTypeUpdateRequest request)
        {
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.Description = NormalizeDescription(request.Description);
        }

        private static string? NormalizeDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            return description.Trim();
        }

        private static ValidationException DuplicateName(string name)
        {
            return new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(ActivityTypeInsertRequest.Name),
                    $"Tip aktivnosti s nazivom '{name.Trim()}' već postoji.")
            });
        }

        private static bool IsUniqueNameViolation(DbUpdateException exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;
            return message.Contains("IX_ActivityTypes_Name", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
