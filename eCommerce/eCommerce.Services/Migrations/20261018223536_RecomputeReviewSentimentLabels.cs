using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class RecomputeReviewSentimentLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Binary Positive/Negative replaces the previous three-class labels.
            // Clearing every stored label lets the API recompute them from the shipped model.
            migrationBuilder.Sql(
                """
                UPDATE Reviews
                SET SentimentLabel = NULL,
                    SentimentScore = NULL
                WHERE SentimentLabel IS NOT NULL
                   OR SentimentScore IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Previous labels cannot be restored.
        }
    }
}
