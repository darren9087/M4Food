using Microsoft.Maui.Controls; // <-- ADDED

namespace M4Food;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}