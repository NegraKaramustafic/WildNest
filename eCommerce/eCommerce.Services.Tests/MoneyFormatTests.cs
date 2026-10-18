using eCommerce.Model.Messages;
using eCommerce.Model.Responses;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class MoneyFormatTests
    {
        [Fact]
        public void Format_UsesEuroSignEnglishGroupingAndTwoDecimals()
        {
            Assert.Equal("€1,250.50", MoneyFormat.Format(1250.50m));
            Assert.Equal("€1,250.00", MoneyFormat.Format(1250m));
            Assert.Equal("€5.00", MoneyFormat.Format(5m));
            Assert.Equal("€10.00", MoneyFormat.Format(10m));
            Assert.Equal("€25.00", MoneyFormat.Format(25m));
            Assert.Equal("€50.00", MoneyFormat.Format(50m));
            Assert.Equal("€100.00", MoneyFormat.Format(100m));
            Assert.Equal("€0.00", MoneyFormat.Format(0m));
            Assert.Equal("-€12.50", MoneyFormat.Format(-12.5m));
        }

        [Fact]
        public async Task DonationAndPaymentNotifications_UseTheSharedEuroFormat()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = new NotificationEventService(publisher, db.Context);

            await service.NotifyDonationReceivedAsync(new DonationResponse
            {
                Id = 3,
                UserId = 4,
                Amount = 10m,
                ZooPartnerName = "Sunbird Sanctuary"
            });
            await service.NotifyPaymentCompletedAsync(4, 9, 1250.5m);

            Assert.Equal(
                "Thank you for donating €10.00 to Sunbird Sanctuary.",
                publisher.Messages[0].Message);
            Assert.Equal(
                "Your payment of €1,250.50 was completed.",
                publisher.Messages[1].Message);
        }

        private sealed class CapturingPublisher : INotificationPublisher
        {
            public List<NotificationMessage> Messages { get; } = new();

            public Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
            {
                Messages.Add(message);
                return Task.CompletedTask;
            }
        }
    }
}
