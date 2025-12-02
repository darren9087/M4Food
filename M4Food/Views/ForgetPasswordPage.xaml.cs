using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Plugin.Firebase.Auth;

namespace M4Food.Views;

public partial class ForgetPasswordPage : ContentPage
{
    private readonly IFirebaseAuth _firebaseAuth;

    public ForgetPasswordPage()
    {
        InitializeComponent();
        this.Loaded += OnPageLoaded;

        // Get Firebase Auth instance
        _firebaseAuth = CrossFirebaseAuth.Current;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        // Set initial state
        TitleSection.Opacity = 0;
        EmailForm.Opacity = 0;
        BackSection.Opacity = 0;

        TitleSection.TranslationY = 30;
        EmailForm.TranslationY = 30;
        BackSection.TranslationY = 30;

        // Start animations (Logo animation removed)
        await Task.WhenAll(
            TitleSection.FadeTo(1, 400, Easing.CubicOut),
            TitleSection.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.Delay(50);

        await Task.WhenAll(
            EmailForm.FadeTo(1, 400, Easing.CubicOut),
            EmailForm.TranslateTo(0, 0, 400, Easing.CubicOut)
        );

        await Task.Delay(50);

        await Task.WhenAll(
            BackSection.FadeTo(1, 400, Easing.CubicOut),
            BackSection.TranslateTo(0, 0, 400, Easing.CubicOut)
        );
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Error", "Please enter your email address.", "OK");
            return;
        }

        // Basic email validation
        if (!email.Contains("@") || !email.Contains("."))
        {
            await DisplayAlert("Error", "Please enter a valid email address.", "OK");
            return;
        }

        // Button animation
        await SendButton.ScaleTo(0.95, 100, Easing.CubicOut);
        await SendButton.ScaleTo(1, 100, Easing.CubicIn);

        // UI TESTING MODE - Show success message without Firebase
        await DisplayAlert("Success",
            $"If an account exists with {email}, you will receive a password reset link shortly.",
            "OK");
        await Navigation.PopAsync();

        /* UNCOMMENT THIS WHEN READY FOR REAL FIREBASE AUTH
        try
        {
            await _firebaseAuth.SendPasswordResetEmailAsync(email);
            
            await DisplayAlert("Success", 
                $"Password reset email sent to {email}. Please check your inbox.", 
                "OK");
            
            // Navigate back to login page
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            string errorMessage = ex.Message switch
            {
                var msg when msg.Contains("invalid-email") || msg.Contains("INVALID_EMAIL") 
                    => "Invalid email address format.",
                var msg when msg.Contains("user-not-found") || msg.Contains("USER_NOT_FOUND") 
                    => "No account found with this email address.",
                var msg when msg.Contains("network") || msg.Contains("NETWORK") 
                    => "Network error. Please check your internet connection.",
                var msg when msg.Contains("too-many-requests") || msg.Contains("TOO_MANY_REQUESTS") 
                    => "Too many requests. Please try again later.",
                _ => $"Failed to send reset email: {ex.Message}"
            };
            
            await DisplayAlert("Error", errorMessage, "OK");
        }
        */
    }

    private async void OnBackToLoginTapped(object sender, EventArgs e)
    {
        // Navigate back to login page
        await Navigation.PopAsync();
    }
}