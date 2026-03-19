using System.Collections.Generic;

namespace HotelReservationSystem.Services.Interfaces;

public interface IExportService
{
    byte[] ExportToCsv<T>(IEnumerable<T> data) where T : class;
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Data") where T : class;
    byte[] ExportToPdf<T>(IEnumerable<T> data, string title) where T : class;
}