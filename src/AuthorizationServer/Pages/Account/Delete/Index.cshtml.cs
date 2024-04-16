using AuthorizationServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthorizationServer.Pages.Delete;

[SecurityHeaders]
[Authorize]
public class Index : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public Index(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPost()
    {
        var name = User.Claims.First(x => x.Type == "name");
        var user = await _userManager.FindByNameAsync(name.Value);
        
        if (user is not null)
        {
            var userResult = await _userManager.DeleteAsync(user);

            if (userResult.Succeeded)
            {
                await _signInManager.SignOutAsync();

                return Redirect("~/Account/Login");
            }

            foreach (var error in userResult.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
        }

        return Page();
    }
}