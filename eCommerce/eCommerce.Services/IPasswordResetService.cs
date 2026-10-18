using eCommerce.Model.Requests;
using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface IPasswordResetService
    {
        Task<ForgotPasswordResponse> RequestAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

        Task ResetAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    }
}
