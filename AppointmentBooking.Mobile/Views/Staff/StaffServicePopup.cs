using AppointmentBooking.Mobile.ViewModels.Staff;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

namespace AppointmentBooking.Mobile.Views.Staff;

/// <summary>Create / edit form for a service, styled like the break/vacation editor.</summary>
public sealed class StaffServicePopup : Popup<bool>
{
    private readonly StaffServicesViewModel viewModel;
    private bool isClosing;

    public StaffServicePopup(StaffServicesViewModel viewModel)
    {
        this.viewModel = viewModel;
        CanBeDismissedByTappingOutsideOfPopup = false;
        BackgroundColor = GetColor("QuickBookingPanelBackground", Color.FromArgb("#14264A"));
        Padding = 0;

        Content = new Border
        {
            WidthRequest = 340,
            MaximumWidthRequest = 400,
            MaximumHeightRequest = 680,
            Padding = 22,
            BackgroundColor = GetColor("QuickBookingPanelBackground", Color.FromArgb("#14264A")),
            Stroke = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
            StrokeThickness = 0.8,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Content = CreateForm()
        };
    }

    private View CreateForm()
    {
        Entry name = CreateEntry("npr. Muško šišanje", Keyboard.Text, StaffServicesViewModel.NameMaxLength);
        name.SetBinding(Entry.TextProperty, nameof(StaffServicesViewModel.FormName));

        Editor description = new()
        {
            Placeholder = "Kratak opis usluge (opcionalno)",
            AutoSize = EditorAutoSizeOption.TextChanges,
            MaxLength = StaffServicesViewModel.DescriptionMaxLength,
            MinimumHeightRequest = 84,
            BackgroundColor = Colors.Transparent,
            TextColor = GetColor("TextOnBrandBackground", Colors.White),
            PlaceholderColor = Color.FromArgb("#8A9BB5")
        };
        description.SetBinding(Editor.TextProperty, nameof(StaffServicesViewModel.FormDescription));

        Label descriptionCounter = CreateLabel(string.Empty, 11, bold: false);
        descriptionCounter.Opacity = 0.55;
        descriptionCounter.HorizontalTextAlignment = TextAlignment.End;
        descriptionCounter.SetBinding(
            Label.TextProperty,
            new Binding(
                nameof(StaffServicesViewModel.FormDescription),
                converter: new TextLengthConverter(StaffServicesViewModel.DescriptionMaxLength)));

        Entry duration = CreateEntry("30", Keyboard.Numeric, 3);
        duration.SetBinding(Entry.TextProperty, nameof(StaffServicesViewModel.FormDuration));

        Entry price = CreateEntry("25,00", Keyboard.Numeric, 12);
        price.SetBinding(Entry.TextProperty, nameof(StaffServicesViewModel.FormPrice));

        Grid numbers = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        View durationField = CreateField("TRAJANJE", CreateSuffixedInput(duration, "min"));
        View priceField = CreateField("CIJENA", CreateSuffixedInput(price, "KM"));
        numbers.Children.Add(durationField);
        numbers.Children.Add(priceField);
        Grid.SetColumn(priceField, 1);

        Label error = CreateLabel(string.Empty, 13, bold: true);
        error.TextColor = GetColor("Error", Color.FromArgb("#FF6B6B"));
        error.SetBinding(Label.TextProperty, nameof(StaffServicesViewModel.FormError));
        error.SetBinding(IsVisibleProperty, nameof(StaffServicesViewModel.HasFormError));

        Button cancelButton = CreateActionButton("Odustani", isPrimary: false);
        cancelButton.Clicked += OnCancelClicked;
        Button saveButton = CreateActionButton("Sačuvaj", isPrimary: true);
        saveButton.Clicked += OnSaveClicked;
        saveButton.SetBinding(IsEnabledProperty, nameof(StaffServicesViewModel.CanSave));

        ActivityIndicator saving = new()
        {
            Color = GetColor("BrandButton", Color.FromArgb("#E7C45A")),
            HeightRequest = 24,
            HorizontalOptions = LayoutOptions.Center
        };
        saving.SetBinding(ActivityIndicator.IsRunningProperty, nameof(StaffServicesViewModel.IsSaving));
        saving.SetBinding(IsVisibleProperty, nameof(StaffServicesViewModel.IsSaving));

        Grid actions = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10,
            Children = { cancelButton, saveButton }
        };
        Grid.SetColumn(saveButton, 1);

