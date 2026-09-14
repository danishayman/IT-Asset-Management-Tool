namespace ItAssetManagement.ViewModels;

public class ErrorViewModel
{
    /// <summary>Correlation id for this request, so a user can quote it and it can be found in the logs.</summary>
    public string? CorrelationId { get; set; }

    public bool ShowCorrelationId => !string.IsNullOrWhiteSpace(CorrelationId);
}
