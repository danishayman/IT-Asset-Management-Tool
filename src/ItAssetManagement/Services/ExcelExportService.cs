using ClosedXML.Excel;
using ItAssetManagement.Domain;
using ItAssetManagement.ViewModels;

namespace ItAssetManagement.Services;

/// <summary>
/// Builds .xlsx workbooks with ClosedXML (MIT licensed, unlike EPPlus whose current
/// licence is non-commercial).
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#212529");

    public byte[] BuildAssetWorkbook(IReadOnlyList<AssetListItemViewModel> assets)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Assets");

        string[] headers =
        [
            "Asset tag", "Name", "Category", "Serial number",
            "Status", "Assigned to", "Location", "Warranty expiry"
        ];

        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var i = 0; i < assets.Count; i++)
        {
            var asset = assets[i];
            var row = i + 2;

            sheet.Cell(row, 1).Value = asset.AssetTag;
            sheet.Cell(row, 2).Value = asset.Name;
            sheet.Cell(row, 3).Value = asset.CategoryName;
            sheet.Cell(row, 4).Value = asset.SerialNumber;
            sheet.Cell(row, 5).Value = asset.Status.ToDisplayName();

            // Blank rather than a dash: the column reads as empty to a filter or pivot,
            // which is what an unassigned asset actually means.
            sheet.Cell(row, 6).Value = asset.AssignedToFullName ?? string.Empty;

            sheet.Cell(row, 7).Value = asset.Location;

            // Written as a real date, not a string, so Excel can sort and filter on it.
            sheet.Cell(row, 8).Value = asset.WarrantyExpiry.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 8).Style.DateFormat.Format = "yyyy-mm-dd";
        }

        StyleSheet(sheet, headers.Length, assets.Count);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] BuildUserWorkbook(IReadOnlyList<UserListItemViewModel> users)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Users");

        string[] headers = ["Username", "Full name", "Role", "Status", "Assets held", "Created"];

        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var i = 0; i < users.Count; i++)
        {
            var user = users[i];
            var row = i + 2;

            sheet.Cell(row, 1).Value = user.Username;
            sheet.Cell(row, 2).Value = user.FullName;
            sheet.Cell(row, 3).Value = user.Role;
            sheet.Cell(row, 4).Value = user.IsActive ? "Active" : "Deactivated";
            sheet.Cell(row, 5).Value = user.AssignedAssetCount;

            // No password material of any kind reaches the export, hashes included.
            sheet.Cell(row, 6).Value = user.CreatedAt.ToLocalTime();
            sheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        }

        StyleSheet(sheet, headers.Length, users.Count);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public string BuildFileName(string prefix) => $"{prefix}_{DateTime.Now:yyyy-MM-dd}.xlsx";

    private static void StyleSheet(IXLWorksheet sheet, int columnCount, int dataRowCount)
    {
        var header = sheet.Range(1, 1, 1, columnCount);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = HeaderFill;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 20;

        // Keeps the header visible while scrolling a long export.
        sheet.SheetView.FreezeRows(1);

        if (dataRowCount > 0)
        {
            sheet.Range(1, 1, dataRowCount + 1, columnCount).SetAutoFilter();
            sheet.Range(1, 1, dataRowCount + 1, columnCount)
                .Style.Border.BottomBorder = XLBorderStyleValues.Hair;
        }

        sheet.Columns().AdjustToContents();

        // AdjustToContents can leave a column absurdly wide if one cell is long, so cap it.
        foreach (var column in sheet.ColumnsUsed())
        {
            column.Width = Math.Clamp(column.Width, 10, 40);
        }
    }
}
