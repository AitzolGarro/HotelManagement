using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelReservationSystem.Services;

public class ExportService : IExportService
{
    // Column definitions for ReservationDto exports: header → property selector
    private static readonly (string Header, Func<ReservationDto, string> Selector)[] ReservationColumns =
    [
        ("Booking Reference",  r => r.BookingReference ?? ""),
        ("Guest Name",         r => r.GuestName),
        ("Guest Email",        r => r.GuestEmail ?? ""),
        ("Guest Phone",        r => r.GuestPhone ?? ""),
        ("Hotel",              r => r.HotelName),
        ("Room",               r => r.RoomNumber),
        ("Room Type",          r => r.RoomType),
        ("Check-in",           r => r.CheckInDate.ToString("yyyy-MM-dd")),
        ("Check-out",          r => r.CheckOutDate.ToString("yyyy-MM-dd")),
        ("Nights",             r => ((r.CheckOutDate - r.CheckInDate).Days).ToString()),
        ("Guests",             r => r.NumberOfGuests.ToString()),
        ("Total Amount",       r => r.TotalAmount.ToString("F2")),
        ("Status",             r => r.Status.ToString()),
        ("Source",             r => r.Source.ToString()),
        ("Special Requests",   r => r.SpecialRequests ?? ""),
        ("Created At",         r => r.CreatedAt.ToString("yyyy-MM-dd HH:mm")),
    ];

    public ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Generic exports (kept for backward compatibility) ────────────────────

    public byte[] ExportToCsv<T>(IEnumerable<T> data) where T : class
    {
        // Specialise for ReservationDto to get clean headers
        if (typeof(T) == typeof(ReservationDto))
            return ExportReservationsToCsv(data.Cast<ReservationDto>());

        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
        csvWriter.WriteRecords(data);
        streamWriter.Flush();
        return memoryStream.ToArray();
    }

    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Data") where T : class
    {
        if (typeof(T) == typeof(ReservationDto))
            return ExportReservationsToExcel(data.Cast<ReservationDto>(), sheetName);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);
        var properties = typeof(T).GetProperties();

        for (int i = 0; i < properties.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = properties[i].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var item in data)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(item);
                worksheet.Cell(row, col + 1).Value = value?.ToString() ?? string.Empty;
            }
            row++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf<T>(IEnumerable<T> data, string title) where T : class
    {
        if (typeof(T) == typeof(ReservationDto))
            return ExportReservationsToPdf(data.Cast<ReservationDto>(), title);

        var properties = typeof(T).GetProperties();
        return GeneratePdf(title,
            properties.Select(p => p.Name).ToArray(),
            data.Select(item => properties.Select(p => p.GetValue(item)?.ToString() ?? "").ToArray()).ToArray());
    }

    // ── ReservationDto-specific exports ──────────────────────────────────────

    private byte[] ExportReservationsToCsv(IEnumerable<ReservationDto> data)
    {
        using var ms = new MemoryStream();
        // UTF-8 BOM so Excel opens it correctly
        using var sw = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        // Write header row
        sw.WriteLine(string.Join(",", ReservationColumns.Select(c => QuoteCsvField(c.Header))));

        foreach (var r in data)
        {
            sw.WriteLine(string.Join(",", ReservationColumns.Select(c => QuoteCsvField(c.Selector(r)))));
        }

        sw.Flush();
        return ms.ToArray();
    }

    private byte[] ExportReservationsToExcel(IEnumerable<ReservationDto> data, string sheetName)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName);

        // Header row with styling
        for (int i = 0; i < ReservationColumns.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = ReservationColumns[i].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        int row = 2;
        foreach (var r in data)
        {
            for (int col = 0; col < ReservationColumns.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                var value = ReservationColumns[col].Selector(r);

                // Try to set numeric values as numbers for proper Excel formatting
                if (col == ReservationColumns.Length - 4 && decimal.TryParse(value, out var amount)) // Total Amount
                    cell.Value = amount;
                else if (col == ReservationColumns.Length - 5 && int.TryParse(value, out var nights)) // Nights
                    cell.Value = nights;
                else if (col == ReservationColumns.Length - 6 && int.TryParse(value, out var guests)) // Guests
                    cell.Value = guests;
                else
                    cell.Value = value;

                // Alternate row shading
                if (row % 2 == 0)
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
            }
            row++;
        }

        // Auto-fit columns and freeze header
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        // Add auto-filter
        if (row > 2)
            ws.Range(1, 1, row - 1, ReservationColumns.Length).SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private byte[] ExportReservationsToPdf(IEnumerable<ReservationDto> data, string title)
    {
        // Use a subset of columns for PDF (landscape A4 fits ~8-10 columns well)
        var pdfColumns = new (string Header, Func<ReservationDto, string> Selector)[]
        {
            ("Booking Ref",   r => r.BookingReference ?? ""),
            ("Guest",         r => r.GuestName),
            ("Hotel",         r => r.HotelName),
            ("Room",          r => r.RoomNumber),
            ("Check-in",      r => r.CheckInDate.ToString("yyyy-MM-dd")),
            ("Check-out",     r => r.CheckOutDate.ToString("yyyy-MM-dd")),
            ("Nights",        r => ((r.CheckOutDate - r.CheckInDate).Days).ToString()),
            ("Amount",        r => $"€{r.TotalAmount:F2}"),
            ("Status",        r => r.Status.ToString()),
            ("Source",        r => r.Source.ToString()),
        };

        return GeneratePdf(title,
            pdfColumns.Select(c => c.Header).ToArray(),
            data.Select(r => pdfColumns.Select(c => c.Selector(r)).ToArray()).ToArray());
    }

    // ── Shared PDF generator ─────────────────────────────────────────────────

    private static byte[] GeneratePdf(string title, string[] headers, string[][] rows)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .PaddingBottom(8)
                    .Row(row =>
                    {
                        row.RelativeItem().Text(title).SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);
                        row.ConstantItem(200).AlignRight()
                            .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (int i = 0; i < headers.Length; i++)
                            cols.RelativeColumn();
                    });

                    // Header
                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell()
                                .Background(Colors.Blue.Darken2)
                                .Padding(4)
                                .Text(h).SemiBold().FontColor(Colors.White).FontSize(8);
                        }
                    });

                    // Data rows with alternating background
                    for (int i = 0; i < rows.Length; i++)
                    {
                        var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                        foreach (var cell in rows[i])
                        {
                            table.Cell()
                                .Background(bg)
                                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .Padding(3)
                                .Text(cell).FontSize(8);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontSize(8);
                    x.CurrentPageNumber().FontSize(8);
                    x.Span(" of ").FontSize(8);
                    x.TotalPages().FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string QuoteCsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}