using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks; // <-- ADDED

namespace M4Food.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        this.Loaded += OnPageLoaded;
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
        // TODO: Implement your traditional login logic here
        await DisplayAlert("Login Attempt", "Traditional login logic will go here!", "OK");
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Forgot Password", "Navigate to password reset page or send reset email.", "OK");
    }

    private async void OnGoogleSignInTapped(object sender, EventArgs e)
    {
        if (sender is Grid grid && grid.Parent is Border border)
        {
            await border.ScaleTo(0.9, 100, Easing.CubicOut);
            await border.ScaleTo(1, 100, Easing.CubicIn);
        }

        await DisplayAlert("Google Sign In", "Google authentication will be implemented here!", "OK");
    }

    // 3. ADDED SIGN UP EVENT HANDLER
    private async void OnSignUpTapped(object sender, EventArgs e)
    {
        // TODO: Navigate to your Sign Up Page
        // Example: await Shell.Current.GoToAsync("SignUpPage");
        await DisplayAlert("Sign Up", "Navigate to Sign Up Page here!", "OK");
    }
}