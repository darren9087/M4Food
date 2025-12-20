namespace M4Food.Views;

public partial class MapPage : ContentPage
{
    public MapPage()
    {
        InitializeComponent();

        string address = "1 Jalan Universiti 96000 Sibu Sarawak";
        string mapUrl =
            $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(address)}";

        MapWebView.Source = mapUrl;
    }
}
