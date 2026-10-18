using eCommerce.Model.Enums;
using eCommerce.Model.Messages;
using eCommerce.Services;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class NotificationDeliveryTests
    {
        [Fact]
        public async Task Store_InsertsOnce_AndSkipsEmailAfterItIsMarkedSent()
        {
            using var db = new SqliteDb();
            var message = Sample();
            var store = new NotificationDeliveryStore(db.Context);

            Assert.True(await store.NeedsEmailAsync(message));
            Assert.True(await store.NeedsEmailAsync(message));
            Assert.Equal(1, await db.Context.Notifications.CountAsync());
            Assert.Null(await db.Context.Notifications.Select(row => row.EmailSentAt).SingleAsync());
            Assert.Equal("Your booking is confirmed.", await db.Context.Notifications.Select(row => row.Message).SingleAsync());

            await store.MarkEmailSentAsync(message.MessageId);

            Assert.False(await store.NeedsEmailAsync(message));
            Assert.Equal(1, await db.Context.Notifications.CountAsync());
            Assert.NotNull(await db.Context.Notifications.Select(row => row.EmailSentAt).SingleAsync());
        }

        [Fact]
        public async Task MarkEmailSent_IsIdempotent()
        {
            using var db = new SqliteDb();
            var message = Sample();
            var store = new NotificationDeliveryStore(db.Context);
            await store.NeedsEmailAsync(message);

            await store.MarkEmailSentAsync(message.MessageId);
            var first = await db.Context.Notifications.Select(row => row.EmailSentAt).SingleAsync();
            await store.MarkEmailSentAsync(message.MessageId);
            var second = await db.Context.Notifications.Select(row => row.EmailSentAt).SingleAsync();

            Assert.Equal(first, second);
        }

        [Fact]
        public async Task MarkEmailSent_ThrowsWhenTheNotificationRowIsMissing()
        {
            using var db = new SqliteDb();
            var store = new NotificationDeliveryStore(db.Context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.MarkEmailSentAsync(Guid.NewGuid()));
        }

        private static NotificationMessage Sample()
        {
            return new NotificationMessage
            {
                MessageId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                UserId = 4,
                Type = NotificationType.ReservationConfirmed,
                Title = "Reservation confirmed",
                Message = "Your booking is confirmed.",
                EmailBody = "This secret email body must not be stored.",
                RelatedEntityType = "Reservation",
                RelatedEntityId = 9
            };
        }
    }
}
