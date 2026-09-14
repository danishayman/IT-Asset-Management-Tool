using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ItAssetManagement.Infrastructure;

/// <summary>
/// Success and error banners that survive the redirect after a mutation. TempData is the
/// right store here: it is read once and then cleared, so refreshing the landing page does
/// not resurrect a message about something that already happened.
/// </summary>
public static class Notify
{
    public const string SuccessKey = "Notify.Success";
    public const string ErrorKey = "Notify.Error";

    public static void Success(this Controller controller, string message) =>
        controller.TempData[SuccessKey] = message;

    public static void Error(this Controller controller, string message) =>
        controller.TempData[ErrorKey] = message;

    public static string? TakeSuccess(this ITempDataDictionary tempData) =>
        tempData[SuccessKey] as string;

    public static string? TakeError(this ITempDataDictionary tempData) =>
        tempData[ErrorKey] as string;
}
