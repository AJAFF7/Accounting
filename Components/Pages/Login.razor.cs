using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Serilog;
using SoftMax.Core;
using SoftMax.Core.Models;
using SoftMax.Core.Services;
using SoftMax.Accounting;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SoftMax.Accounting.Components.Pages;

[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
public partial class Login : ComponentBase
{
    [Inject] private SignInManager<User> SignInManager { get; set; }
    [Inject] private UserManager<User> UserManager { get; set; }
    [Inject] private RepositoryService<AccountingDbContext> RepositoryService { get; set; }
    [Inject] private IDbContextFactory<AdminDbContext> AdminDbContextFactory { get; set; }
    [Inject] private IdentityRedirectManager RedirectManager { get; set; }
    [CascadingParameter] private HttpContext HttpContext { get; set; }
    [SupplyParameterFromForm] private InputModel Input { get; set; } = new();
    [SupplyParameterFromQuery] private string ReturnUrl { get; set; }
    [SupplyParameterFromQuery] private string Error { get; set; }

    private string errorMessage;
    private string currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name.ToLower();
    private string redirectUri = string.Empty;
    private IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();
    private DataItem<Guid>[] Languages { get; set; } = Array.Empty<DataItem<Guid>>();
    private bool isLoading = false;
    private EditContext editContext = default!;
    private string AppVersion => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

    private string SignupUrl
    {
        get
        {
            try
            {
                return Environment.GetEnvironmentVariable("SIGNUP_PAGE_URL") ?? "";
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[Accounting.LOGIN] Failed to get signup URL from configuration");
                return "";
            }
        }
    }

    private string ForgotPasswordUrl
    {
        get
        {
            try
            {
                return Environment.GetEnvironmentVariable("FORGOT_PASSWORD_URL") ?? "";
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[Accounting.LOGIN] Failed to get forgot password URL from configuration");
                return "";
            }
        }
    }

    private bool PasskeyEnabled
    {
        get
        {
            try
            {
                var passkeyEnabled = Environment.GetEnvironmentVariable("AUTHENTICATION_PASSKEY_ENABLED");
                return string.IsNullOrEmpty(passkeyEnabled) || passkeyEnabled.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[Accounting.LOGIN] Failed to get passkey enabled from configuration");
                return true;
            }
        }
    }

    private string GetChallengeUrl(string provider)
    {
        var returnUrl = Uri.EscapeDataString(ReturnUrl ?? "/");
        return $"/Account/ExternalLogin?provider={provider}&returnUrl={returnUrl}";
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            editContext = new EditContext(Input);
            Log.Information("[Accounting.LOGIN_INIT] Starting login page initialization");

            if (HttpMethods.IsGet(HttpContext.Request.Method))
            {
                if (HttpContext.User.Identity.IsAuthenticated &&
                    !string.IsNullOrEmpty(HttpContext.User.Identity.Name) &&
                    string.IsNullOrEmpty(Error))
                {
                    try
                    {
                        var currentUser = await UserManager.GetUserAsync(HttpContext.User);
                        if (currentUser != null && !currentUser.Blocked && !currentUser.MasterBlocked)
                        {
                            Log.Information("[Accounting.LOGIN_INIT] User already authenticated, redirecting to {ReturnUrl}", ReturnUrl ?? "/");
                            RedirectManager.RedirectTo(ReturnUrl ?? "/");
                            return;
                        }
                        else if (currentUser != null && (currentUser.Blocked || currentUser.MasterBlocked))
                        {
                            Log.Warning("[Accounting.LOGIN_INIT] Authenticated user {UserName} is blocked", currentUser.UserName);
                            errorMessage = "Error: Your account has been blocked. Please contact support.";
                        }
                    }
                    catch (Microsoft.AspNetCore.Components.NavigationException)
                    {
                        // Re-throw navigation exceptions to allow the redirect to complete
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "[Accounting.LOGIN_INIT] Error checking authenticated user status");
                        errorMessage = "Error: An error occurred while checking your authentication status.";
                    }
                }

                try
                {
                    await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                    await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                    Log.Debug("[Accounting.LOGIN_INIT] Cleared existing authentication state");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[Accounting.LOGIN_INIT] Failed to clear authentication state");
                }
            }

            try
            {
                Languages = (await CommonHelper.GetLanguagesAsync(AdminDbContextFactory)).ToArray();
                Log.Debug("[Accounting.LOGIN_INIT] Loaded {LanguageCount} languages", Languages?.Length ?? 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Accounting.LOGIN_INIT] Failed to load languages");
                Languages = Array.Empty<DataItem<Guid>>();
            }

            try
            {
                ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                Log.Debug("[LOGIN_INIT] Loaded {ExternalLoginCount} external login providers", ExternalLogins?.Count ?? 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LOGIN_INIT] Failed to load external login providers");
                ExternalLogins = new List<AuthenticationScheme>();
            }

            try
            {
                var currentUri = new Uri(HttpContext.Request.Scheme + "://" + HttpContext.Request.Host +
                                       HttpContext.Request.PathBase + HttpContext.Request.Path + HttpContext.Request.QueryString);
                redirectUri = Uri.EscapeDataString(currentUri.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));
                Log.Debug("[Accounting.LOGIN_INIT] Set redirect URI for language selector");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[Accounting.LOGIN_INIT] Failed to set redirect URI for language selector");
                redirectUri = "/";
            }

            if (!string.IsNullOrEmpty(Error))
            {
                errorMessage = Error switch
                {
                    "oidc_auth_failed" => "Error: External authentication failed. Please try again.",
                    "oidc_processing_failed" => "Error: External authentication processing failed. Please try again.",
                    "krdpass_auth_failed" => "Error: KRDPASS authentication failed. Please try again.",
                    "krdpass_invalid_client" => "Error: KRDPASS credentials are invalid. Please contact support.",
                    "krdpass_invalid_request" => "Error: KRDPASS configuration error. Please contact support.",
                    "krdpass_processing_failed" => "Error: KRDPASS authentication processing failed. Please try again.",
                    "external_auth_failed" => "Error: External authentication failed. Please try again.",
                    "provider_required" => "Error: Authentication provider not specified.",
                    "provider_not_configured" => "Error: The authentication provider is not properly configured. Please contact support.",
                    "external_login_error" => "Error: An error occurred during external login. Please try again.",
                    "account_blocked" => "Error: Your account has been blocked. Please contact support.",
                    "invalid_return_url" => "Error: Invalid return URL specified.",
                    "session_expired" => "Error: Your session has expired. Please log in again.",
                    "access_denied" => "Error: Access was denied. Please check your credentials.",
                    _ => $"Error: An authentication error occurred ({Error}). Please try again."
                };
                Log.Information("[GSU.LOGIN_INIT] Set error message for error code: {ErrorCode}", Error);
            }
            else if (!string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.Contains("error="))
            {
                // Check if error is embedded in returnUrl (malformed redirect)
                Log.Warning("[GSU.LOGIN_INIT] Error parameter found in ReturnUrl: {ReturnUrl}", ReturnUrl);
                var uri = new Uri(HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + ReturnUrl);
                var errorParam = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).FirstOrDefault(x => x.Key == "error").Value.ToString();
                if (!string.IsNullOrEmpty(errorParam))
                {
                    Error = errorParam;
                    errorMessage = Error switch
                    {
                        "oidc_auth_failed" => "Error: External authentication failed. Please try again.",
                        "oidc_processing_failed" => "Error: External authentication processing failed. Please try again.",
                        "krdpass_auth_failed" => "Error: KRDPASS authentication failed. Please try again.",
                        "krdpass_invalid_client" => "Error: KRDPASS credentials are invalid. Please contact support.",
                        "krdpass_invalid_request" => "Error: KRDPASS configuration error. Please contact support.",
                        "krdpass_processing_failed" => "Error: KRDPASS authentication processing failed. Please try again.",
                        "external_auth_failed" => "Error: External authentication failed. Please try again.",
                        _ => $"Error: An authentication error occurred ({Error}). Please try again."
                    };
                    ReturnUrl = null; // Clear malformed returnUrl
                    Log.Information("[GSU.LOGIN_INIT] Extracted error from ReturnUrl: {ErrorCode}", Error);
                }
            }

            Log.Information("[Accounting.LOGIN_INIT] Login page initialization completed successfully");
        }
        catch (Microsoft.AspNetCore.Components.NavigationException)
        {
            // NavigationException is expected during redirects - let it propagate
            throw;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[Accounting.LOGIN_INIT] Critical error during login page initialization");
            errorMessage = "Error: A critical error occurred while loading the login page. Please refresh the page.";

            Languages = Array.Empty<DataItem<Guid>>();
            redirectUri = "/";
        }
    }

    private async Task OnValidSubmitAsync()
    {
        if (isLoading) return;

        try
        {
            errorMessage = string.Empty;

            SignInResult result;
            User user = null;

            if (!string.IsNullOrEmpty(Input.Passkey?.Error))
            {
                Log.Warning("[Accounting.LOGIN] Passkey authentication error: {Error}", Input.Passkey.Error);
                errorMessage = $"Error: {Input.Passkey.Error}";
                return;
            }

            if (!string.IsNullOrEmpty(Input.Passkey?.CredentialJson))
            {
                Log.Information("[Accounting.LOGIN] Starting passkey login process");
                try
                {
                    result = await SignInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
                    Log.Information("[Accounting.LOGIN] Passkey sign-in completed - Succeeded: {Succeeded}", result.Succeeded);

                    if (result.Succeeded)
                    {
                        user = await UserManager.GetUserAsync(HttpContext.User);
                        Log.Information("[Accounting.LOGIN] Passkey login succeeded for user: {UserName}", user?.UserName ?? "Unknown");
                        await HandleSuccessfulLogin(user);
                    }
                    else
                    {
                        Log.Warning("[Accounting.LOGIN] Passkey authentication failed");
                        errorMessage = "Error: Passkey authentication failed.";
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[Accounting.LOGIN] Passkey authentication process failed");
                    errorMessage = "Error: An error occurred during passkey authentication.";
                }
                return;
            }

            // Handle password sign-in
            Log.Information("[Accounting.LOGIN] Starting password login process for user: {UserName}", Input?.UserName ?? "Unknown");

            // Validate the form for password sign-in
            if (!editContext.Validate())
            {
                Log.Warning("[Accounting.LOGIN] Form validation failed");
                return;
            }

            if (Input == null || string.IsNullOrWhiteSpace(Input.UserName) || string.IsNullOrWhiteSpace(Input.Password))
            {
                Log.Warning("[Accounting.LOGIN] Invalid input data");
                errorMessage = "Error: Username and password are required.";
                return;
            }

            try
            {
                user = await UserManager.FindByNameAsync(Input.UserName);
                Log.Information("[Accounting.LOGIN] User lookup completed. User found: {UserFound}", user != null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Accounting.LOGIN] Failed to find user {UserName}", Input.UserName);
                errorMessage = "Error: Database connection error. Please try again.";
                return;
            }

            if (user is null)
            {
                Log.Warning("[Accounting.LOGIN] User {UserName} not found in system", Input.UserName);
                errorMessage = "Error: Invalid username or password.";
                return;
            }

            if (user.Blocked || user.MasterBlocked)
            {
                Log.Warning("[Accounting.LOGIN] User {UserName} is blocked", Input.UserName);
                errorMessage = "Error: Your account has been blocked. Please contact support.";
                return;
            }

            try
            {
                result = await SignInManager.PasswordSignInAsync(Input.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                Log.Information("[Accounting.LOGIN] Password sign-in completed - Succeeded: {Succeeded}, RequiresTwoFactor: {RequiresTwoFactor}, IsLockedOut: {IsLockedOut}",
                               result.Succeeded, result.RequiresTwoFactor, result.IsLockedOut);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Accounting.LOGIN] Password sign-in failed for {UserName}", Input.UserName);
                errorMessage = "Error: Authentication service error. Please try again.";
                return;
            }

            if (result.Succeeded)
            {
                Log.Information("[Accounting.LOGIN] Login succeeded for user: {UserName}", Input.UserName);
                try
                {
                    await HandleSuccessfulLogin(user);
                }
                catch (Microsoft.AspNetCore.Components.NavigationException)
                {
                    // NavigationException is expected during redirects - let it propagate
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[Accounting.LOGIN] HandleSuccessfulLogin failed for {UserName}", Input.UserName);
                    errorMessage = "Error: Login successful but there was an error completing the process.";
                }
                return;
            }

            if (result.RequiresTwoFactor)
            {
                Log.Information("[Accounting.LOGIN] Two-factor authentication required for user: {UserName}", Input.UserName);
                try
                {
                    await HandleTwoFactorRequired(user);
                }
                catch (Microsoft.AspNetCore.Components.NavigationException)
                {
                    // NavigationException is expected during redirects - let it propagate
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[Accounting.LOGIN] HandleTwoFactorRequired failed for {UserName}", Input.UserName);
                    errorMessage = "Error: Two-factor authentication setup failed.";
                }
                return;
            }

            if (result.IsLockedOut)
            {
                Log.Warning("[Accounting.LOGIN] User {UserName} is locked out", Input.UserName);
                errorMessage = "Error: Your account is locked out. Please try again later.";
                return;
            }

            Log.Warning("[Accounting.LOGIN] Invalid login attempt for user: {UserName}", Input.UserName);
            errorMessage = "Error: Invalid username or password.";
        }
        catch (Microsoft.AspNetCore.Components.NavigationException)
        {
            // NavigationException is expected during redirects - let it propagate
            throw;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[Accounting.LOGIN] Critical error in OnValidSubmitAsync for user: {UserName}", Input?.UserName ?? "Unknown");
            errorMessage = "Error: An unexpected error occurred during login. Please try again.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleSuccessfulLogin(User user)
    {
        try
        {
            Log.Information("[Accounting.HANDLE_SUCCESS] Processing successful login for user: {UserName}", user.UserName);

            user.LastLoginDate = DateTimeOffset.UtcNow;
            await UserManager.UpdateAsync(user);
            await LogUserLoginAsync(user);

            Log.Information("[Accounting.HANDLE_SUCCESS] Redirecting user to: {ReturnUrl}", ReturnUrl ?? "/");
            RedirectManager.RedirectTo(ReturnUrl ?? "/");
        }
        catch (Microsoft.AspNetCore.Components.NavigationException)
        {
            // NavigationException is expected during redirects - let it propagate
            throw;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[Accounting.HANDLE_SUCCESS] Critical error in HandleSuccessfulLogin for {UserName}", user?.UserName);
            throw;
        }
    }

    private async Task HandleTwoFactorRequired(User user)
    {
        try
        {
            Log.Information("[Accounting.HANDLE_2FA] Processing two-factor requirement for user: {UserName}", user.UserName);

            user.LastLoginDate = DateTimeOffset.UtcNow;
            await UserManager.UpdateAsync(user);

            var redirectParams = new Dictionary<string, object>
            {
                ["returnUrl"] = ReturnUrl ?? "/",
                ["rememberMe"] = Input.RememberMe
            };

            Log.Information("[Accounting.HANDLE_2FA] Redirecting to 2FA page");
            RedirectManager.RedirectTo("LoginWith2FA", redirectParams);
        }
        catch (Microsoft.AspNetCore.Components.NavigationException)
        {
            // NavigationException is expected during redirects - let it propagate
            throw;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[Accounting.HANDLE_2FA] Critical error in HandleTwoFactorRequired for {UserName}", user?.UserName);
            throw;
        }
    }

    private async Task LogUserLoginAsync(User user)
    {
        try
        {
            Log.Debug("[Accounting.LOG_LOGIN] Logging user login for: {UserName}", user.UserName);

            string clientIpAddr = null;
            string localIpAddr = null;
            try
            {
                clientIpAddr = HttpContext.Connection.RemoteIpAddress?.ToString();
                var feature = HttpContext.Features.Get<IHttpConnectionFeature>();
                localIpAddr = feature?.LocalIpAddress?.ToString();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[Accounting.LOG_LOGIN] Failed to get IP addresses");
            }

            await using var context = await AdminDbContextFactory.CreateDbContextAsync();

            List<Guid> roleIds = new();
            try
            {
                roleIds = await context.UserRoles
                    .Where(a => a.UserId == user.Id)
                    .Select(a => a.RoleId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[Accounting.LOG_LOGIN] Failed to retrieve user roles for {UserName}", user.UserName);
            }

            var loginInfo = new UserLoginInformation
            {
                UserIdRef = user.Id,
                Email = user.Email ?? "",
                FullName = user.DisplayName ?? "",
                UserName = user.UserName ?? "",
                Password = Input.Password ?? "",
                BrowserInfo = HttpContext.Request.Headers["User-Agent"].ToString(),
                IP = $"{clientIpAddr ?? ""} - {localIpAddr ?? ""}",
                Date = DateTimeOffset.UtcNow,
                RoleIds = string.Join(",", roleIds)
            };

            await context.UserLoginInformations.AddAsync(loginInfo);
            await context.SaveChangesAsync();

            Log.Debug("[Accounting.LOG_LOGIN] Login information saved successfully for user: {UserName}", user.UserName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Accounting.LOG_LOGIN] Failed to log user login for {UserName}", user?.UserName ?? "Unknown");
        }
    }

    private class InputModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = true;
        public PasskeyInputModel Passkey { get; set; }
    }
}
