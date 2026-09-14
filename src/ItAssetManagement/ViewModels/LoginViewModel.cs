using System.ComponentModel.DataAnnotations;

namespace ItAssetManagement.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Where to send the user once signed in. Only ever honoured if it is a local URL.</summary>
    public string? ReturnUrl { get; set; }
}
