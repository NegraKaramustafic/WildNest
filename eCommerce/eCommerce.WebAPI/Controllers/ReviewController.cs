using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Route("api/Review")]
public class ReviewController : BaseCRUDController<ReviewResponse, ReviewSearchObject, ReviewInsertRequest, ReviewUpdateRequest, IReviewService>
{
    public ReviewController(IReviewService reviewService) : base(reviewService)
    {
    }
}
