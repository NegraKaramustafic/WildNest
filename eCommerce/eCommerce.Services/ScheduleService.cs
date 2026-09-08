using System;
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
    public class ScheduleService : BaseCRUDService<Schedule, ScheduleResponse, ScheduleSearchObject, ScheduleInsertRequest, ScheduleUpdateRequest>, IScheduleService
    {
        public ScheduleService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<ScheduleInsertRequest> insertValidator, IValidator<ScheduleUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override async Task<IQueryable<Schedule>> IncludeRelatedEntitiesAsync(ScheduleSearchObject? search, IQueryable<Schedule> query = null!)
        {
            query = query.Include(s => s.Activity);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<ScheduleResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Schedule>()
                .AsNoTracking()
                .Include(s => s.Activity)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Schedule with id {id} not found.");
            }

            return _mapper.Map<ScheduleResponse>(entity);
        }

        public override async Task<ScheduleResponse> InsertAsync(ScheduleInsertRequest request)
        {
            var created = await base.InsertAsync(request);
            return await GetByIdAsync(created.Id);
        }

        public override async Task<ScheduleResponse> UpdateAsync(int id, ScheduleUpdateRequest request)
        {
            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
        }

        protected override Schedule MapInsertRequestToEntity(ScheduleInsertRequest request)
        {
            var activity = GetActiveActivity(request.ActivityId);
            var entity = base.MapInsertRequestToEntity(request);

            if (!request.CapacityLeft.HasValue)
            {
                entity.CapacityLeft = activity.Capacity;
            }

            return entity;
        }

        protected override void MapUpdateRequestToEntity(ScheduleUpdateRequest request, Schedule entity)
        {
            var existingCapacityLeft = entity.CapacityLeft;
            base.MapUpdateRequestToEntity(request, entity);

            if (!request.CapacityLeft.HasValue)
            {
                entity.CapacityLeft = existingCapacityLeft;
            }
        }

        protected override IEnumerable<Schedule> ApplyFilters(IEnumerable<Schedule> query, ScheduleSearchObject? search)
        {
            if (search != null)
            {
                if (search.ActivityId.HasValue)
                {
                    query = query.Where(s => s.ActivityId == search.ActivityId.Value);
                }

                if (search.Date.HasValue)
                {
                    query = query.Where(s => s.Date == search.Date.Value);
                }

                if (search.DateFrom.HasValue)
                {
                    query = query.Where(s => s.Date >= search.DateFrom.Value);
                }

                if (search.DateTo.HasValue)
                {
                    query = query.Where(s => s.Date <= search.DateTo.Value);
                }

                if (search.Status.HasValue)
                {
                    query = query.Where(s => s.Status == search.Status.Value);
                }
            }

            return query;
        }

        private Activity GetActiveActivity(int activityId)
        {
            var activity = _dbContext.Activities.Find(activityId);
            if (activity == null)
            {
                throw new KeyNotFoundException($"Activity with id {activityId} not found.");
            }

            if (activity.Status != ActivityStatus.Active)
            {
                throw new ClinetException("Cannot create a schedule for an activity that is not Active.");
            }

            return activity;
        }
    }
}
