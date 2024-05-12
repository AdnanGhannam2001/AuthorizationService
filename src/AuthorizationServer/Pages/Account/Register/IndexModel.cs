using System.ComponentModel.DataAnnotations;
using AuthorizationServer.Models;
using SocialMediaService.WebApi.Protos;

namespace AuthorizationServer.Pages.Register;

public class InputModel
{
    [Required]
    public string Username { get; set; } = "";

    [Required]
    public string FirstName { get; set; } = "";

    [Required]
    public string LastName { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    [Required]
    public string Email { get; set; } = "";

    public string? PhoneNumber { get; set; }

    [Required]
    public Genders Gender { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    public string? ReturnUrl { get; set; }

    public string? Button { get; set; }

    public ApplicationUser ToApplicationUser()
    {
        return new ()
        {
            UserName = Username,
            Email = Email
        };
    }
}