using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class AdoptionSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter adoptions by owner user id (Admin only; non-admins are always scoped to themselves).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Filter adoptions by animal id.
        /// </summary>
        public int? AnimalId { get; set; }

        /// <summary>
        /// Filter adoptions by status.
        /// </summary>
        public AdoptionStatus? Status { get; set; }
    }
}
