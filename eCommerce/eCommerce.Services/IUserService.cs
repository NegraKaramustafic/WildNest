using eCommerce.Model.Access;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IUserService : IBaseCRUDService<UserResponse, UserSearch, UserInsertRequest, UserUpdateRequest>
    {
        Task<UserSensitiveResponse?> GetByUsernameAsync(string username);
        Task<UserResponse?> GetWithRoleByIdAsync(int id);
        Task ChangePasswordAsync(UserPasswordChangeRequest request);
        Task<string> GetSecurityStampAsync(int userId);
        Task RevokeSessionsAsync(int userId);
        Task<UserResponse> SetActiveAsync(int id, bool isActive);
        void EnsureActiveForSignIn(bool isActive);
    }
}
