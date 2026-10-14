using eCommerce.Common.Services.CryptoService;
using eCommerce.Model;
using eCommerce.Model.Access;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.Services
{
    public class UserService : BaseCRUDService<User, UserResponse, UserSearch, UserInsertRequest, UserUpdateRequest>, IUserService
    {
        private readonly ICryptoService _cryptoService;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly ISecurityStampCache _securityStampCache;

        public UserService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<UserInsertRequest> insertValidator,
            IValidator<UserUpdateRequest> updateValidator,
            ICryptoService cryptoService,
            IAuthenticatedUserAccessor userAccessor,
            ISecurityStampCache securityStampCache)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _cryptoService = cryptoService;
            _userAccessor = userAccessor;
            _securityStampCache = securityStampCache;
        }


        protected override IQueryable<User> ApplyFilters(IQueryable<User> query, UserSearch? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Email))
                {
                    var email = search.Email.Trim();
                    query = query.Where(u => u.Email.Contains(email));
                }

                if (!string.IsNullOrWhiteSpace(search.Username))
                {
                    var username = search.Username.Trim();
                    query = query.Where(u => u.Username.Contains(username));
                }

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    var name = search.Name.Trim();
                    query = query.Where(u => u.FirstName.Contains(name) || u.LastName.Contains(name));
                }

                if (search.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == search.IsActive.Value);
                }
            }

            return query;
        }

        protected override IQueryable<User> ApplyDefaultOrder(IQueryable<User> query)
        {
            return query.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id);
        }

        protected override User MapInsertRequestToEntity(UserInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);

            // Handle password hashing for User entity
            var salt = _cryptoService.GenerateSlat();
            entity.PasswordSalt = salt;
            entity.PasswordHash = _cryptoService.GenerateHash(request.Password, salt);
            entity.SecurityStamp = NewSecurityStamp();

            return entity;
        }

        public override async Task<UserResponse> InsertAsync(UserInsertRequest request)
        {
            // let FluentValidation throw if the request isn't valid; the exception filter will
            // convert the resulting ValidationException into the standard error format.
            await _insertValidator.ValidateAndThrowAsync(request);

            // Check if email or username already exists
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new ClientException($"Email '{request.Email}' is already in use.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
            {
                throw new ClientException($"Username '{request.Username}' is already in use.");
            }

            var entity = MapInsertRequestToEntity(request);
            entity.CreatedAt = DateTime.UtcNow;
            if (!entity.UserRoles.Any())
            {
                entity.UserRoles.Add(new UserRole
                {
                    RoleId = await GetCustomerRoleIdAsync(),
                    DateAssigned = DateTime.UtcNow
                });
            }

            _dbContext.Users.Add(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<UserResponse>(entity);
        }


        public override async Task<UserResponse> GetByIdAsync(int id)
        {
            if (!CanAccessUser(id))
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            return await base.GetByIdAsync(id);
        }

        public override async Task<UserResponse> UpdateAsync(int id, UserUpdateRequest request)
        {
            if (!CanAccessUser(id))
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            await _updateValidator.ValidateAndThrowAsync(request);

            var entity = await _dbContext.Users.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            var existingIsActive = entity.IsActive;

            // Check if email or username already exists
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
            {
                throw new ClientException($"Email '{request.Email}' is already in use.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username && u.Id != id))
            {
                throw new ClientException($"Username '{request.Username}' is already in use.");
            }

            MapUpdateRequestToEntity(request, entity);
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                entity.IsActive = existingIsActive;
            }

            var deactivated = existingIsActive && !entity.IsActive;
            if (deactivated)
            {
                await RotateSecurityStampAsync(entity);
            }

            _dbContext.Users.Update(entity);
            await _dbContext.SaveChangesAsync();
            if (deactivated)
            {
                _securityStampCache.Invalidate(entity.Id);
            }

            return _mapper.Map<UserResponse>(entity);
        }

        public override async Task DeleteAsync(int id)
        {
            var currentUserId = _userAccessor.GetUserId();
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                throw new ClientException("You cannot delete your own account.");
            }

            var entity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            var softDeleted = false;
            await DbTransactions.ExecuteAsync(_dbContext, async () =>
            {
                if (await HasRelatedRowsAsync(id))
                {
                    entity.IsActive = false;
                    await RotateSecurityStampAsync(entity);
                    softDeleted = true;
                }
                else
                {
                    var notifications = await _dbContext.Notifications.Where(n => n.UserId == id).ToListAsync();
                    _dbContext.Notifications.RemoveRange(notifications);
                    var badges = await _dbContext.UserBadges.Where(b => b.UserId == id).ToListAsync();
                    _dbContext.UserBadges.RemoveRange(badges);
                    _dbContext.Users.Remove(entity);
                }

                await _dbContext.SaveChangesAsync();
            });

            if (softDeleted)
            {
                _securityStampCache.Invalidate(entity.Id);
            }
        }

        public async Task<UserSensitiveResponse?> GetByUsernameAsync(string username)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            UserSensitiveResponse? response = null;

            if (user != null)
            {
                response = _mapper.Map<UserSensitiveResponse>(user);
                response.Role = user.UserRoles.FirstOrDefault()?.Role.Name;
            }

            return response;
        }

        public async Task<UserResponse?> GetWithRoleByIdAsync(int id)
        {
            var user = await _dbContext.Users
               .AsNoTracking()
               .Include(u => u.UserRoles)
               .ThenInclude(ur => ur.Role)
               .FirstOrDefaultAsync(u => u.Id == id);

            UserResponse? response = null;

            if (user != null)
            {
                response = _mapper.Map<UserResponse>(user);
                response.Role = user.UserRoles.First().Role.Name;
            }

            return response;
        }

        public async Task ChangePasswordAsync(UserPasswordChangeRequest request)
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new ClientException("User id claim is missing.");

            if (request.Id != 0 && request.Id != userId)
            {
                throw new ClientException("You can only change your own password.");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("User not found");

            if (!_cryptoService.Verify(user.PasswordHash, user.PasswordSalt, request.Password))
                throw new ClientException("Wrong credential");

            if (!request.NewPassword.Equals(request.ConfirmNewPassword))
                throw new ClientException("Password confirmation doesn't match the new password.");

            user.PasswordSalt = _cryptoService.GenerateSlat();
            user.PasswordHash = _cryptoService.GenerateHash(request.NewPassword, user.PasswordSalt);
            await RotateSecurityStampAsync(user);

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            _securityStampCache.Invalidate(user.Id);
        }

        public async Task<string> GetSecurityStampAsync(int userId)
        {
            var stamp = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.SecurityStamp)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(stamp))
            {
                throw new ClientException("User security stamp is missing.");
            }

            return stamp;
        }

        public async Task RevokeSessionsAsync(int userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            await RotateSecurityStampAsync(user);
            await _dbContext.SaveChangesAsync();
            _securityStampCache.Invalidate(userId);
        }

        private async Task RotateSecurityStampAsync(User user)
        {
            user.SecurityStamp = NewSecurityStamp();
            var tokens = await _dbContext.RefreshTokens.Where(token => token.UserId == user.Id).ToListAsync();
            _dbContext.RefreshTokens.RemoveRange(tokens);
        }

        private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

        private bool CanAccessUser(int userId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var currentUserId = _userAccessor.GetUserId();
            return currentUserId.HasValue && currentUserId.Value == userId;
        }

        private async Task<int> GetCustomerRoleIdAsync()
        {
            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == Roles.Customer);

            if (role == null)
            {
                throw new ClientException("Customer role is not configured.");
            }

            return role.Id;
        }

        private async Task<bool> HasRelatedRowsAsync(int userId)
        {
            if (await _dbContext.Tickets.AnyAsync(t => t.UserId == userId || t.VerifiedByUserId == userId))
            {
                return true;
            }

            if (await _dbContext.Reservations.AnyAsync(r => r.UserId == userId))
            {
                return true;
            }

            if (await _dbContext.Payments.AnyAsync(p => p.UserId == userId))
            {
                return true;
            }

            if (await _dbContext.Adoptions.AnyAsync(a => a.UserId == userId))
            {
                return true;
            }

            if (await _dbContext.Donations.AnyAsync(d => d.UserId == userId))
            {
                return true;
            }

            return await _dbContext.Reviews.AnyAsync(r => r.UserId == userId);
        }
    }
}
