using System.Globalization;
using eCommerce.Model.Enums;
using eCommerce.Model.Responses;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eCommerce.Services.Reports
{
    public class QuestPdfReportRenderer : IPdfReportRenderer
    {
        private const string Green = "#1E7A46";
        private const string Ink = "#1C1C1C";
        private const string Muted = "#7A7A72";
        private const string Line = "#E4DFD4";
        private const string Stripe = "#F7F5F0";

        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

        static QuestPdfReportRenderer()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] RenderReservations(ReservationReportDocument document)
        {
            return Render(PageSizes.A4.Landscape(), "Reservations by period", content =>
            {
                content.Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text(Period(document.DateFrom, document.DateTo)).FontColor(Muted);
                    column.Item().Text(document.ActivityName == null
                        ? "Activity: All activities."
                        : $"Activity: {document.ActivityName}.").FontColor(Muted);
                    column.Item().Text(document.Status.HasValue
                        ? $"Status: {StatusName(document.Status.Value)}."
                        : "Status: All statuses.").FontColor(Muted);
                    column.Item().Text(GeneratedLine(document.GeneratedByName, document.GeneratedAtUtc));
                    column.Item().Text("Includes reservations created in the selected period.").FontSize(9).FontColor(Muted);
                    column.Item().Text("Revenue is the sum of totals for reservations with a completed payment.").FontSize(9).FontColor(Muted);

                    column.Item().PaddingTop(4).Text("Summary").FontSize(14).SemiBold().FontColor(Green);
                    column.Item().Element(table => SummaryTable(table, new (string, string)[]
                    {
                        ("Total reservations", Count(document.TotalReservations)),
                        ("Total guests", Count(document.TotalGuests)),
                        ("Revenue", Money(document.Revenue))
                    }));

                    column.Item().PaddingTop(4).Text("Counts by status").FontSize(14).SemiBold().FontColor(Green);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                        });
                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "Status");
                            HeaderCell(header.Cell(), "Count");
                        });
                        foreach (var status in document.StatusCounts)
                        {
                            BodyCell(table.Cell(), StatusName(status.Status), false);
                            BodyCell(table.Cell(), Count(status.Count), false);
                        }
                    });

                    column.Item().PaddingTop(4).Text("Reservations").FontSize(14).SemiBold().FontColor(Green);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(78);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.5f);
                            columns.ConstantColumn(52);
                            columns.ConstantColumn(72);
                            columns.ConstantColumn(78);
                            columns.RelativeColumn(1.4f);
                        });
                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "Code");
                            HeaderCell(header.Cell(), "Customer");
                            HeaderCell(header.Cell(), "Activity");
                            HeaderCell(header.Cell(), "Schedule");
                            HeaderCell(header.Cell(), "Guests");
                            HeaderCell(header.Cell(), "Status");
                            HeaderCell(header.Cell(), "Total");
                            HeaderCell(header.Cell(), "Created");
                        });
                        for (var index = 0; index < document.Rows.Count; index++)
                        {
                            var row = document.Rows[index];
                            var stripe = index % 2 == 1;
                            BodyCell(table.Cell(), row.Code, stripe);
                            BodyCell(table.Cell(), row.CustomerName, stripe);
                            BodyCell(table.Cell(), row.ActivityName, stripe);
                            BodyCell(table.Cell(), Schedule(row.ScheduleDate, row.ScheduleStartTime), stripe);
                            BodyCell(table.Cell(), Count(row.GuestCount), stripe);
                            BodyCell(table.Cell(), StatusName(row.Status), stripe);
                            BodyCell(table.Cell(), Money(row.TotalPrice), stripe);
                            BodyCell(table.Cell(), Timestamp(row.CreatedAt), stripe);
                        }
                    });
                });
            });
        }

        public byte[] RenderDonations(DonationReportDocument document)
        {
            return Render(PageSizes.A4, "Donations by zoo partner", content =>
            {
                content.Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text(Period(document.DateFrom, document.DateTo)).FontColor(Muted);
                    column.Item().Text(document.PartnerName == null
                        ? "Zoo partner: All partners."
                        : $"Zoo partner: {document.PartnerName}.").FontColor(Muted);
                    column.Item().Text(GeneratedLine(document.GeneratedByName, document.GeneratedAtUtc));
                    column.Item().Text("Totals include completed payments only. The list includes every donation in the period.").FontSize(9).FontColor(Muted);

                    column.Item().PaddingTop(4).Text("Summary").FontSize(14).SemiBold().FontColor(Green);
                    column.Item().Element(table => SummaryTable(table, new (string, string)[]
                    {
                        ("Completed donations", Count(document.TotalDonations)),
                        ("Grand total", Money(document.GrandTotal))
                    }));

                    column.Item().PaddingTop(4).Text("Totals by zoo partner").FontSize(14).SemiBold().FontColor(Green);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.4f);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                        });
                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "Zoo partner");
                            HeaderCell(header.Cell(), "Location");
                            HeaderCell(header.Cell(), "Completed");
                            HeaderCell(header.Cell(), "Total");
                        });
                        for (var index = 0; index < document.Partners.Count; index++)
                        {
                            var row = document.Partners[index];
                            var stripe = index % 2 == 1;
                            BodyCell(table.Cell(), row.PartnerName, stripe);
                            BodyCell(table.Cell(), row.Location, stripe);
                            BodyCell(table.Cell(), Count(row.DonationCount), stripe);
                            BodyCell(table.Cell(), Money(row.TotalAmount), stripe);
                        }

                        table.Cell().Background(Green).Padding(5).Text("Grand total").FontColor(Colors.White).SemiBold();
                        table.Cell().Background(Green).Padding(5).Text(string.Empty);
                        table.Cell().Background(Green).Padding(5).Text(Count(document.TotalDonations)).FontColor(Colors.White).SemiBold();
                        table.Cell().Background(Green).Padding(5).Text(Money(document.GrandTotal)).FontColor(Colors.White).SemiBold();
                    });

                    column.Item().PaddingTop(4).Text("Donations").FontSize(14).SemiBold().FontColor(Green);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.2f);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(90);
                            columns.RelativeColumn(1.6f);
                        });
                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "Zoo partner");
                            HeaderCell(header.Cell(), "Location");
                            HeaderCell(header.Cell(), "Amount");
                            HeaderCell(header.Cell(), "Payment");
                            HeaderCell(header.Cell(), "Created");
                        });
                        for (var index = 0; index < document.Lines.Count; index++)
                        {
                            var row = document.Lines[index];
                            var stripe = index % 2 == 1;
                            BodyCell(table.Cell(), row.PartnerName, stripe);
                            BodyCell(table.Cell(), row.Location, stripe);
                            BodyCell(table.Cell(), Money(row.Amount), stripe);
                            BodyCell(table.Cell(), row.PaymentStatus, stripe);
                            BodyCell(table.Cell(), Timestamp(row.CreatedAt), stripe);
                        }
                    });
                });
            });
        }

        private static byte[] Render(PageSize pageSize, string title, Action<IContainer> content)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(pageSize);
                    page.Margin(32);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(style => style.FontSize(9).FontColor(Ink));
                    page.Header().Element(header => ComposeHeader(header, title));
                    page.Content().PaddingVertical(12).Element(content);
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        private static void ComposeHeader(IContainer container, string title)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(28).Height(28).Background(Green).AlignCenter().AlignMiddle()
                        .Text("W").FontColor(Colors.White).Bold().FontSize(14);
                    row.ConstantItem(10);
                    row.RelativeItem().Column(text =>
                    {
                        text.Item().Text("WildNest").FontSize(16).Bold().FontColor(Green);
                        text.Item().Text(title).FontSize(12).SemiBold();
                    });
                });
                column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Green);
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.PaddingTop(6).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Text("WildNest").FontSize(8).FontColor(Muted);
                row.RelativeItem().AlignRight().AlignMiddle().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }

        private static void SummaryTable(IContainer container, IReadOnlyList<(string Label, string Value)> rows)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                });
                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Item");
                    HeaderCell(header.Cell(), "Value");
                });
                foreach (var row in rows)
                {
                    BodyCell(table.Cell(), row.Label, false);
                    BodyCell(table.Cell(), row.Value, false);
                }
            });
        }

        private static void HeaderCell(IContainer cell, string text)
        {
            cell.Background(Green).Padding(5).Text(text).FontColor(Colors.White).SemiBold();
        }

        private static void BodyCell(IContainer cell, string text, bool stripe)
        {
            cell.Background(stripe ? Stripe : Colors.White)
                .BorderBottom(0.5f)
                .BorderColor(Line)
                .Padding(5)
                .Text(text);
        }

        private static string Period(DateOnly from, DateOnly to) =>
            $"Period: {Date(from)} to {Date(to)}.";

        private static string GeneratedLine(string name, DateTime generatedAtUtc) =>
            $"Generated by {name} on {Timestamp(generatedAtUtc)}.";

        private static string Date(DateOnly date) =>
            date.ToString("dd MMM yyyy", English);

        private static string Timestamp(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return utc.ToString("dd MMM yyyy, h:mm tt", English) + " UTC";
        }

        private static string Schedule(DateOnly date, TimeOnly time) =>
            date.ToDateTime(time).ToString("dd MMM yyyy, h:mm tt", English);

        private static string Money(decimal amount) =>
            amount.ToString("N2", English) + " KM";

        private static string Count(int value) =>
            value.ToString("N0", English);

        private static string StatusName(ReservationStatus status) => status switch
        {
            ReservationStatus.Pending => "Pending",
            ReservationStatus.Confirmed => "Confirmed",
            ReservationStatus.Completed => "Completed",
            ReservationStatus.Cancelled => "Cancelled",
            _ => status.ToString()
        };
    }
}
