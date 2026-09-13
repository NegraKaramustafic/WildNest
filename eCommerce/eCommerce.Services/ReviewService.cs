using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class ReviewService : BaseCRUDService<Review, ReviewResponse, ReviewSearchObject, ReviewInsertRequest, ReviewUpdateRequest>, IReviewService
    {
        private const string AdminRole = "Admin";

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly RecommendationModelCache _recommendationModelCache;

        public ReviewService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ReviewInsertRequest> insertValidator,
            IValidator<ReviewUpdateRequest> updateValidator,
            IAuthenticatedUserAccessor userAccessor,
            RecommendationModelCache recommendationModelCache)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _userAccessor = userAccessor;
            _recommendationModelCache = recommendationModelCache;
        }

        protected override async Task<IQueryable<Review>> IncludeRelatedEntitiesAsync(ReviewSearchObject? search, IQueryable<Review> query = null!)
        {
            query = query
                .Include(r => r.User)
                .Include(r => r.Activity);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<ReviewResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Review>()
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Activity)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Review with id {id} not found.");
            }

            return _mapper.Map<ReviewResponse>(entity);
        }

        public override async Task<ReviewResponse> InsertAsync(ReviewInsertRequest request)
        {
            var userId = RequireCurrentUserId();

            var alreadyReviewed = _dbContext.Reviews.Any(r =>
                r.UserId == userId && r.ActivityId == request.ActivityId);
            if (alreadyReviewed)
            {
                throw new ClinetException("You have already reviewed this activity");
            }

            var created = await base.InsertAsync(request);
            _recommendationModelCache.MarkDirty();
            return await GetByIdAsync(created.Id);
        }

        protected override Review MapInsertRequestToEntity(ReviewInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.UserId = RequireCurrentUserId();
            return entity;
        }

        public override async Task<ReviewResponse> UpdateAsync(int id, ReviewUpdateRequest request)
        {
            var entity = await _dbContext.Reviews.FindAsync(id);
            if (entity == null || !CanModifyReview(entity.UserId))
            {
                throw new KeyNotFoundException($"Review with id {id} not found.");
            }

            await base.UpdateAsync(id, request);
            _recommendationModelCache.MarkDirty();
            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Reviews.FindAsync(id);
            if (entity == null || !CanModifyReview(entity.UserId))
            {
                throw new KeyNotFoundException($"Review with id {id} not found.");
            }

            await base.DeleteAsync(id);
            _recommendationModelCache.MarkDirty();
        }

        protected override void MapUpdateRequestToEntity(ReviewUpdateRequest request, Review entity)
        {
            var existingUserId = entity.UserId;
            var existingActivityId = entity.ActivityId;
            var existingCreatedAt = entity.CreatedAt;
            base.MapUpdateRequestToEntity(request, entity);
            entity.UserId = existingUserId;
            entity.ActivityId = existingActivityId;
            entity.CreatedAt = existingCreatedAt;
        }

        protected override IEnumerable<Review> ApplyFilters(IEnumerable<Review> query, ReviewSearchObject? search)
        {
            if (search != null)
            {
                if (search.ActivityId.HasValue)
                {
                    query = query.Where(r => r.ActivityId == search.ActivityId.Value);
                }

                if (search.UserId.HasValue)
                {
                    query = query.Where(r => r.UserId == search.UserId.Value);
                }
            }

            return query;
        }

        private int RequireCurrentUserId()
        {
            return _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
        }

        private bool CanModifyReview(int reviewUserId)
        {
            if (_userAccessor.IsInRole(AdminRole))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == reviewUserId;
        }
    }
}
