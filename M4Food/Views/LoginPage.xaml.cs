using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Plugin.Firebase.Auth;
#if ANDROID
using Microsoft.Maui.ApplicationModel;
using Plugin.Firebase.Auth.Google;
#endif

namespace M4Food.Views;

public partial class LoginPage : ContentPage
{
    private readonly IFirebaseAuth _firebaseAuth;

    public LoginPage()
    {
        InitializeComponent();
        this.Loaded += OnPageLoaded;
        
        // Get Firebase Auth instance
        _firebaseAuth = CrossFirebaseAuth.Current;
    }

    // 4. UPDATED ANIMATION LOGIC
    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        // Set initial state (already set in XAML, but good practice)
        LogoBorder.Opacity = 0;
        WelcomeText.Opacity = 0;
        LoginForm.Opacity = 0;
        Separator.Opacity = 0;
        GoogleButton.Opacity = 0;
        Footer.Opacity = 0;

        LogoBorder.TranslationY = 30;
        WelcomeText.TranslationY = 30;
        LoginForm.TranslationY = 30;
        Separator.TranslationY = 30;
        GoogleButton.TranslationY = 30;
        Footer.TranslationY = 30;

        // Start animations
        await Task.WhenAll(
            LogoBorder.FadeTo(1, 400, Easing.CubicOut),
            LogoBorder.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.Delay(100); // Small delay

        await Task.WhenAll(
            WelcomeText.FadeTo(1, 400, Easing.CubicOut),
            WelcomeText.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.Delay(50); // Small delay

        await Task.WhenAll(
            LoginForm.FadeTo(1, 400, Easing.CubicOut),
            LoginForm.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.Delay(50); // Small delay

        await Task.WhenAll(
            Separator.FadeTo(1, 400, Easing.CubicOut),
            Separator.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.WhenAll(
            GoogleButton.FadeTo(1, 400, Easing.CubicOut),
            GoogleButton.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.WhenAll(
            Footer.FadeTo(1, 400, Easing.CubicOut),
            Footer.TranslateTo(0, 0, 400, Easing.CubicOut)
        );
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // Get email and password from input fields
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text;

            // Validate input
            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlert("Error", "Please enter your email address.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter your password.", "OK");
                return;
            }

            // Sign in with Firebase
            var user = await _firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
            
            if (user != null)
            {
                // Login successful!
                await DisplayAlert("Success", $"Welcome back, {user.Email}!", "OK");
                
                // Navigate to main page or home page
                // Example: await Shell.Current.GoToAsync("//MainPage");
            }
        }
        catch (Exception ex)
        {
            // Handle Firebase authentication errors
            string errorMessage = ex.Message switch
            {
                var msg when msg.Contains("invalid-email") || msg.Contains("INVALID_EMAIL") => "Invalid email address format.",
                var msg when msg.Contains("user-disabled") || msg.Contains("USER_DISABLED") => "This account has been disabled.",
                var msg when msg.Contains("user-not-found") || msg.Contains("USER_NOT_FOUND") => "No account found with this email.",
                var msg when msg.Contains("wrong-password") || msg.Contains("WRONG_PASSWORD") || msg.Contains("INVALID_PASSWORD") => "Incorrect password.",
                var msg when msg.Contains("network") || msg.Contains("NETWORK") => "Network error. Please check your internet connection.",
                _ => $"Login failed: {ex.Message}"
            };
            
            await DisplayAlert("Login Failed", errorMessage, "OK");
        }
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Forgot Password", "Navigate to password reset page or send reset email.", "OK");
    }

    private async void OnGoogleSignInTapped(object sender, EventArgs e)
    {
        try
        {
            // Button press animation for better UX
            if (sender is Grid grid && grid.Parent is Border border)
            {
                await border.ScaleTo(0.9, 100, Easing.CubicOut);
                await border.ScaleTo(1, 100, Easing.CubicIn);
            }

#if ANDROID
            // Google Sign-In with Firebase on Android
            var googleAuth = CrossFirebaseAuthGoogle.Current;

            var user = await googleAuth.SignInWithGoogleAsync();

            if (user != null)
            {
                await DisplayAlert("Success", $"Signed in as {user.Email}.", "OK");

                // TODO: Navigate to your main/home page after successful Google sign-in
                // Example: await Shell.Current.GoToAsync("//MainPage");
            }
#else
            await DisplayAlert(
                "Not Supported",
                "Google Sign-In is currently only supported on Android. iOS and other platforms require additional setup.",
                "OK");
#endif
        }
        catch (Exception ex)
        {
            string errorMessage = ex.Message switch
            {
                var msg when msg.Contains("cancelled", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("canceled", StringComparison.OrdinalIgnoreCase)
                        => "Sign in was cancelled.",
                var msg when msg.Contains("network", StringComparison.OrdinalIgnoreCase)
                        => "Network error. Please check your internet connection.",
                var msg when msg.Contains("account-exists-with-different-credential", StringComparison.OrdinalIgnoreCase)
                        => "An account already exists with the same email address but different sign-in credentials.",
                var msg when msg.Contains("invalid-credential", StringComparison.OrdinalIgnoreCase)
                        => "The credential is invalid or has expired.",
                var msg when msg.Contains("operation-not-allowed", StringComparison.OrdinalIgnoreCase)
                        => "Google sign-in is not enabled. Please check Firebase console settings.",
                _ => $"Google sign-in failed: {ex.Message}"
            };

            await DisplayAlert("Sign In Failed", errorMessage, "OK");
        }
    }

    // 3. ADDED SIGN UP EVENT HANDLER
    private async void OnSignUpTapped(object sender, EventArgs e)
    {
        // TODO: Navigate to your Sign Up Page
        // Example: await Shell.Current.GoToAsync("SignUpPage");
        await DisplayAlert("Sign Up", "Navigate to Sign Up Page here!", "OK");
    }
}