using eCommerce.Model.Requests;
using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface IReportService
    {
        Task<ReservationReportDocument> BuildReservationReportAsync(ReservationReportRequest request);

        Task<DonationReportDocument> BuildDonationReportAsync(DonationReportRequest request);

        Task<GeneratedPdf> ReservationsPdfAsync(ReservationReportRequest request);

        Task<GeneratedPdf> DonationsPdfAsync(DonationReportRequest request);
    }

    public sealed record GeneratedPdf(byte[] Content, string FileName);
}
