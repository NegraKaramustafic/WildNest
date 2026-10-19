using eCommerce.Model;
using eCommerce.Model.Enums;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Database
{
    /// <summary>
    /// Inserts demo rows whose dates must stay relative to the day the database is created.
    /// Catalog rows stay in HasData. This seeder skips any row it has already inserted.
    /// </summary>
    public static class DemoDataSeeder
    {
        public const int HorizonWeeks = 6;

        public readonly record struct PlannedSchedule(
            int ActivityId,
            DateOnly Date,
            TimeOnly StartTime,
            TimeOnly EndTime,
            int Capacity,
            ScheduleStatus Status);

        private readonly record struct CourseAccount(
            string Username,
            string FirstName,
            string LastName,
            string Email,
            string RoleName,
            string PasswordSalt,
            string PasswordHash);

        private static readonly CourseAccount[] CourseAccounts =
        {
            new("desktop", "Dana", "Cole", "desktop@gmail.com", Roles.Admin,
                "1n8sEVFjBNPkHtt/6VR3/A==", "3Ful4/7xLv77tu6Fs6p0tl7vS7M="),
            new("mobile", "Mina", "Cole", "mobile@gmail.com", Roles.Customer,
                "+fAqV1z5r0vuJ9WqgcX81g==", "s20h1z2CJEvr0JsePi9UZNuDYo0="),
            new("Admin", "Avery", "Cole", "admin.role@gmail.com", Roles.Admin,
                "of4G1qAWlOLww3TEIOMs6g==", "OQypa9ltl269zb6ywFUVH5/IYCg="),
            new("Customer", "Casey", "Cole", "customer.role@gmail.com", Roles.Customer,
                "mGojMlytT0zeg+6n2IDT+Q==", "/6Z2exOrl/lYXsnltd1/wCp4Ddo=")
        };

        public static async Task SeedAsync(
            ECommerceDbContext db,
            DateOnly today,
            CancellationToken cancellationToken = default)
        {
            if (db.Database.CurrentTransaction != null)
            {
                await SeedCoreAsync(db, today, cancellationToken);
                return;
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await SeedCoreAsync(db, today, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private static async Task SeedCoreAsync(
            ECommerceDbContext db,
            DateOnly today,
            CancellationToken cancellationToken)
        {
            await EnsureCourseAccountsAsync(db, cancellationToken);
            await EnsureSchedulesAsync(db, today, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await EnsureReservationsAsync(db, today, cancellationToken);
            await EnsureTicketsAsync(db, today, cancellationToken);
            await EnsureDonationsAsync(db, cancellationToken);
            await EnsureAdoptionsAsync(db, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await EnsureBadgesAsync(db, cancellationToken);
            await EnsureNotificationsAsync(db, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Schedules from the Monday of the current week through the next six weeks.
        /// Days before <paramref name="today"/> are omitted.
        /// </summary>
        public static IReadOnlyList<PlannedSchedule> Plan(DateOnly today)
        {
            var monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var patterns = new (int DayOffset, int ActivityId, int StartHour, int StartMinute, int EndHour, int EndMinute, int Capacity)[]
            {
                (1, 1, 9, 0, 10, 30, 15),
                (2, 5, 10, 0, 11, 0, 12),
                (2, 6, 9, 0, 10, 0, 10),
                (3, 3, 11, 0, 13, 0, 12),
                (3, 8, 11, 0, 13, 0, 14),
                (4, 9, 14, 0, 15, 30, 10),
                (5, 7, 17, 0, 18, 30, 16),
                (5, 2, 20, 0, 22, 0, 20),
                (6, 10, 10, 0, 11, 0, 8)
            };

            var slots = new List<PlannedSchedule>();
            for (var week = 0; week < HorizonWeeks; week++)
            {
                foreach (var pattern in patterns)
                {
                    var date = monday.AddDays(week * 7 + pattern.DayOffset);
                    if (date < today)
                    {
                        continue;
                    }

                    slots.Add(new PlannedSchedule(
                        pattern.ActivityId,
                        date,
                        new TimeOnly(pattern.StartHour, pattern.StartMinute),
                        new TimeOnly(pattern.EndHour, pattern.EndMinute),
                        pattern.Capacity,
                        ScheduleStatus.Active));
                }

                if (week == 1)
                {
                    var cancelledDate = monday.AddDays(7 + 2);
                    if (cancelledDate >= today)
                    {
                        slots.Add(new PlannedSchedule(
                            2,
                            cancelledDate,
                            new TimeOnly(18, 0),
                            new TimeOnly(20, 0),
                            20,
                            ScheduleStatus.Cancelled));
                    }
                }
            }

            return slots;
        }

        private static async Task EnsureCourseAccountsAsync(ECommerceDbContext db, CancellationToken cancellationToken)
        {
            var roles = await db.Roles.ToDictionaryAsync(role => role.Name, role => role.Id, cancellationToken);
            foreach (var account in CourseAccounts)
            {
                if (await db.Users.AnyAsync(user => user.Username == account.Username, cancellationToken))
                {
                    continue;
                }

                if (!roles.TryGetValue(account.RoleName, out var roleId))
                {
                    continue;
                }

                var user = new User
                {
                    FirstName = account.FirstName,
                    LastName = account.LastName,
                    Email = account.Email,
                    Username = account.Username,
                    PasswordHash = account.PasswordHash,
                    PasswordSalt = account.PasswordSalt,
                    IsActive = true,
                    SecurityStamp = "seedstamp-" + account.Username,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    PhoneNumber = null
                };
                db.Users.Add(user);
                db.UserRoles.Add(new UserRole
                {
                    User = user,
                    RoleId = roleId,
                    DateAssigned = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                });
            }
        }

        private static async Task EnsureSchedulesAsync(ECommerceDbContext db, DateOnly today, CancellationToken cancellationToken)
        {
            var planned = Plan(today).ToList();
            planned.AddRange(PastSchedules(today));
            foreach (var slot in planned)
            {
                var exists = await db.Schedules.AnyAsync(
                    schedule => schedule.ActivityId == slot.ActivityId
                        && schedule.Date == slot.Date
                        && schedule.StartTime == slot.StartTime,
                    cancellationToken);
                if (exists)
                {
                    continue;
                }

                db.Schedules.Add(new Schedule
                {
                    ActivityId = slot.ActivityId,
                    Date = slot.Date,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    CapacityLeft = slot.Capacity,
                    Status = slot.Status
                });
            }
        }

        private static IEnumerable<PlannedSchedule> PastSchedules(DateOnly today)
        {
            yield return Past(today, 21, 1, 9, 0, 10, 30, 15);
            yield return Past(today, 18, 3, 11, 0, 13, 0, 12);
            yield return Past(today, 14, 2, 20, 0, 22, 0, 20);
            yield return Past(today, 10, 5, 10, 0, 11, 0, 12);
            yield return Past(today, 7, 6, 9, 0, 10, 0, 10);
        }

        private static PlannedSchedule Past(
            DateOnly today,
            int daysAgo,
            int activityId,
            int startHour,
            int startMinute,
            int endHour,
            int endMinute,
            int capacity)
        {
            return new PlannedSchedule(
                activityId,
                today.AddDays(-daysAgo),
                new TimeOnly(startHour, startMinute),
                new TimeOnly(endHour, endMinute),
                capacity,
                ScheduleStatus.Active);
        }

        private static async Task EnsureReservationsAsync(ECommerceDbContext db, DateOnly today, CancellationToken cancellationToken)
        {
            var customer1 = await UserIdAsync(db, "customer1", cancellationToken);
            var customer2 = await UserIdAsync(db, "customer2", cancellationToken);
            var mobile = await UserIdAsync(db, "mobile", cancellationToken);
            var admin = await UserIdAsync(db, "admin1", cancellationToken);
            if (customer1 == null || customer2 == null || mobile == null || admin == null)
            {
                return;
            }

            var prices = await db.Activities.AsNoTracking()
                .ToDictionaryAsync(activity => activity.Id, activity => activity.Price, cancellationToken);

            await AddReservationAsync(db, "SEED-R-DONE", customer1.Value, 1, today.AddDays(-21), new TimeOnly(9, 0), 2,
                ReservationStatus.Completed, prices, admin.Value, null, null, PaymentStatus.Completed, "seed-reservation-done", cancellationToken);
            await AddReservationAsync(db, "SEED-R-DONE2", customer2.Value, 3, today.AddDays(-18), new TimeOnly(11, 0), 1,
                ReservationStatus.Completed, prices, admin.Value, null, null, PaymentStatus.Completed, "seed-reservation-done2", cancellationToken);
            await AddReservationAsync(db, "SEED-R-DONE3", mobile.Value, 2, today.AddDays(-14), new TimeOnly(20, 0), 2,
                ReservationStatus.Completed, prices, admin.Value, null, null, PaymentStatus.Completed, "seed-reservation-done3", cancellationToken);
            await AddReservationAsync(db, "SEED-R-CANC", customer1.Value, 5, today.AddDays(-10), new TimeOnly(10, 0), 1,
                ReservationStatus.Cancelled, prices, null, admin.Value, "The visitor could not attend that day.", PaymentStatus.Refunded, "seed-reservation-canc", cancellationToken);
            await AddReservationAsync(db, "SEED-R-CONF", customer1.Value, 8, null, null, 2,
                ReservationStatus.Confirmed, prices, admin.Value, null, null, PaymentStatus.Completed, "seed-reservation-conf", cancellationToken, today);
            await AddReservationAsync(db, "SEED-R-CONF2", mobile.Value, 6, null, null, 1,
                ReservationStatus.Confirmed, prices, admin.Value, null, null, PaymentStatus.Completed, "seed-reservation-conf2", cancellationToken, today);
            await AddReservationAsync(db, "SEED-R-CONF3", customer2.Value, 9, null, null, 1,
                ReservationStatus.Confirmed, prices, admin.Value, null, null, PaymentStatus.Completed, "seed-reservation-conf3", cancellationToken, today);
            await AddReservationAsync(db, "SEED-R-PEND", customer2.Value, 7, null, null, 2,
                ReservationStatus.Pending, prices, null, null, null, PaymentStatus.Pending, "seed-reservation-pend", cancellationToken, today);
        }

        private static async Task AddReservationAsync(
            ECommerceDbContext db,
            string code,
            int userId,
            int activityId,
            DateOnly? date,
            TimeOnly? start,
            int guests,
            ReservationStatus status,
            IReadOnlyDictionary<int, decimal> prices,
            int? confirmedBy,
            int? cancelledBy,
            string? cancellationReason,
            PaymentStatus paymentStatus,
            string sessionId,
            CancellationToken cancellationToken,
            DateOnly? futureFrom = null)
        {
            if (await db.Reservations.AnyAsync(reservation => reservation.Code == code, cancellationToken))
            {
                return;
            }

            if (!prices.TryGetValue(activityId, out var price))
            {
                return;
            }

            Schedule? schedule;
            if (date.HasValue && start.HasValue)
            {
                schedule = await db.Schedules.FirstOrDefaultAsync(
                    item => item.ActivityId == activityId && item.Date == date.Value && item.StartTime == start.Value,
                    cancellationToken);
            }
            else
            {
                var from = futureFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);
                schedule = await db.Schedules
                    .Where(item => item.ActivityId == activityId
                        && item.Date > from
                        && item.Status == ScheduleStatus.Active
                        && item.CapacityLeft >= guests)
                    .OrderBy(item => item.Date)
                    .ThenBy(item => item.StartTime)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (schedule == null)
            {
                return;
            }

            if (status != ReservationStatus.Cancelled)
            {
                var alreadyBooked = await db.Reservations.AnyAsync(
                    reservation => reservation.UserId == userId
                        && reservation.ScheduleId == schedule.Id
                        && reservation.Status != ReservationStatus.Cancelled,
                    cancellationToken);
                if (alreadyBooked)
                {
                    return;
                }
            }

            if (status != ReservationStatus.Cancelled && schedule.CapacityLeft >= guests)
            {
                schedule.CapacityLeft -= guests;
            }

            var now = DateTime.UtcNow;
            var payment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = price * guests,
                Status = paymentStatus,
                UserId = userId,
                RelatedEntityType = nameof(PaymentRelatedEntityType.Reservation),
                StripeCheckoutSessionId = sessionId,
                CreatedAt = now
            };
            var reservation = new Reservation
            {
                UserId = userId,
                Schedule = schedule,
                GuestCount = guests,
                TotalPrice = price * guests,
                Status = status,
                Code = code,
                Payment = payment,
                CreatedAt = now,
                ReminderSentAt = now,
                ConfirmedByUserId = status is ReservationStatus.Confirmed or ReservationStatus.Completed ? confirmedBy : null,
                ConfirmedAt = status is ReservationStatus.Confirmed or ReservationStatus.Completed ? now : null,
                CancelledByUserId = status == ReservationStatus.Cancelled ? cancelledBy : null,
                CancelledAt = status == ReservationStatus.Cancelled ? now : null,
                CancellationReason = cancellationReason
            };
            db.Payments.Add(payment);
            db.Reservations.Add(reservation);
        }

        private static async Task EnsureTicketsAsync(ECommerceDbContext db, DateOnly today, CancellationToken cancellationToken)
        {
            var customer1 = await UserIdAsync(db, "customer1", cancellationToken);
            var customer2 = await UserIdAsync(db, "customer2", cancellationToken);
            var mobile = await UserIdAsync(db, "mobile", cancellationToken);
            var admin = await UserIdAsync(db, "admin1", cancellationToken);
            if (customer1 == null || customer2 == null || mobile == null || admin == null)
            {
                return;
            }

            await AddTicketAsync(db, "SEED-T-ACTIVE", mobile.Value, today.AddDays(3), 2, TicketStatus.Active,
                null, null, PaymentStatus.Completed, "seed-ticket-active", cancellationToken);
            await AddTicketAsync(db, "SEED-T-USED", customer1.Value, today.AddDays(-12), 3, TicketStatus.Used,
                admin.Value, TicketVerificationMethod.ManualCode, PaymentStatus.Completed, "seed-ticket-used", cancellationToken);
            await AddTicketAsync(db, "SEED-T-CANCEL", customer2.Value, today.AddDays(4), 1, TicketStatus.Cancelled,
                null, null, PaymentStatus.Refunded, "seed-ticket-cancel", cancellationToken);
            await AddTicketAsync(db, "SEED-T-PEND", customer1.Value, today.AddDays(6), 2, TicketStatus.PendingPayment,
                null, null, PaymentStatus.Pending, "seed-ticket-pend", cancellationToken);
        }

        private static async Task AddTicketAsync(
            ECommerceDbContext db,
            string code,
            int userId,
            DateOnly visitDate,
            int visitors,
            TicketStatus status,
            int? verifiedBy,
            TicketVerificationMethod? method,
            PaymentStatus paymentStatus,
            string sessionId,
            CancellationToken cancellationToken)
        {
            if (await db.Tickets.AnyAsync(ticket => ticket.Code == code, cancellationToken))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var price = ZooPricing.ZooEntryPricePerPerson;
            var payment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = price * visitors,
                Status = paymentStatus,
                UserId = userId,
                RelatedEntityType = nameof(PaymentRelatedEntityType.Ticket),
                StripeCheckoutSessionId = sessionId,
                CreatedAt = now
            };
            db.Payments.Add(payment);
            db.Tickets.Add(new Ticket
            {
                UserId = userId,
                VisitDate = visitDate,
                VisitorCount = visitors,
                PricePerPerson = price,
                TotalPrice = price * visitors,
                Code = code,
                QrCodeData = code,
                Status = status,
                VerifiedAt = status == TicketStatus.Used ? now : null,
                VerifiedByUserId = status == TicketStatus.Used ? verifiedBy : null,
                VerificationMethod = method,
                Payment = payment,
                CreatedAt = now,
                ReminderSentAt = now
            });
        }

        private static async Task EnsureDonationsAsync(ECommerceDbContext db, CancellationToken cancellationToken)
        {
            var customer1 = await UserIdAsync(db, "customer1", cancellationToken);
            var customer2 = await UserIdAsync(db, "customer2", cancellationToken);
            var mobile = await UserIdAsync(db, "mobile", cancellationToken);
            if (customer1 == null || customer2 == null || mobile == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            await AddDonationAsync(db, mobile.Value, 1, 25m, now.AddMonths(-5), PaymentStatus.Completed, "seed-donation-m5", true, cancellationToken);
            await AddDonationAsync(db, customer1.Value, 2, 50m, now.AddMonths(-4), PaymentStatus.Completed, "seed-donation-m4", true, cancellationToken);
            await AddDonationAsync(db, mobile.Value, 2, 10m, now.AddMonths(-3), PaymentStatus.Completed, "seed-donation-m3", true, cancellationToken);
            await AddDonationAsync(db, customer2.Value, 3, 100m, now.AddMonths(-2), PaymentStatus.Completed, "seed-donation-m2", true, cancellationToken);
            await AddDonationAsync(db, mobile.Value, 3, 5m, now.AddMonths(-1), PaymentStatus.Completed, "seed-donation-m1", true, cancellationToken);
            await AddDonationAsync(db, customer1.Value, 1, 10m, now.AddDays(-20), PaymentStatus.Refunded, "seed-donation-refund", false, cancellationToken);
            await AddDonationAsync(db, customer2.Value, 1, 25m, now.AddDays(-2), PaymentStatus.Pending, "seed-donation-pend", false, cancellationToken);
        }

        private static async Task AddDonationAsync(
            ECommerceDbContext db,
            int userId,
            int partnerId,
            decimal amount,
            DateTime createdAt,
            PaymentStatus paymentStatus,
            string sessionId,
            bool countTowardGoal,
            CancellationToken cancellationToken)
        {
            if (await db.Payments.AnyAsync(payment => payment.StripeCheckoutSessionId == sessionId, cancellationToken))
            {
                return;
            }

            var partner = await db.ZooPartners.FirstOrDefaultAsync(item => item.Id == partnerId, cancellationToken);
            if (partner == null)
            {
                return;
            }

            if (countTowardGoal)
            {
                partner.CurrentAmount += amount;
            }

            var payment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = amount,
                Status = paymentStatus,
                UserId = userId,
                RelatedEntityType = nameof(PaymentRelatedEntityType.Donation),
                StripeCheckoutSessionId = sessionId,
                CreatedAt = createdAt
            };
            db.Payments.Add(payment);
            db.Donations.Add(new Donation
            {
                UserId = userId,
                ZooPartnerId = partnerId,
                Amount = amount,
                Payment = payment,
                CreatedAt = createdAt
            });
        }

        private static async Task EnsureAdoptionsAsync(ECommerceDbContext db, CancellationToken cancellationToken)
        {
            var customer1 = await UserIdAsync(db, "customer1", cancellationToken);
            var customer2 = await UserIdAsync(db, "customer2", cancellationToken);
            var mobile = await UserIdAsync(db, "mobile", cancellationToken);
            var admin = await UserIdAsync(db, "admin1", cancellationToken);
            if (customer1 == null || customer2 == null || mobile == null || admin == null)
            {
                return;
            }

            await AddAdoptionAsync(db, "seed-adoption-kavi", mobile.Value, 1, 15m, AdoptionStatus.Active,
                PaymentStatus.Completed, null, null, null, cancellationToken);
            await AddAdoptionAsync(db, "seed-adoption-bao", customer1.Value, 5, 25m, AdoptionStatus.Active,
                PaymentStatus.Completed, null, null, null, cancellationToken);
            await AddAdoptionAsync(db, "seed-adoption-luna", customer2.Value, 4, 20m, AdoptionStatus.Cancelled,
                PaymentStatus.Refunded, admin.Value, DateTime.UtcNow.AddDays(-6),
                "The visitor asked to stop this monthly gift.", cancellationToken);
        }

        private static async Task AddAdoptionAsync(
            ECommerceDbContext db,
            string sessionId,
            int userId,
            int animalId,
            decimal monthlyAmount,
            AdoptionStatus status,
            PaymentStatus paymentStatus,
            int? rejectedBy,
            DateTime? rejectedAt,
            string? rejectionReason,
            CancellationToken cancellationToken)
        {
            if (await db.Payments.AnyAsync(payment => payment.StripeCheckoutSessionId == sessionId, cancellationToken))
            {
                return;
            }

            var started = DateTime.UtcNow.AddMonths(-2);
            var payment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = monthlyAmount,
                Status = paymentStatus,
                UserId = userId,
                RelatedEntityType = nameof(PaymentRelatedEntityType.Adoption),
                StripeCheckoutSessionId = sessionId,
                CreatedAt = started
            };
            db.Payments.Add(payment);
            db.Adoptions.Add(new Adoption
            {
                UserId = userId,
                AnimalId = animalId,
                MonthlyAmount = monthlyAmount,
                Status = status,
                StartDate = started,
                Payment = payment,
                RejectedByUserId = rejectedBy,
                RejectedAt = rejectedAt,
                RejectionReason = rejectionReason
            });
        }

        private static async Task EnsureBadgesAsync(ECommerceDbContext db, CancellationToken cancellationToken)
        {
            var badges = await db.Badges.AsNoTracking().ToListAsync(cancellationToken);
            var users = await db.Users.AsNoTracking()
                .Where(user => user.Username == "mobile" || user.Username == "customer1" || user.Username == "customer2")
                .Select(user => new { user.Id, user.Username })
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                var adoptionCount = await db.Adoptions.CountAsync(
                    adoption => adoption.UserId == user.Id && adoption.Payment != null && adoption.Payment.Status == PaymentStatus.Completed,
                    cancellationToken);
                var reservationCount = await db.Reservations.CountAsync(
                    reservation => reservation.UserId == user.Id && reservation.Payment != null && reservation.Payment.Status == PaymentStatus.Completed,
                    cancellationToken);
                var donationCount = await db.Donations.CountAsync(
                    donation => donation.UserId == user.Id && donation.Payment != null && donation.Payment.Status == PaymentStatus.Completed,
                    cancellationToken);

                foreach (var badge in badges)
                {
                    var earned = badge.Criteria switch
                    {
                        BadgeCriteria.FirstAdoption => adoptionCount >= 1,
                        BadgeCriteria.FirstBooking => reservationCount >= 1,
                        BadgeCriteria.ThreeDonations => donationCount >= 3,
                        BadgeCriteria.FiveAdoptions => adoptionCount >= 5,
                        _ => false
                    };
                    if (!earned)
                    {
                        continue;
                    }

                    var already = await db.UserBadges.AnyAsync(
                        row => row.UserId == user.Id && row.BadgeId == badge.Id,
                        cancellationToken);
                    if (already)
                    {
                        continue;
                    }

                    db.UserBadges.Add(new UserBadge
                    {
                        UserId = user.Id,
                        BadgeId = badge.Id,
                        EarnedAt = DateTime.UtcNow
                    });
                }
            }
        }

        private static async Task EnsureNotificationsAsync(ECommerceDbContext db, CancellationToken cancellationToken)
        {
            var mobile = await UserIdAsync(db, "mobile", cancellationToken);
            var customer1 = await UserIdAsync(db, "customer1", cancellationToken);
            if (mobile == null || customer1 == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            await AddNotificationAsync(db, mobile.Value, "11111111-1111-4111-8111-111111111101",
                "Reservation confirmed.", "Your activity reservation is confirmed.",
                NotificationType.ReservationConfirmed, false, now.AddDays(-1), cancellationToken);
            await AddNotificationAsync(db, mobile.Value, "11111111-1111-4111-8111-111111111102",
                "Badge earned.", "You earned the Wildlife Guardian badge.",
                NotificationType.BadgeEarned, false, now.AddHours(-20), cancellationToken);
            await AddNotificationAsync(db, mobile.Value, "11111111-1111-4111-8111-111111111103",
                "Donation received.", "Thank you. Your donation was received.",
                NotificationType.DonationReceived, true, now.AddDays(-30), cancellationToken);
            await AddNotificationAsync(db, mobile.Value, "11111111-1111-4111-8111-111111111104",
                "Upcoming visit.", "Your reserved activity is coming up.",
                NotificationType.UpcomingReminder, false, now.AddHours(-5), cancellationToken);
            await AddNotificationAsync(db, customer1.Value, "11111111-1111-4111-8111-111111111105",
                "Reservation cancelled.", "Your reservation was cancelled.",
                NotificationType.ReservationCancelled, true, now.AddDays(-9), cancellationToken);
            await AddNotificationAsync(db, customer1.Value, "11111111-1111-4111-8111-111111111106",
                "Ticket ready.", "Your zoo ticket is ready to show at the gate.",
                NotificationType.TicketPurchased, false, now.AddDays(-2), cancellationToken);
        }

        private static async Task AddNotificationAsync(
            ECommerceDbContext db,
            int userId,
            string messageId,
            string title,
            string message,
            NotificationType type,
            bool isRead,
            DateTime createdAt,
            CancellationToken cancellationToken)
        {
            var id = Guid.Parse(messageId);
            if (await db.Notifications.AnyAsync(notification => notification.MessageId == id, cancellationToken))
            {
                return;
            }

            db.Notifications.Add(new Notification
            {
                MessageId = id,
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = isRead,
                CreatedAt = createdAt,
                EmailSentAt = createdAt
            });
        }

        private static async Task<int?> UserIdAsync(ECommerceDbContext db, string username, CancellationToken cancellationToken)
        {
            return await db.Users
                .Where(user => user.Username == username)
                .Select(user => (int?)user.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
