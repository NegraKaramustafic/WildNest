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
        private readonly IReservationService _reservationService;

        public ScheduleService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ScheduleInsertRequest> insertValidator,
            IValidator<ScheduleUpdateRequest> updateValidator,
            IReservationService reservationService)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _reservationService = reservationService;
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
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var entity = await _dbContext.Schedules.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Schedule with id {id} not found.");
            }

            if (request.ActivityId != entity.ActivityId)
            {
                GetActiveActivity(request.ActivityId);
            }

            var effectiveCapacity = request.CapacityLeft ?? entity.CapacityLeft;
            EnsureCapacityWithinActivity(request.ActivityId, effectiveCapacity);

            var openReservations = await _dbContext.Reservations
                .Where(r => r.ScheduleId == id && r.Status != ReservationStatus.Cancelled)
                .ToListAsync();

            if (openReservations.Count > 0 && ChangesLockedFields(entity, request))
            {
                throw new ClinetException("Termin ima rezervacije. Aktivnost, datum i vrijeme se ne mogu mijenjati.");
            }

            var cancelling = request.Status == ScheduleStatus.Cancelled && entity.Status != ScheduleStatus.Cancelled;
            if (cancelling && openReservations.Count > 0)
            {
                var blocked = openReservations.Any(r =>
                    r.Status != ReservationStatus.Pending && r.Status != ReservationStatus.Confirmed);
                if (blocked)
                {
                    throw new ClinetException(
                        "Termin ima završene rezervacije. Završenu rezervaciju nije moguće otkazati kroz postojeći tok otkazivanja, pa se termin ne može otkazati.");
                }

                foreach (var reservation in openReservations)
                {
                    await _reservationService.CancelAsync(reservation.Id);
                }

                await _dbContext.Entry(entity).ReloadAsync();
                entity.Status = ScheduleStatus.Cancelled;
                await _dbContext.SaveChangesAsync();
                return await GetByIdAsync(id);
            }

            EnsureNoActiveOverlap(request.ActivityId, request.Date, request.StartTime, request.EndTime, id, request.Status);
            MapUpdateRequestToEntity(request, entity);
            await _dbContext.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Schedules.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Schedule with id {id} not found.");
            }

            var statuses = await _dbContext.Reservations
                .Where(r => r.ScheduleId == id)
                .Select(r => r.Status)
                .ToListAsync();

            if (statuses.Any(status => status != ReservationStatus.Cancelled))
            {
                throw new ClinetException("Termin ima rezervacije i ne može se obrisati – otkažite ga.");
            }

            if (statuses.Count > 0)
            {
                throw new ClinetException("Termin ima otkazane rezervacije i ne može se obrisati.");
            }

            await base.DeleteAsync(id);
        }

        protected override Schedule MapInsertRequestToEntity(ScheduleInsertRequest request)
        {
            var activity = GetActiveActivity(request.ActivityId);
            if (request.Date < DateOnly.FromDateTime(DateTime.Today))
            {
                throw new ClinetException("Datum ne može biti u prošlosti.");
            }

            if (request.EndTime <= request.StartTime)
            {
                throw new ClinetException("Vrijeme završetka mora biti nakon vremena početka.");
            }

            if (request.CapacityLeft.HasValue)
            {
                EnsureCapacityValue(request.CapacityLeft.Value, activity.Capacity);
            }

            EnsureNoActiveOverlap(request.ActivityId, request.Date, request.StartTime, request.EndTime, null, request.Status);

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
                throw new ClinetException("Termin se može kreirati samo za aktivnu aktivnost.");
            }

            return activity;
        }

        private void EnsureCapacityWithinActivity(int activityId, int capacityLeft)
        {
            var activity = _dbContext.Activities.Find(activityId);
            if (activity == null)
            {
                throw new KeyNotFoundException($"Activity with id {activityId} not found.");
            }

            EnsureCapacityValue(capacityLeft, activity.Capacity);
        }

        private static void EnsureCapacityValue(int capacityLeft, int activityCapacity)
        {
            if (capacityLeft < 0)
            {
                throw new ClinetException("Preostali kapacitet ne može biti negativan.");
            }

            if (capacityLeft > activityCapacity)
            {
                throw new ClinetException($"Preostali kapacitet ne može biti veći od kapaciteta aktivnosti ({activityCapacity}).");
            }
        }

        private void EnsureNoActiveOverlap(
            int activityId,
            DateOnly date,
            TimeOnly startTime,
            TimeOnly endTime,
            int? excludeScheduleId,
            ScheduleStatus status)
        {
            if (status != ScheduleStatus.Active || endTime <= startTime)
            {
                return;
            }

            var overlaps = _dbContext.Schedules.Any(schedule =>
                (!excludeScheduleId.HasValue || schedule.Id != excludeScheduleId.Value) &&
                schedule.ActivityId == activityId &&
                schedule.Date == date &&
                schedule.Status == ScheduleStatus.Active &&
                schedule.StartTime < endTime &&
                startTime < schedule.EndTime);

            if (overlaps)
            {
                throw new ClinetException("Već postoji aktivan termin koji se preklapa za istu aktivnost na isti datum.");
            }
        }

        private static bool ChangesLockedFields(Schedule entity, ScheduleUpdateRequest request)
        {
            return request.ActivityId != entity.ActivityId
                || request.Date != entity.Date
                || request.StartTime != entity.StartTime
                || request.EndTime != entity.EndTime;
        }
    }
}
