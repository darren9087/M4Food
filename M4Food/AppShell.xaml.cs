using M4Food.Views;

namespace M4Food
{
    // Ensure the class is declared as partial and matches the x:Class in AppShell.xaml
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        }
    }
}

