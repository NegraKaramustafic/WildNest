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
    public class AdoptionService : BaseCRUDService<Adoption, AdoptionResponse, AdoptionSearchObject, AdoptionInsertRequest, AdoptionUpdateRequest>, IAdoptionService
    {
        private const string AdminRole = "Admin";

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly IBadgeAwardService _badgeAwardService;
        private readonly INotificationEventService _notificationEvents;

        public AdoptionService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<AdoptionInsertRequest> insertValidator,
            IValidator<AdoptionUpdateRequest> updateValidator,
            IAuthenticatedUserAccessor userAccessor,
            IBadgeAwardService badgeAwardService,
            INotificationEventService notificationEvents)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _userAccessor = userAccessor;
            _badgeAwardService = badgeAwardService;
            _notificationEvents = notificationEvents;
        }

        protected override async Task<IQueryable<Adoption>> IncludeRelatedEntitiesAsync(AdoptionSearchObject? search, IQueryable<Adoption> query = null!)
        {
            query = query
                .Include(a => a.User)
                .Include(a => a.Animal);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<AdoptionResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Adoption>()
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Animal)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                throw new KeyNotFoundException($"Adoption with id {id} not found.");
            }

            return _mapper.Map<AdoptionResponse>(entity);
        }

        public override async Task<AdoptionResponse> InsertAsync(AdoptionInsertRequest request)
        {
            var created = await base.InsertAsync(request);
            var response = await GetByIdAsync(created.Id);
            await _badgeAwardService.CheckAndAwardBadgesAsync(response.UserId);
            await _notificationEvents.NotifyAdoptionCreatedAsync(response);
            return response;
        }

        public override async Task<AdoptionResponse> UpdateAsync(int id, AdoptionUpdateRequest request)
        {
            var entity = await _dbContext.Adoptions.FindAsync(id);
            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                throw new KeyNotFoundException($"Adoption with id {id} not found.");
            }

            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Adoptions.FindAsync(id);
            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                throw new KeyNotFoundException($"Adoption with id {id} not found.");
            }

            await base.DeleteAsync(id);
        }

        protected override Adoption MapInsertRequestToEntity(AdoptionInsertRequest request)
        {
            var animal = _dbContext.Animals.Find(request.AnimalId);
            if (animal == null)
            {
                throw new KeyNotFoundException($"Animal with id {request.AnimalId} not found.");
            }

            if (!animal.AdoptionPrice.HasValue)
            {
                throw new ClinetException("This animal is not available for adoption");
            }

            var entity = base.MapInsertRequestToEntity(request);
            entity.UserId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
            entity.MonthlyAmount = animal.AdoptionPrice.Value;
            entity.Status = AdoptionStatus.Active;
            entity.StartDate = DateTime.UtcNow;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(AdoptionUpdateRequest request, Adoption entity)
        {
            var existingStatus = entity.Status;
            var existingStartDate = entity.StartDate;
            var existingUserId = entity.UserId;
            var existingAnimalId = entity.AnimalId;
            base.MapUpdateRequestToEntity(request, entity);
            entity.Status = existingStatus;
            entity.StartDate = existingStartDate;
            entity.UserId = existingUserId;
            entity.AnimalId = existingAnimalId;
        }

        protected override IEnumerable<Adoption> ApplyFilters(IEnumerable<Adoption> query, AdoptionSearchObject? search)
        {
            if (_userAccessor.IsInRole(AdminRole))
            {
                if (search?.UserId.HasValue == true)
                {
                    query = query.Where(a => a.UserId == search.UserId.Value);
                }
            }
            else
            {
                var userId = _userAccessor.GetUserId();
                query = userId.HasValue
                    ? query.Where(a => a.UserId == userId.Value)
                    : query.Where(a => false);
            }

            if (search != null)
            {
                if (search.AnimalId.HasValue)
                {
                    query = query.Where(a => a.AnimalId == search.AnimalId.Value);
                }

                if (search.Status.HasValue)
                {
                    query = query.Where(a => a.Status == search.Status.Value);
                }
            }

            return query;
        }

        public async Task<AdoptionResponse> CancelAsync(int id)
        {
            var entity = await _dbContext.Adoptions.FindAsync(id);
            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                throw new KeyNotFoundException($"Adoption with id {id} not found.");
            }

            if (entity.Status == AdoptionStatus.Cancelled)
            {
                throw new ClinetException("Adoption is already cancelled.");
            }

            entity.Status = AdoptionStatus.Cancelled;
            await _dbContext.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        private bool CanAccessAdoption(int adoptionUserId)
        {
            if (_userAccessor.IsInRole(AdminRole))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == adoptionUserId;
        }
    }
}
