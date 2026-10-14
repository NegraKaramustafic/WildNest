using System.Collections.Generic;
using eCommerce.Model;
using System.Linq;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using eCommerce.Model.Exceptions;

namespace eCommerce.Services
{
    public class UserBadgeService : BaseReadService<UserBadge, UserBadgeResponse, UserBadgeSearchObject>, IUserBadgeService
    {

        private readonly IAuthenticatedUserAccessor _userAccessor;

        public UserBadgeService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IAuthenticatedUserAccessor userAccessor)
            : base(mapper, dbContext)
        {
            _userAccessor = userAccessor;
        }

        protected override async Task<IQueryable<UserBadge>> IncludeRelatedEntitiesAsync(UserBadgeSearchObject? search, IQueryable<UserBadge> query = null!)
        {
            query = query.Include(ub => ub.Badge);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<UserBadgeResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<UserBadge>()
                .AsNoTracking()
                .Include(ub => ub.Badge)
                .FirstOrDefaultAsync(ub => ub.Id == id);

            if (entity == null || !CanAccessUserBadge(entity.UserId))
            {
                throw new NotFoundException($"UserBadge with id {id} not found.");
            }

            return _mapper.Map<UserBadgeResponse>(entity);
        }

        public override async Task<PageResult<UserBadgeResponse>> GetAllAsync(UserBadgeSearchObject? search = null)
        {
            if (!_userAccessor.IsInRole(Roles.Admin) && search?.UserId.HasValue == true)
            {
                var userId = _userAccessor.GetUserId();
                if (!userId.HasValue || search.UserId.Value != userId.Value)
                {
                    throw new UnauthorizedAccessException("You are not allowed to view another user's badges.");
                }
            }

            return await base.GetAllAsync(search);
        }

        protected override IQueryable<UserBadge> ApplyFilters(IQueryable<UserBadge> query, UserBadgeSearchObject? search)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                if (search?.UserId.HasValue == true)
                {
                    query = query.Where(ub => ub.UserId == search.UserId.Value);
                }
            }
            else
            {
                var userId = _userAccessor.GetUserId();
                query = userId.HasValue
                    ? query.Where(ub => ub.UserId == userId.Value)
                    : query.Where(ub => false);
            }

            return query;
        }

        protected override IQueryable<UserBadge> ApplyDefaultOrder(IQueryable<UserBadge> query)
        {
            return query.OrderByDescending(ub => ub.EarnedAt).ThenByDescending(ub => ub.Id);
        }

        private bool CanAccessUserBadge(int badgeUserId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == badgeUserId;
        }
    }
}
