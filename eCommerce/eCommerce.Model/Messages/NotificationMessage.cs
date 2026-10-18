using System.Text.Json;
using System.Text.Json.Serialization;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Messages
{
    public class NotificationMessage
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public Guid MessageId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? Email { get; set; }

        /// <summary>
        /// Optional email subject. The notification row keeps <see cref="Title"/>.
        /// </summary>
        public string? EmailSubject { get; set; }

        /// <summary>
        /// Optional email body. Never persisted and never written to logs.
        /// The notification row keeps <see cref="Message"/>, which must not contain secrets.
        /// </summary>
        public string? EmailBody { get; set; }
    }
}
