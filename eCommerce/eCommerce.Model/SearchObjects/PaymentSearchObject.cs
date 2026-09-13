using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class PaymentSearchObject : BaseSearchObject
    {
        public int? UserId { get; set; }

        /// <summary>
        /// Filter payments by method.
        /// </summary>
        public PaymentMethod? Method { get; set; }

        /// <summary>
        /// Filter payments by status.
        /// </summary>
        public PaymentStatus? Status { get; set; }
    }
}
