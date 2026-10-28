using CommunityToolkit.Maui.Views;

namespace AppointmentBooking.Mobile.Views.Shared;

public sealed class DeleteAccountPopup : Popup<string?>
{
    private readonly Entry passwordEntry;
    private readonly Button deleteButton = null!;

    public DeleteAccountPopup()
    {
        CanBeDismissedByTappingOutsideOfPopup = false;
        BackgroundColor = Color.FromArgb("#14264A");
        Padding = 0;

        passwordEntry = new Entry
        {
            Placeholder = "Trenutna lozinka",
            IsPassword = true,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#AAB7CC"),
            BackgroundColor = Colors.Transparent,
            HeightRequest = 48,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };
        passwordEntry.TextChanged += (_, _) =>
            deleteButton.IsEnabled = !string.IsNullOrWhiteSpace(passwordEntry.Text);

        Button cancelButton = CreateButton(
            "ODUSTANI",
            Color.FromArgb("#07152F"),
            Colors.White,
            Color.FromArgb("#2B3D5C"));
        cancelButton.Clicked += async (_, _) => await CloseAsync(null);

        deleteButton = CreateButton(
            "IZBRIŠI NALOG",
            Color.FromArgb("#07152F"),
            Color.FromArgb("#F07A7A"),
            Color.FromArgb("#F07A7A"));
        deleteButton.IsEnabled = false;
        deleteButton.Clicked += async (_, _) => await CloseAsync(passwordEntry.Text?.Trim());

        VerticalStackLayout content = new()
        {
            Spacing = 16,
            Children =
            {
                new Label
                {
                    Text = "Brisanje naloga",
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                },
                new Label
                {
                    Text = "Ova radnja je trajna. Unesite trenutnu lozinku da biste potvrdili brisanje naloga.",
                    FontSize = 14,
                    TextColor = Colors.White
                },
                new Border
                {
                    BackgroundColor = Color.FromArgb("#07152F"),
                    Stroke = Color.FromArgb("#2B3D5C"),
                    StrokeThickness = 0.8,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                    Padding = new Thickness(12, 0),
                    Content = passwordEntry
                },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Star)
                    },
                    ColumnSpacing = 10,
                    Children = { cancelButton, deleteButton }
                }
            }
        };
        Grid.SetColumn(deleteButton, 1);

        Content = new Border
        {
            BackgroundColor = Color.FromArgb("#14264A"),
            Stroke = Color.FromArgb("#2B3D5C"),
            StrokeThickness = 0.8,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            Padding = 22,
            WidthRequest = 340,
            Content = content
        };
    }

    private static Button CreateButton(string text, Color background, Color foreground, Color border) => new()
    {
        Text = text,
        BackgroundColor = background,
        TextColor = foreground,
        FontAttributes = FontAttributes.Bold,
        FontSize = 12,
        CornerRadius = 14,
        HeightRequest = 50,
        BorderColor = border,
        BorderWidth = 1
    };
}
