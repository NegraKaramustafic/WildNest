using eCommerce.Common.Services.CryptoService;
using eCommerce.Model;
using eCommerce.Model.Access;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using eCommerce.Services.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            ISecurityStampCache securityStampCache,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
            _cryptoService = cryptoService;
            _userAccessor = userAccessor;
            _securityStampCache = securityStampCache;
        }

        protected override string NotFoundMessage => "User not found.";


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

                if (!string.IsNullOrWhiteSpace(search.Role))
                {
                    var role = RoleNames.IsKnown(search.Role)
                        ? RoleNames.Canonical(search.Role)
                        : search.Role.Trim();
                    query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == role));
                }

                if (!string.IsNullOrWhiteSpace(search.FTS))
                {
                    var term = search.FTS.Trim();
                    query = query.Where(u =>
                        u.FirstName.Contains(term)
                        || u.LastName.Contains(term)
                        || u.Username.Contains(term)
                        || u.Email.Contains(term)
                        || (u.FirstName + " " + u.LastName).Contains(term));
                }
            }

            return query;
        }

        protected override Task<IQueryable<User>> IncludeRelatedEntitiesAsync(UserSearch? search, IQueryable<User> query = null!)
        {
            query = query.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
            return Task.FromResult(query);
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

            request.ProfileImageBase64 = ImageContent.NormalizeProfileImage(request.ProfileImageBase64);
            var entity = MapInsertRequestToEntity(request);
            entity.CreatedAt = DateTime.UtcNow;
            var roleName = Roles.Customer;
            if (_userAccessor.IsInRole(Roles.Admin) && !string.IsNullOrWhiteSpace(request.Role))
            {
                roleName = RoleNames.Canonical(request.Role);
            }

            entity.UserRoles.Add(new UserRole
            {
                RoleId = await GetRoleIdAsync(roleName),
                DateAssigned = DateTime.UtcNow
            });

            _dbContext.Users.Add(entity);
            await _dbContext.SaveChangesAsync();

            var response = _mapper.Map<UserResponse>(entity);
            response.Role = roleName;
            return response;
        }


        public override async Task<UserResponse> GetByIdAsync(int id)
        {
            if (!CanAccessUser(id))
            {
                ThrowNotFound("User", id, "User not found.");
            }

            var entity = await _dbContext.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (entity == null)
            {
                ThrowNotFound("User", id, "User not found.");
            }

            return _mapper.Map<UserResponse>(entity);
        }

        public override async Task<UserResponse> UpdateAsync(int id, UserUpdateRequest request)
        {
            if (!CanAccessUser(id))
            {
                ThrowNotFound("User", id, "User not found.");
            }

            await _updateValidator.ValidateAndThrowAsync(request);

            var entity = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
            {
                ThrowNotFound("User", id, "User not found.");
            }

            var existingIsActive = entity.IsActive;
            var currentUserId = _userAccessor.GetUserId();
            var isAdmin = _userAccessor.IsInRole(Roles.Admin);
            if (isAdmin && existingIsActive && !request.IsActive && currentUserId == id)
            {
                throw new ClientException("You cannot block your own account.");
            }

            if (isAdmin && !string.IsNullOrWhiteSpace(request.Role))
            {
                var requestedRole = RoleNames.Canonical(request.Role);
                if (currentUserId == id && requestedRole != Roles.Admin)
                {
                    throw new ClientException("You cannot remove your own administrator role.");
                }
            }

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
            if (!isAdmin)
            {
                entity.IsActive = existingIsActive;
            }

            if (request.ProfileImageBase64 != null)
            {
                entity.ProfileImageBase64 = ImageContent.NormalizeProfileImage(request.ProfileImageBase64);
            }

            var passwordChanged = ApplyPasswordIfProvided(entity, request.Password);
            if (isAdmin && !string.IsNullOrWhiteSpace(request.Role))
            {
                await ReplaceRoleAsync(entity, RoleNames.Canonical(request.Role));
            }

            var deactivated = existingIsActive && !entity.IsActive;
            if (deactivated || passwordChanged)
            {
                await RotateSecurityStampAsync(entity);
            }

            _dbContext.Users.Update(entity);
            await _dbContext.SaveChangesAsync();
            if (deactivated || passwordChanged)
            {
                _securityStampCache.Invalidate(entity.Id);
            }

            return await GetByIdAsync(id);
        }

        public async Task<UserResponse> SetActiveAsync(int id, bool isActive)
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                throw new UnauthorizedAccessException("Only an administrator can block or unblock a user.");
            }

            var entity = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
            {
                ThrowNotFound("User", id, "User not found.");
            }

            var currentUserId = _userAccessor.GetUserId();
            if (!isActive && currentUserId == id)
            {
                throw new ClientException("You cannot block your own account.");
            }

            var deactivated = entity.IsActive && !isActive;
            entity.IsActive = isActive;
            if (deactivated)
            {
                await RotateSecurityStampAsync(entity);
            }

            await _dbContext.SaveChangesAsync();
            if (deactivated)
            {
                _securityStampCache.Invalidate(entity.Id);
            }

            return _mapper.Map<UserResponse>(entity);
        }

        public void EnsureActiveForSignIn(bool isActive)
        {
            if (!isActive)
            {
                throw new ClientException("This account is blocked. Sign-in is not allowed.");
            }
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
                ThrowNotFound("User", id, "User not found.");
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
                response.Role = user.UserRoles.FirstOrDefault()?.Role.Name ?? string.Empty;
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
                ThrowNotFound("User", userId, "User not found.");

            if (!_cryptoService.Verify(user.PasswordHash, user.PasswordSalt, request.Password))
                throw new ClientException("Wrong credential.");

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
                ThrowNotFound("User", userId, "User not found.");
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

        private bool ApplyPasswordIfProvided(User entity, string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            entity.PasswordSalt = _cryptoService.GenerateSlat();
            entity.PasswordHash = _cryptoService.GenerateHash(password.Trim(), entity.PasswordSalt);
            return true;
        }

        private async Task ReplaceRoleAsync(User entity, string roleName)
        {
            var roleId = await GetRoleIdAsync(roleName);
            if (entity.UserRoles.Count == 1 && entity.UserRoles.First().RoleId == roleId)
            {
                return;
            }

            _dbContext.UserRoles.RemoveRange(entity.UserRoles);
            entity.UserRoles.Clear();
            entity.UserRoles.Add(new UserRole
            {
                UserId = entity.Id,
                RoleId = roleId,
                DateAssigned = DateTime.UtcNow
            });
        }

        private async Task<int> GetRoleIdAsync(string roleName)
        {
            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (role == null)
            {
                throw new ClientException($"{roleName} role is not configured.");
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
