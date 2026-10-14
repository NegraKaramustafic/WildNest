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
    public class NotificationService : BaseReadService<Notification, NotificationResponse, NotificationSearchObject>, INotificationService
    {

        private readonly IAuthenticatedUserAccessor _userAccessor;

        public NotificationService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IAuthenticatedUserAccessor userAccessor)
            : base(mapper, dbContext)
        {
            _userAccessor = userAccessor;
        }

        protected override async Task<IQueryable<Notification>> IncludeRelatedEntitiesAsync(NotificationSearchObject? search, IQueryable<Notification> query = null!)
        {
            query = query.Include(n => n.User);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<NotificationResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Notification>()
                .AsNoTracking()
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (entity == null || !CanAccessNotification(entity.UserId))
            {
                throw new NotFoundException($"Notification with id {id} not found.");
            }

            return _mapper.Map<NotificationResponse>(entity);
        }

        public override async Task<PageResult<NotificationResponse>> GetAllAsync(NotificationSearchObject? search = null)
        {
            search ??= new NotificationSearchObject();

            if (!_userAccessor.IsInRole(Roles.Admin) && search.UserId.HasValue)
            {
                var userId = _userAccessor.GetUserId();
                if (!userId.HasValue || search.UserId.Value != userId.Value)
                {
                    throw new UnauthorizedAccessException("You are not allowed to view another user's notifications.");
                }
            }

            return await base.GetAllAsync(search);
        }

        protected override IQueryable<Notification> ApplyFilters(IQueryable<Notification> query, NotificationSearchObject? search)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                if (search?.UserId.HasValue == true)
                {
                    query = query.Where(n => n.UserId == search.UserId.Value);
                }
            }
            else
            {
                var userId = _userAccessor.GetUserId();
                query = userId.HasValue
                    ? query.Where(n => n.UserId == userId.Value)
                    : query.Where(n => false);
            }

            if (search != null)
            {
                if (search.IsRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == search.IsRead.Value);
                }

                if (search.Type.HasValue)
                {
                    query = query.Where(n => n.Type == search.Type.Value);
                }
            }

            return query;
        }

        protected override IQueryable<Notification> ApplyDefaultOrder(IQueryable<Notification> query)
        {
            return query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id);
        }

        public async Task<int> GetUnreadCountAsync(int? userId)
        {
            var targetUserId = ResolveTargetUserId(userId);
            return await _dbContext.Notifications.CountAsync(n => n.UserId == targetUserId && !n.IsRead);
        }

        public async Task<NotificationResponse> MarkReadAsync(int id)
        {
            var entity = await _dbContext.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (entity == null || !CanAccessNotification(entity.UserId))
            {
                throw new NotFoundException($"Notification with id {id} not found.");
            }

            if (!entity.IsRead)
            {
                entity.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }

            return _mapper.Map<NotificationResponse>(entity);
        }

        public async Task<int> MarkAllReadAsync()
        {
            var userId = RequireCurrentUserId();
            var unread = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
            }

            if (unread.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
            }

            return unread.Count;
        }

        private int ResolveTargetUserId(int? requestedUserId)
        {
            var currentUserId = RequireCurrentUserId();

            if (!requestedUserId.HasValue)
            {
                return currentUserId;
            }

            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return requestedUserId.Value;
            }

            if (requestedUserId.Value != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not allowed to view another user's notifications.");
            }

            return currentUserId;
        }

        private int RequireCurrentUserId()
        {
            return _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
        }

        private bool CanAccessNotification(int notificationUserId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == notificationUserId;
        }
    }
}
