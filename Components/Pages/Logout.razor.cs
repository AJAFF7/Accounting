using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using SoftMax.Core;
using SoftMax.Core.Models;
using System.Security.Claims;

namespace SoftMax.Accounting.Components.Pages;

public partial class Logout : ComponentBase
{
    [Inject] private SignInManager<User> SignInManager { get; set; }
    [Inject] private IDistributedCache Cache { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private RepositoryService<AccountingDbContext> RepositoryService { get; set; }
    [CascadingParameter] private HttpContext HttpContext { get; set; }
    [SupplyParameterFromQuery] private string ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Log.Information("[Accounting.LOGOUT] Starting logout process");

            var user = HttpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        await Cache.RemoveAsync($"User_{userId}");
                        Log.Debug("[Accounting.LOGOUT] Cleared cache for user: {UserId}", userId);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "[Accounting.LOGOUT] Failed to clear user cache");
                    }
                }

                await SignInManager.SignOutAsync();
                Log.Information("[Accounting.LOGOUT] User signed out successfully");
            }
            else
            {
                Log.Information("[Accounting.LOGOUT] No authenticated user to log out");
            }

            // Local authentication - redirect to login page
            var loginUrl = AppConstants.Authentication.DefaultLoginPath;
            if (!string.IsNullOrEmpty(ReturnUrl))
                loginUrl += $"?ReturnUrl={Uri.EscapeDataString(ReturnUrl)}";

            Log.Information("[Accounting.LOGOUT] Redirecting to: {LoginUrl}", loginUrl);
            NavigationManager.NavigateTo(loginUrl, forceLoad: true);
        }
        catch (Microsoft.AspNetCore.Components.NavigationException)
        {
            // NavigationException is expected during redirects - let it propagate
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Accounting.LOGOUT] Critical error during logout");
            NavigationManager.NavigateTo(AppConstants.Authentication.DefaultLoginPath, forceLoad: true);
        }
    }
}