        VerticalStackLayout form = new()
        {
            Spacing = 16,
            Children =
            {
                CreateLabel(viewModel.FormTitle, 20, bold: true),
                CreateField("NAZIV", CreateInputBorder(name, new Thickness(12, 0))),
                CreateField("OPIS", new VerticalStackLayout
                {
                    Spacing = 4,
                    Children = { CreateInputBorder(description, new Thickness(12, 6)), descriptionCounter }
                }),
                numbers,
                error,
                saving,
                actions
            }
        };

        return new ScrollView
        {
            BindingContext = viewModel,
            Content = form
        };
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (isClosing)
        {
            return;
        }

        if (await viewModel.SaveAsync())
        {
            await CloseOnceAsync(true);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (isClosing || viewModel.IsSaving)
        {
            return;
        }

        viewModel.CancelForm();
        await CloseOnceAsync(false);
    }

    private async Task CloseOnceAsync(bool result)
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        await CloseAsync(result);
    }

    private static View CreateField(string title, View input)
    {
        Label label = CreateLabel(title, 11, bold: true);
        label.CharacterSpacing = 1;
        label.Opacity = 0.6;

        return new VerticalStackLayout
        {
            Spacing = 6,
            Children = { label, input }
        };
    }

    private static Border CreateSuffixedInput(Entry entry, string suffix)
    {
        Label suffixLabel = CreateLabel(suffix, 13, bold: true);
        suffixLabel.TextColor = GetColor("BrandButton", Color.FromArgb("#E7C45A"));
        suffixLabel.VerticalOptions = LayoutOptions.Center;

        Grid content = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 6,
            Children = { entry, suffixLabel }
        };
        Grid.SetColumn(suffixLabel, 1);
        return CreateInputBorder(content, new Thickness(12, 0));
    }

    private static Entry CreateEntry(string placeholder, Keyboard keyboard, int maxLength) => new()
    {
        Placeholder = placeholder,
        Keyboard = keyboard,
        MaxLength = maxLength,
        HeightRequest = 48,
        BackgroundColor = Colors.Transparent,
        TextColor = GetColor("TextOnBrandBackground", Colors.White),
        PlaceholderColor = Color.FromArgb("#8A9BB5")
    };

    private static Label CreateLabel(string text, double fontSize, bool bold) => new()
    {
        Text = text,
        FontSize = fontSize,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        TextColor = GetColor("TextOnBrandBackground", Colors.White)
    };

    private static Border CreateInputBorder(View content, Thickness padding) => new()
    {
        Padding = padding,
        BackgroundColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
        Stroke = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
        StrokeThickness = 0.8,
        StrokeShape = new RoundRectangle { CornerRadius = 13 },
        Content = content
    };

    private static Button CreateActionButton(string text, bool isPrimary) => new()
    {
        Text = text,
        BackgroundColor = isPrimary
            ? GetColor("BrandButton", Color.FromArgb("#E7C45A"))
            : GetColor("BrandBackground", Color.FromArgb("#07152F")),
        TextColor = isPrimary
            ? GetColor("BrandBackground", Color.FromArgb("#07152F"))
            : GetColor("TextOnBrandBackground", Colors.White),
        BorderColor = isPrimary
            ? GetColor("BrandButton", Color.FromArgb("#E7C45A"))
            : GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
        BorderWidth = isPrimary ? 0 : 1,
        FontAttributes = FontAttributes.Bold,
        HeightRequest = 50,
        CornerRadius = 14
    };

    private static Color GetColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color color
            ? color
            : fallback;

    private sealed class TextLengthConverter(int maxLength) : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
            $"{(value as string)?.Length ?? 0}/{maxLength}";

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
