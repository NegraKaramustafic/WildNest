using eCommerce.Model.Responses;

namespace eCommerce.Services.Reports
{
    public interface IPdfReportRenderer
    {
        byte[] RenderReservations(ReservationReportDocument document);

        byte[] RenderDonations(DonationReportDocument document);
    }
}
