using AuthorizationServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthorizationServer.Pages.Account.Info;

[SecurityHeaders]
[Authorize]
public class Index : PageModel
{
    public ApplicationUser? UserInfo { get; private set; }

    private readonly UserManager<ApplicationUser> _userManager;

    public Index (UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
        var id = User.Claims.First(x => x.Type == "sub");

        UserInfo = await _userManager.FindByIdAsync(id.Value);

        return Page();
    }
}