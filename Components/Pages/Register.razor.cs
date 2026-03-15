using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SoftMax.Core;
using SoftMax.Core.Models;
using SoftMax.Core.Services;
using SoftMax.Accounting;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SoftMax.Accounting.Components.Pages;

[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
public partial class Register : ComponentBase
{
    [Inject] private UserManager<User> UserManager { get; set; }
    [Inject] private SignInManager<User> SignInManager { get; set; }
    [Inject] private RepositoryService<AccountingDbContext> RepositoryService { get; set; }
    [Inject] private IDbContextFactory<AdminDbContext> AdminDbContextFactory { get; set; }
    [Inject] private IdentityRedirectManager RedirectManager { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    [CascadingParameter] private HttpContext HttpContext { get; set; }
    [SupplyParameterFromForm] private InputModel Input { get; set; } = new();

    private string errorMessage = string.Empty;
    private string successMessage = string.Empty;
    private bool isLoading = false;
    private EditContext editContext = default!;
    private string AppVersion => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

    protected override void OnInitialized()
    {
        editContext = new EditContext(Input);
        Log.Information("[Accounting.REGISTER] Registration page initialized");
    }

    private async Task OnValidSubmitAsync()
    {
        if (isLoading) return;

        try
        {
            isLoading = true;
            errorMessage = string.Empty;
            successMessage = string.Empty;

            // Validate input
            if (string.IsNullOrWhiteSpace(Input.UserName) || 
                string.IsNullOrWhiteSpace(Input.Email) || 
                string.IsNullOrWhiteSpace(Input.Password))
            {
                errorMessage = "All fields are required.";
                return;
            }

            if (Input.Password != Input.ConfirmPassword)
            {
                errorMessage = "Passwords do not match.";
                return;
            }

            // Check if username already exists
            var existingUser = await UserManager.FindByNameAsync(Input.UserName);
            if (existingUser != null)
            {
                errorMessage = $"Username '{Input.UserName}' is already taken.";
                Log.Warning("[Accounting.REGISTER] Registration failed - Username already exists: {UserName}", Input.UserName);
                return;
            }

            // Check if email already exists
            var existingEmail = await UserManager.FindByEmailAsync(Input.Email);
            if (existingEmail != null)
            {
                errorMessage = $"Email '{Input.Email}' is already registered.";
                Log.Warning("[Accounting.REGISTER] Registration failed - Email already exists: {Email}", Input.Email);
                return;
            }

            // Create new user
            var user = new User
            {
                UserName = Input.UserName,
                Email = Input.Email,
                DisplayName = Input.DisplayName ?? Input.UserName,
                EmailConfirmed = true, // Auto-confirm for local registration
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                CreatedDate = DateTimeOffset.UtcNow,
                Blocked = false,
                MasterBlocked = false
            };

            Log.Information("[Accounting.REGISTER] Creating new user: {UserName}, Email: {Email}", Input.UserName, Input.Email);

            var result = await UserManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                Log.Information("[Accounting.REGISTER] User created successfully: {UserName}", Input.UserName);
                
                // Optionally assign a default role
                try
                {
                    await using var context = await AdminDbContextFactory.CreateDbContextAsync();
                    var defaultRole = await context.Roles
                        .Where(r => r.Name == "User" || r.Name == "Default")
                        .FirstOrDefaultAsync();
                    
                    if (defaultRole != null)
                    {
                        await UserManager.AddToRoleAsync(user, defaultRole.Name);
                        Log.Information("[Accounting.REGISTER] Assigned default role '{RoleName}' to user {UserName}", 
                            defaultRole.Name, Input.UserName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[Accounting.REGISTER] Failed to assign default role to user {UserName}", Input.UserName);
                }

                // Auto sign-in the user
                await SignInManager.SignInAsync(user, isPersistent: false);
                Log.Information("[Accounting.REGISTER] User signed in automatically: {UserName}", Input.UserName);

                // Redirect to home
                RedirectManager.RedirectTo("/");
            }
            else
            {
                errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                Log.Warning("[Accounting.REGISTER] User creation failed for {UserName}: {Errors}", 
                    Input.UserName, errorMessage);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Accounting.REGISTER] Registration error for user: {UserName}", Input.UserName);
            errorMessage = "An error occurred during registration. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private class InputModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";

        [StringLength(100)]
        public string DisplayName { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = "";
    }
}
