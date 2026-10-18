using eCommerce.Model.Enums;
using eCommerce.Services;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using eCommerce.Services.ReservationStateMachine;
using eCommerce.Services.Validators;
using MapsterMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class BookingIntegrityTests
    {
        [Fact]
        public async Task Insert_RejectsPastAndInactiveSchedules_AndASecondActiveBooking()
        {
            using var db = new SqliteDb();
            var scheduleId = await AddSchedule(db.Context, new DateOnly(2020, 1, 1), ScheduleStatus.Active, 4);
            var service = Create(db.Context, userId: 4);

            var past = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(new ReservationInsertRequest
            {
                ScheduleId = scheduleId,
                GuestCount = 1
            }));
            Assert.Equal("This schedule is in the past.", past.Message);

            var inactiveId = await AddSchedule(db.Context, new DateOnly(2027, 6, 1), ScheduleStatus.Cancelled, 4);
            var inactive = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(new ReservationInsertRequest
            {
                ScheduleId = inactiveId,
                GuestCount = 1
            }));
            Assert.Equal("This schedule is not active.", inactive.Message);

            var openId = await AddSchedule(db.Context, new DateOnly(2027, 6, 2), ScheduleStatus.Active, 4);
            var created = await service.InsertAsync(new ReservationInsertRequest
            {
                ScheduleId = openId,
                GuestCount = 2
            });
            Assert.Equal(ReservationStatus.Pending, created.Status);

            var duplicate = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(new ReservationInsertRequest
            {
                ScheduleId = openId,
                GuestCount = 1
            }));
            Assert.Equal("You already have a booking for this schedule.", duplicate.Message);

            db.Context.ChangeTracker.Clear();
            var capacity = await db.Context.Schedules.Where(s => s.Id == openId).Select(s => s.CapacityLeft).SingleAsync();
            Assert.Equal(2, capacity);

            var stored = await db.Context.Reservations.SingleAsync(r => r.Id == created.Id);
            stored.Status = ReservationStatus.Cancelled;
            var schedule = await db.Context.Schedules.SingleAsync(s => s.Id == openId);
            schedule.CapacityLeft += stored.GuestCount;
            await db.Context.SaveChangesAsync();

            var again = await service.InsertAsync(new ReservationInsertRequest
            {
                ScheduleId = openId,
                GuestCount = 1
            });
            Assert.NotEqual(created.Id, again.Id);
        }

        [Fact]
        public async Task Insert_ParallelRequests_CannotOverbookTheLastSpots()
        {
            using var shared = new SharedSqlite();
            int scheduleId;
            using (var seed = shared.CreateContext())
            {
                scheduleId = await AddSchedule(seed, new DateOnly(2027, 7, 1), ScheduleStatus.Active, 1);
            }

            var first = Task.Run(() => Book(shared, userId: 4, scheduleId));
            var second = Task.Run(() => Book(shared, userId: 5, scheduleId));
            var results = await Task.WhenAll(Wrap(first), Wrap(second));

            Assert.Equal(1, results.Count(result => result == null));
            Assert.Equal(1, results.Count(result => result == "Not enough spots available."));

            using var check = shared.CreateContext();
            var capacity = await check.Schedules.Where(s => s.Id == scheduleId).Select(s => s.CapacityLeft).SingleAsync();
            var active = await check.Reservations.CountAsync(r =>
                r.ScheduleId == scheduleId && r.Status != ReservationStatus.Cancelled);
            Assert.Equal(0, capacity);
            Assert.Equal(1, active);
        }

        private static async Task<string?> Wrap(Task task)
        {
            try
            {
                await task;
                return null;
            }
            catch (ClientException ex)
            {
                return ex.Message;
            }
        }

        private static async Task Book(SharedSqlite shared, int userId, int scheduleId)
        {
            using var context = shared.CreateContext();
            var service = Create(context, userId);
            await service.InsertAsync(new ReservationInsertRequest
            {
                ScheduleId = scheduleId,
                GuestCount = 1
            });
        }

        private static async Task<int> AddSchedule(ECommerceDbContext context, DateOnly date, ScheduleStatus status, int capacity)
        {
            var schedule = new Schedule
            {
                ActivityId = 1,
                Date = date,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                CapacityLeft = capacity,
                Status = status
            };
            context.Schedules.Add(schedule);
            await context.SaveChangesAsync();
            return schedule.Id;
        }

        private static ReservationService Create(ECommerceDbContext context, int userId)
        {
            var loggerFactory = new SilentLoggerFactory();
            var mapper = new Mapper();
            var accessor = new TestUserAccessor { UserId = userId, Admin = false };
            var provider = new StateBag();
            var clock = new ZooClock("Europe/Sarajevo");
            var pending = new PendingReservationState(context, mapper, provider, loggerFactory);
            var confirmed = new ConfirmedReservationState(context, mapper, provider, loggerFactory);
            var completed = new CompletedReservationState(context, mapper, provider, loggerFactory);
            var cancelled = new CancelledReservationState(context, mapper, provider, loggerFactory);
            var initial = new InitialReservationState(
                context,
                mapper,
                provider,
                loggerFactory,
                new ReservationInsertValidator(),
                accessor,
                clock);
            provider.Add(typeof(PendingReservationState), pending);
            provider.Add(typeof(ConfirmedReservationState), confirmed);
            provider.Add(typeof(CompletedReservationState), completed);
            provider.Add(typeof(CancelledReservationState), cancelled);
            provider.Add(typeof(InitialReservationState), initial);
            var root = new BaseReservationState(context, mapper, provider, loggerFactory);
            return new ReservationService(
                context,
                mapper,
                root,
                accessor,
                new NotificationEventService(new CapturingPublisher(), context),
                new RecommendationModelCache(),
                new RecordingPaymentService(),
                new ReservationCancelValidator(),
                loggerFactory);
        }

        private sealed class CapturingPublisher : INotificationPublisher
        {
            public Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }

    internal sealed class SharedSqlite : IDisposable
    {
        private readonly SqliteConnection _keepAlive;
        private readonly List<SqliteConnection> _connections = new();

        public SharedSqlite()
        {
            var name = "wn" + Guid.NewGuid().ToString("N");
            ConnectionString = $"Data Source=file:{name}?mode=memory&cache=shared";
            _keepAlive = Open();
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public string ConnectionString { get; }

        public ECommerceDbContext CreateContext()
        {
            var connection = Open();
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseSqlite(connection)
                .Options;
            return new ECommerceDbContext(options);
        }

        public void Dispose()
        {
            foreach (var connection in _connections)
            {
                connection.Dispose();
            }

            _keepAlive.Dispose();
        }

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA busy_timeout = 8000;";
            command.ExecuteNonQuery();
            _connections.Add(connection);
            return connection;
        }
    }
}
