using AuthorizationServer.Data;
using AuthorizationServer.Models;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PR2.Shared.Common;
using SocialMediaService.WebApi.Protos;

namespace AuthorizationServer.Pages.Register;

[SecurityHeaders]
[AllowAnonymous]
public class Index : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ProfileService.ProfileServiceClient _client;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly ApplicationDbContext _context;

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public Index(
        IIdentityServerInteractionService interaction,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ProfileService.ProfileServiceClient client)
    {
        _userManager = userManager;
        _interaction = interaction;
        _context = context;
        _signInManager = signInManager;
        _client = client;
    }

    public IActionResult OnGet(string? returnUrl)
    {
        Input = new InputModel { ReturnUrl = returnUrl };
        return Page();
    }
        
    public async Task<IActionResult> OnPost()
    {
        var context = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);

        if (Input.Button != "create")
        {
            if (context != null)
            {
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                if (context.IsNativeClient())
                {
                    return this.LoadingPage(Input.ReturnUrl);
                }

                return Redirect(Input.ReturnUrl ?? "~/");
            }
            else
            {
                return Redirect("~/");
            }
        }

        if (ModelState.IsValid)
        {
            if (await _userManager.FindByNameAsync(Input.Username!) != null)
            {
                ModelState.AddModelError("Input.Username", "Invalid username");
            }

            var user = Input.ToApplicationUser();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userResult = await _userManager.CreateAsync(user, Input.Password!);

                if (userResult.Succeeded)
                {
                    var profile = new CreateProfileRequest() 
                    {
                        Id = user.Id,
                        FirstName = Input.FirstName,
                        LastName = Input.LastName,
                        DateOfBirth = Timestamp.FromDateTime(Input.DateOfBirth.ToUniversalTime()),
                        Gender = Input.Gender,
                        PhoneNumber = Input.PhoneNumber ?? string.Empty
                    };

                    await _client.CreateProfileAsync(profile);

                    await transaction.CommitAsync();

                    await _signInManager.SignInAsync(user, false);
                    
                    if (context != null)
                    {
                        if (context.IsNativeClient())
                        {
                            return this.LoadingPage(Input.ReturnUrl);
                        }

                        return Redirect(Input.ReturnUrl ?? "~/");
                    }

                    if (Url.IsLocalUrl(Input.ReturnUrl))
                    {
                        return Redirect(Input.ReturnUrl);
                    }
                    else if (string.IsNullOrEmpty(Input.ReturnUrl))
                    {
                        return Redirect("~/");
                    }
                    else
                    {
                        throw new ArgumentException("invalid return URL");
                    }
                }

                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }
            }
            catch (Exception exp)
            {
                // TODO: Create a Method for This
                if (exp is RpcException rpcException)
                {
                    foreach(var error in rpcException.Status.Detail.Split('|'))
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                }

                await transaction.RollbackAsync();
            }
        }

        return Page();
    }
}
