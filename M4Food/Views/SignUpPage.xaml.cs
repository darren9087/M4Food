using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Plugin.Firebase.Auth;
#if ANDROID
using Plugin.Firebase.Auth.Google;
#endif

namespace M4Food.Views;

public partial class SignUpPage : ContentPage
{
    private readonly IFirebaseAuth _firebaseAuth;

    public SignUpPage()
    {
        InitializeComponent();
        this.Loaded += OnPageLoaded;

        // Get Firebase Auth instance
        _firebaseAuth = CrossFirebaseAuth.Current;
    }

    // Animation on page load
    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        // Set initial state
        LogoBorder.Opacity = 0;
        WelcomeText.Opacity = 0;
        SignUpForm.Opacity = 0;
        Separator.Opacity = 0;
        GoogleButton.Opacity = 0;
        Footer.Opacity = 0;

        LogoBorder.TranslationY = 30;
        WelcomeText.TranslationY = 30;
        SignUpForm.TranslationY = 30;
        Separator.TranslationY = 30;
        GoogleButton.TranslationY = 30;
        Footer.TranslationY = 30;

        // Start animations
        await Task.WhenAll(
            LogoBorder.FadeTo(1, 400, Easing.CubicOut),
            LogoBorder.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.Delay(100);

        await Task.WhenAll(
            WelcomeText.FadeTo(1, 400, Easing.CubicOut),
            WelcomeText.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.Delay(50);

        await Task.WhenAll(
            SignUpForm.FadeTo(1, 400, Easing.CubicOut),
            SignUpForm.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.Delay(50);

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

    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        try
        {
            // Get input values
            var username = UsernameEntry.Text?.Trim();
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text;
            var confirmPassword = ConfirmPasswordEntry.Text;

            // Validate input
            if (string.IsNullOrWhiteSpace(username))
            {
                await DisplayAlert("Error", "Please enter a username.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlert("Error", "Please enter your email address.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter a password.", "OK");
                return;
            }

            if (password.Length < 6)
            {
                await DisplayAlert("Error", "Password must be at least 6 characters long.", "OK");
                return;
            }

            if (password != confirmPassword)
            {
                await DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            // CreateUserAsync returns IFirebaseUser directly
            var user = await _firebaseAuth.CreateUserAsync(email, password);

            if (user != null)
            {
                // Update user profile with username
                // Note: You may need to save the username to Firestore or Realtime Database

                await DisplayAlert("Success", $"Account created successfully! Welcome, {username}!", "OK");

                // Navigate back to login or to main page
                await Navigation.PopAsync();
                // Or navigate to main page:
                // await Shell.Current.GoToAsync("//MainPage");
            }
        }
        catch (Exception ex)
        {
            // Handle Firebase authentication errors
            string errorMessage = ex.Message switch
            {
                var msg when msg.Contains("invalid-email") || msg.Contains("INVALID_EMAIL")
                    => "Invalid email address format.",
                var msg when msg.Contains("email-already-in-use") || msg.Contains("EMAIL_EXISTS")
                    => "This email address is already registered.",
                var msg when msg.Contains("weak-password") || msg.Contains("WEAK_PASSWORD")
                    => "Password is too weak. Please use a stronger password.",
                var msg when msg.Contains("network") || msg.Contains("NETWORK")
                    => "Network error. Please check your internet connection.",
                var msg when msg.Contains("operation-not-allowed")
                    => "Email/password accounts are not enabled. Please contact support.",
                _ => $"Sign up failed: {ex.Message}"
            };

            await DisplayAlert("Sign Up Failed", errorMessage, "OK");
        }
    }

    private async void OnSignInTapped(object sender, EventArgs e)
    {
        // Navigate back to login page
        await Navigation.PopAsync();
    }

    private async void OnGoogleSignUpTapped(object sender, EventArgs e)
    {
        try
        {
            // Button press animation
            if (sender is Grid grid && grid.Parent is Border border)
            {
                await border.ScaleTo(0.9, 100, Easing.CubicOut);
                await border.ScaleTo(1, 100, Easing.CubicIn);
            }

#if ANDROID
            // Google Sign-Up with Firebase on Android
            var googleAuth = CrossFirebaseAuthGoogle.Current;

            // Use SignInWithGoogleAsync - returns IFirebaseUser directly
            var user = await googleAuth.SignInWithGoogleAsync();

            if (user != null)
            {
                await DisplayAlert("Success", $"Account created with Google! Welcome, {user.Email}!", "OK");

                // Navigate to your main/home page after successful Google sign-up
                await Navigation.PopAsync();
                // Or: await Shell.Current.GoToAsync("//MainPage");
            }
#else
            await DisplayAlert(
                "Not Supported",
                "Google Sign-Up is currently only supported on Android. iOS and other platforms require additional setup.",
                "OK");
#endif
        }
        catch (Exception ex)
        {
            string errorMessage = ex.Message switch
            {
                var msg when msg.Contains("cancelled", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("canceled", StringComparison.OrdinalIgnoreCase)
                        => "Sign up was cancelled.",
                var msg when msg.Contains("network", StringComparison.OrdinalIgnoreCase)
                        => "Network error. Please check your internet connection.",
                var msg when msg.Contains("account-exists-with-different-credential", StringComparison.OrdinalIgnoreCase)
                        => "An account already exists with the same email address but different sign-in credentials.",
                var msg when msg.Contains("invalid-credential", StringComparison.OrdinalIgnoreCase)
                        => "The credential is invalid or has expired.",
                var msg when msg.Contains("operation-not-allowed", StringComparison.OrdinalIgnoreCase)
                        => "Google sign-up is not enabled. Please check Firebase console settings.",
                _ => $"Google sign-up failed: {ex.Message}"
            };

            await DisplayAlert("Sign Up Failed", errorMessage, "OK");
        }
    }
}