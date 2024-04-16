using System.ComponentModel.DataAnnotations;
using AuthorizationServer.Models;

namespace AuthorizationServer.Pages.Update;

public class InputModel
{
    [Required]
    public string? Username { get; set; }

    [Required]
    public string? Email { get; set; }

    public string? ReturnUrl { get; set; }

    public string? Button { get; set; }

    public ApplicationUser ToApplicationUser() {
        return new () {
            UserName = Username,
            Email = Email
        };
    }
}