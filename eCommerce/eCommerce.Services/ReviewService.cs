using System;
using eCommerce.Model;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using eCommerce.Services.Sentiment;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class ReviewService : BaseCRUDService<Review, ReviewResponse, ReviewSearchObject, ReviewInsertRequest, ReviewUpdateRequest>, IReviewService
    {

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly RecommendationModelCache _recommendationModelCache;
        private readonly ISentimentService _sentiment;

        public ReviewService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ReviewInsertRequest> insertValidator,
            IValidator<ReviewUpdateRequest> updateValidator,
            IAuthenticatedUserAccessor userAccessor,
            RecommendationModelCache recommendationModelCache,
            ISentimentService sentiment,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
            _userAccessor = userAccessor;
            _recommendationModelCache = recommendationModelCache;
            _sentiment = sentiment;
        }

        protected override string NotFoundMessage => "Review not found.";

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
                ThrowNotFound("Review", id, "Review not found.");
            }

            return _mapper.Map<ReviewResponse>(entity);
        }

        public override async Task<ReviewResponse> InsertAsync(ReviewInsertRequest request)
        {
            var userId = RequireCurrentUserId();
            await _sentiment.EnsureReadyAsync();

            var alreadyReviewed = await _dbContext.Reviews.AnyAsync(r =>
                r.UserId == userId && r.ActivityId == request.ActivityId);
            if (alreadyReviewed)
            {
                throw new ClientException("You have already reviewed this activity.");
            }

            var created = await base.InsertAsync(request);
            _recommendationModelCache.MarkDirty();
            return await GetByIdAsync(created.Id);
        }

        protected override Review MapInsertRequestToEntity(ReviewInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.UserId = RequireCurrentUserId();
            ApplySentiment(entity);
            return entity;
        }

        public override async Task<ReviewResponse> UpdateAsync(int id, ReviewUpdateRequest request)
        {
            await _sentiment.EnsureReadyAsync();
            var entity = await _dbContext.Reviews.FindAsync(id);
            if (entity == null || !CanModifyReview(entity.UserId))
            {
                ThrowNotFound("Review", id, "Review not found.");
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
                ThrowNotFound("Review", id, "Review not found.");
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
            ApplySentiment(entity);
        }

        public async Task<int> BackfillSentimentAsync()
        {
            await _sentiment.EnsureReadyAsync();
            var pending = await _dbContext.Reviews
                .Where(review => review.SentimentLabel == null && review.Comment != "")
                .ToListAsync();

            var updated = 0;
            foreach (var review in pending)
            {
                var result = _sentiment.Classify(review.Comment);
                if (result.Label == null)
                {
                    continue;
                }

                review.SentimentLabel = result.Label;
                review.SentimentScore = result.Score;
                updated++;
            }

            if (updated > 0)
            {
                await _dbContext.SaveChangesAsync();
            }

            return updated;
        }

        public async Task<ReviewSentimentStatisticsResponse> GetSentimentStatisticsAsync()
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                throw new UnauthorizedAccessException("Only administrators can view review sentiment statistics.");
            }

            var byLabel = await _dbContext.Reviews.AsNoTracking()
                .GroupBy(review => review.SentimentLabel)
                .Select(group => new
                {
                    Label = group.Key,
                    Count = group.Count(),
                    AverageRating = group.Average(review => (double?)review.Rating)
                })
                .ToListAsync();

            var byMonth = await _dbContext.Reviews.AsNoTracking()
                .Where(review => review.SentimentLabel != null)
                .GroupBy(review => new { review.CreatedAt.Year, review.CreatedAt.Month, review.SentimentLabel })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Label = group.Key.SentimentLabel,
                    Count = group.Count()
                })
                .ToListAsync();

            var byActivity = await _dbContext.Reviews.AsNoTracking()
                .Where(review => review.SentimentLabel != null)
                .GroupBy(review => new { review.ActivityId, review.Activity.Name, review.SentimentLabel })
                .Select(group => new
                {
                    group.Key.ActivityId,
                    group.Key.Name,
                    Label = group.Key.SentimentLabel,
                    Count = group.Count(),
                    AverageRating = group.Average(review => (double?)review.Rating)
                })
                .ToListAsync();

            var byAnimal = await (
                from review in _dbContext.Reviews.AsNoTracking()
                where review.SentimentLabel != null && review.Activity.AnimalId != null
                join animal in _dbContext.Animals.AsNoTracking()
                    on review.Activity.AnimalId equals (int?)animal.Id
                group review by new { animal.Id, animal.Name, review.SentimentLabel }
                into grouped
                select new
                {
                    grouped.Key.Id,
                    grouped.Key.Name,
                    Label = grouped.Key.SentimentLabel,
                    Count = grouped.Count()
                }).ToListAsync();

            var total = byLabel.Sum(row => row.Count);
            var labeled = byLabel.Where(row => row.Label != null).Sum(row => row.Count);
            var ratingWeight = byLabel.Sum(row => (row.AverageRating ?? 0) * row.Count);

            var labels = SentimentLabels.All.Select(label =>
            {
                var row = byLabel.FirstOrDefault(item => item.Label == label);
                var count = row?.Count ?? 0;
                return new SentimentLabelStatisticResponse
                {
                    Label = label,
                    Count = count,
                    Percentage = labeled == 0 ? 0 : Math.Round(100.0 * count / labeled, 1, MidpointRounding.AwayFromZero),
                    AverageRating = row?.AverageRating
                };
            }).ToList();

            var monthly = byMonth
                .GroupBy(row => new { row.Year, row.Month })
                .OrderBy(group => group.Key.Year)
                .ThenBy(group => group.Key.Month)
                .Select(group => new SentimentTrendPointResponse
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    PositiveCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Positive),
                    NeutralCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Neutral),
                    NegativeCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Negative)
                })
                .ToList();

            var activities = byActivity
                .GroupBy(row => row.ActivityId)
                .Select(group =>
                {
                    var count = group.Sum(row => row.Count);
                    var average = count == 0
                        ? (double?)null
                        : group.Sum(row => (row.AverageRating ?? 0) * row.Count) / count;
                    return new SentimentActivityBreakdownResponse
                    {
                        ActivityId = group.Key,
                        ActivityName = group.Select(row => row.Name).FirstOrDefault() ?? string.Empty,
                        PositiveCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Positive),
                        NeutralCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Neutral),
                        NegativeCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Negative),
                        AverageRating = average
                    };
                })
                .OrderBy(row => row.ActivityName)
                .ThenBy(row => row.ActivityId)
                .ToList();

            var animals = byAnimal
                .GroupBy(row => row.Id)
                .Select(group => new SentimentAnimalBreakdownResponse
                {
                    AnimalId = group.Key,
                    AnimalName = group.Select(row => row.Name).FirstOrDefault() ?? string.Empty,
                    PositiveCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Positive),
                    NeutralCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Neutral),
                    NegativeCount = CountFor(group, row => row.Label, row => row.Count, SentimentLabels.Negative)
                })
                .OrderBy(row => row.AnimalName)
                .ThenBy(row => row.AnimalId)
                .ToList();

            return new ReviewSentimentStatisticsResponse
            {
                TotalReviews = total,
                LabeledReviews = labeled,
                UnlabeledReviews = total - labeled,
                OverallAverageRating = total == 0 ? null : ratingWeight / total,
                Labels = labels,
                MonthlyTrend = monthly,
                Activities = activities,
                Animals = animals
            };
        }

        protected override IQueryable<Review> ApplyFilters(IQueryable<Review> query, ReviewSearchObject? search)
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

                if (!string.IsNullOrWhiteSpace(search.SentimentLabel))
                {
                    var label = SentimentLabels.Canonical(search.SentimentLabel);
                    if (label == null)
                    {
                        throw new ClientException("Sentiment label must be Positive, Neutral, or Negative.");
                    }

                    query = query.Where(review => review.SentimentLabel == label);
                }

                if (!string.IsNullOrWhiteSpace(search.FTS))
                {
                    var term = search.FTS.Trim();
                    query = query.Where(review =>
                        review.Comment.Contains(term)
                        || review.Activity.Name.Contains(term)
                        || review.User.FirstName.Contains(term)
                        || review.User.LastName.Contains(term)
                        || (review.User.FirstName + " " + review.User.LastName).Contains(term));
                }
            }

            return query;
        }

        protected override IQueryable<Review> ApplyDefaultOrder(IQueryable<Review> query)
        {
            return query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);
        }

        private void ApplySentiment(Review entity)
        {
            var result = _sentiment.Classify(entity.Comment);
            entity.SentimentLabel = result.Label;
            entity.SentimentScore = result.Score;
        }

        private static int CountFor<T>(IEnumerable<T> rows, Func<T, string?> label, Func<T, int> count, string expected)
        {
            var total = 0;
            foreach (var row in rows)
            {
                if (label(row) == expected)
                {
                    total += count(row);
                }
            }

            return total;
        }

        private int RequireCurrentUserId()
        {
            return _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
        }

        private bool CanModifyReview(int reviewUserId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == reviewUserId;
        }
    }
}
