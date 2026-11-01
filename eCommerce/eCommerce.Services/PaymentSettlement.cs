using eCommerce.Model.Enums;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    internal static class PaymentSettlement
    {
        public static async Task<bool> IsCompletedOrRefundedAsync(ECommerceDbContext db, int? paymentId)
        {
            if (!paymentId.HasValue)
            {
                return false;
            }

            var status = await db.Payments.AsNoTracking()
                .Where(payment => payment.Id == paymentId.Value)
                .Select(payment => (PaymentStatus?)payment.Status)
                .FirstOrDefaultAsync();

            return status is PaymentStatus.Completed or PaymentStatus.Refunded;
        }
    }
}
