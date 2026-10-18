using eCommerce.Model;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using eCommerce.Services.Reports;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class ReportService : IReportService
    {
        public const string EmptyReservationsMessage = "No reservations match these filters.";
        public const string EmptyDonationsMessage = "No donations match these filters.";

        private readonly ECommerceDbContext _dbContext;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly IValidator<ReservationReportRequest> _reservationValidator;
        private readonly IValidator<DonationReportRequest> _donationValidator;
        private readonly IPdfReportRenderer _renderer;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ECommerceDbContext dbContext,
            IAuthenticatedUserAccessor userAccessor,
            IValidator<ReservationReportRequest> reservationValidator,
            IValidator<DonationReportRequest> donationValidator,
            IPdfReportRenderer renderer,
            ILogger<ReportService> logger)
        {
            _dbContext = dbContext;
            _userAccessor = userAccessor;
            _reservationValidator = reservationValidator;
            _donationValidator = donationValidator;
            _renderer = renderer;
            _logger = logger;
        }

        public async Task<GeneratedPdf> ReservationsPdfAsync(ReservationReportRequest request)
        {
            var document = await BuildReservationReportAsync(request);
            return new GeneratedPdf(
                _renderer.RenderReservations(document),
                $"reservations-{document.DateFrom:yyyy-MM-dd}-to-{document.DateTo:yyyy-MM-dd}.pdf");
        }

        public async Task<GeneratedPdf> DonationsPdfAsync(DonationReportRequest request)
        {
            var document = await BuildDonationReportAsync(request);
            return new GeneratedPdf(
                _renderer.RenderDonations(document),
                $"donations-by-partner-{document.DateFrom:yyyy-MM-dd}-to-{document.DateTo:yyyy-MM-dd}.pdf");
        }

        public async Task<ReservationReportDocument> BuildReservationReportAsync(ReservationReportRequest request)
        {
            EnsureAdmin();
            await _reservationValidator.ValidateAndThrowAsync(request);

            string? activityName = null;
            if (request.ActivityId.HasValue)
            {
                activityName = await _dbContext.Activities.AsNoTracking()
                    .Where(activity => activity.Id == request.ActivityId.Value)
                    .Select(activity => activity.Name)
                    .FirstOrDefaultAsync();
                if (activityName == null)
                {
                    _logger.LogWarning("Activity {ActivityId} was not found.", request.ActivityId);
                    throw new NotFoundException("Activity not found.");
                }
            }

            var from = request.DateFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusive = request.DateTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var filtered = _dbContext.Reservations.AsNoTracking()
                .Where(reservation => reservation.CreatedAt >= from && reservation.CreatedAt < toExclusive);

            if (request.ActivityId.HasValue)
            {
                filtered = filtered.Where(reservation => reservation.Schedule.ActivityId == request.ActivityId.Value);
            }

            if (request.Status.HasValue)
            {
                filtered = filtered.Where(reservation => reservation.Status == request.Status.Value);
            }

            var total = await filtered.CountAsync();
            if (total == 0)
            {
                throw new ClientException(EmptyReservationsMessage);
            }

            var statusRows = await filtered
                .GroupBy(reservation => reservation.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync();

            var guests = await filtered.SumAsync(reservation => (int?)reservation.GuestCount) ?? 0;
            var revenue = await filtered
                .Where(reservation => reservation.Payment != null && reservation.Payment.Status == PaymentStatus.Completed)
                .SumAsync(reservation => (decimal?)reservation.TotalPrice) ?? 0m;

            var rows = await filtered
                .OrderByDescending(reservation => reservation.CreatedAt)
                .ThenByDescending(reservation => reservation.Id)
                .Select(reservation => new ReservationReportRow
                {
                    Code = reservation.Code,
                    CustomerName = reservation.User.FirstName + " " + reservation.User.LastName,
                    ActivityName = reservation.Schedule.Activity.Name,
                    ScheduleDate = reservation.Schedule.Date,
                    ScheduleStartTime = reservation.Schedule.StartTime,
                    GuestCount = reservation.GuestCount,
                    Status = reservation.Status,
                    TotalPrice = reservation.TotalPrice,
                    CreatedAt = reservation.CreatedAt
                })
                .ToListAsync();

            var counts = Enum.GetValues<ReservationStatus>()
                .Select(status => new ReservationStatusCount
                {
                    Status = status,
                    Count = statusRows.FirstOrDefault(row => row.Status == status)?.Count ?? 0
                })
                .ToList();

            return new ReservationReportDocument
            {
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                ActivityName = activityName,
                Status = request.Status,
                GeneratedByName = await GeneratedByNameAsync(),
                GeneratedAtUtc = DateTime.UtcNow,
                TotalReservations = total,
                TotalGuests = guests,
                Revenue = revenue,
                StatusCounts = counts,
                Rows = rows
            };
        }

        public async Task<DonationReportDocument> BuildDonationReportAsync(DonationReportRequest request)
        {
            EnsureAdmin();
            await _donationValidator.ValidateAndThrowAsync(request);

            string? partnerName = null;
            if (request.ZooPartnerId.HasValue)
            {
                partnerName = await _dbContext.ZooPartners.AsNoTracking()
                    .Where(partner => partner.Id == request.ZooPartnerId.Value)
                    .Select(partner => partner.Name)
                    .FirstOrDefaultAsync();
                if (partnerName == null)
                {
                    _logger.LogWarning("Zoo partner {ZooPartnerId} was not found.", request.ZooPartnerId);
                    throw new NotFoundException("Zoo partner not found.");
                }
            }

            var from = request.DateFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusive = request.DateTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var filtered = _dbContext.Donations.AsNoTracking()
                .Where(donation => donation.CreatedAt >= from && donation.CreatedAt < toExclusive);

            if (request.ZooPartnerId.HasValue)
            {
                filtered = filtered.Where(donation => donation.ZooPartnerId == request.ZooPartnerId.Value);
            }

            var completed = filtered.Where(donation =>
                donation.Payment != null && donation.Payment.Status == PaymentStatus.Completed);

            var partners = await completed
                .GroupBy(donation => new
                {
                    donation.ZooPartnerId,
                    donation.ZooPartner.Name,
                    donation.ZooPartner.Location
                })
                .Select(group => new DonationPartnerTotal
                {
                    ZooPartnerId = group.Key.ZooPartnerId,
                    PartnerName = group.Key.Name,
                    Location = group.Key.Location,
                    DonationCount = group.Count(),
                    TotalAmount = group.Sum(donation => donation.Amount)
                })
                .ToListAsync();

            partners = partners
                .OrderByDescending(row => row.TotalAmount)
                .ThenBy(row => row.PartnerName)
                .ThenBy(row => row.ZooPartnerId)
                .ToList();

            if (partners.Count == 0)
            {
                throw new ClientException(EmptyDonationsMessage);
            }

            var lineRows = await filtered
                .OrderByDescending(donation => donation.CreatedAt)
                .ThenByDescending(donation => donation.Id)
                .Select(donation => new
                {
                    PartnerName = donation.ZooPartner.Name,
                    Location = donation.ZooPartner.Location,
                    donation.Amount,
                    Status = donation.Payment == null ? (PaymentStatus?)null : donation.Payment.Status,
                    donation.CreatedAt
                })
                .ToListAsync();

            return new DonationReportDocument
            {
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                PartnerName = partnerName,
                GeneratedByName = await GeneratedByNameAsync(),
                GeneratedAtUtc = DateTime.UtcNow,
                TotalDonations = partners.Sum(row => row.DonationCount),
                GrandTotal = partners.Sum(row => row.TotalAmount),
                Partners = partners,
                Lines = lineRows.Select(row => new DonationReportLine
                {
                    PartnerName = row.PartnerName,
                    Location = row.Location,
                    Amount = row.Amount,
                    PaymentStatus = row.Status == null ? "No payment" : PaymentStatusLabel(row.Status.Value),
                    CreatedAt = DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc)
                }).ToList()
            };
        }

        private static string PaymentStatusLabel(PaymentStatus status) => status switch
        {
            PaymentStatus.Pending => "Pending",
            PaymentStatus.Completed => "Completed",
            PaymentStatus.Failed => "Failed",
            PaymentStatus.Refunded => "Refunded",
            _ => status.ToString()
        };

        private void EnsureAdmin()
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                _logger.LogWarning("A non-administrator requested a report.");
                throw new UnauthorizedAccessException("Only administrators can generate reports.");
            }
        }

        private async Task<string> GeneratedByNameAsync()
        {
            var userId = _userAccessor.GetUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("A report was requested without a user id claim.");
                throw new UnauthorizedAccessException("Only administrators can generate reports.");
            }

            var name = await _dbContext.Users.AsNoTracking()
                .Where(user => user.Id == userId.Value)
                .Select(user => user.FirstName + " " + user.LastName)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Report generator {UserId} was not found.", userId);
                return "Administrator";
            }

            return name.Trim();
        }
    }
}
