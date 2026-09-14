using ItAssetManagement.ViewModels;

namespace ItAssetManagement.Services;

public interface IExcelExportService
{
    /// <summary>Renders the supplied assets as a styled .xlsx workbook.</summary>
    byte[] BuildAssetWorkbook(IReadOnlyList<AssetListItemViewModel> assets);

    /// <summary>A filename carrying a date stamp, so repeated exports do not collide in a downloads folder.</summary>
    string BuildFileName(string prefix);
}
