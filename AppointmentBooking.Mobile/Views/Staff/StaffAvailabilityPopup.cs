using System.ComponentModel;
using AppointmentBooking.Mobile.ViewModels.Staff;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Shapes;
using AppointmentBooking.Mobile.Views.Shared;

namespace AppointmentBooking.Mobile.Views.Staff;

public sealed class StaffAvailabilityPopup : Popup<bool>
{
    private readonly StaffAvailabilityViewModel viewModel;
    private readonly Page parentPage;
    private Grid? popupContent;
    private Grid? timeSelectorOverlay;
    private bool isDatePickerOpen;
    private bool isClosing;

    public StaffAvailabilityPopup(StaffAvailabilityViewModel viewModel, Page parentPage)
    {
        this.viewModel = viewModel;
        this.parentPage = parentPage;
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

        // Subscribe only while the popup is in the visual tree, so a popup closed by any
        // path (including the platform back button) does not stay referenced by the view model.
        Loaded += OnPopupLoaded;
        Unloaded += OnPopupUnloaded;
    }

    private void OnPopupLoaded(object? sender, EventArgs e)
    {
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnPopupUnloaded(object? sender, EventArgs e) =>
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;

    private View CreateForm()
    {
        Button breakButton = CreateTypeButton("Pauza", nameof(viewModel.BreakTypeBackground), nameof(viewModel.BreakTypeTextColor));
        breakButton.Command = viewModel.SelectBreakCommand;
        Button vacationButton = CreateTypeButton("Godišnji odmor", nameof(viewModel.VacationTypeBackground), nameof(viewModel.VacationTypeTextColor));
        vacationButton.Command = viewModel.SelectVacationCommand;

        Grid typeSelector = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8,
            Children = { breakButton, vacationButton }
        };
        Grid.SetColumn(vacationButton, 1);
        Border typePanel = new()
        {
            Padding = 4,
            BackgroundColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
            Stroke = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
            StrokeThickness = 0.8,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = typeSelector
        };

        VerticalStackLayout breakFields = new()
        {
            Spacing = 12,
            Children =
            {
                CreateLabel("Datum pauze"),
                CreateCalendarDateButton(nameof(viewModel.BreakDateDisplay), OnBreakDateClicked),
                CreateLabel("Vrijeme pauze"),
                CreateTimeRow(nameof(viewModel.BreakStartsAtDisplay), viewModel.OpenBreakStartTimeSelectorCommand,
                    nameof(viewModel.BreakEndsAtDisplay), viewModel.OpenBreakEndTimeSelectorCommand)
            }
        };
        breakFields.SetBinding(IsVisibleProperty, nameof(viewModel.IsBreakSelected));

        VerticalStackLayout vacationFields = new()
        {
            Spacing = 12,
            Children =
            {
                CreateLabel("Početak godišnjeg odmora"),
                CreateCalendarDateButton(nameof(viewModel.VacationStartsOnDisplay), OnVacationStartDateClicked),
                CreateLabel("Završetak godišnjeg odmora"),
                CreateCalendarDateButton(nameof(viewModel.VacationEndsOnDisplay), OnVacationEndDateClicked)
            }
        };
        vacationFields.SetBinding(IsVisibleProperty, nameof(viewModel.IsVacationSelected));

        Editor note = new()
        {
            Placeholder = "Napomena (opcionalno)",
            AutoSize = EditorAutoSizeOption.TextChanges,
            MaxLength = 500,
            MinimumHeightRequest = 72,
            BackgroundColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#8A9BB5")
        };
        note.SetBinding(Editor.TextProperty, nameof(viewModel.Note));
        Border noteField = CreateInputBorder(note, new Thickness(12, 8));

        Button saveButton = new()
        {
            Text = "Sačuvaj",
            BackgroundColor = GetColor("BrandButton", Color.FromArgb("#E7C45A")),
            TextColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
            BorderColor = GetColor("BrandButton", Color.FromArgb("#E7C45A")),
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50,
            CornerRadius = 14
        };
        saveButton.Clicked += OnSaveClicked;

        Button cancelButton = new()
        {
            Text = "Odustani",
            BackgroundColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
            BorderColor = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
            BorderWidth = 1,
            TextColor = GetColor("TextOnBrandBackground", Colors.Black),
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50,
            CornerRadius = 14
        };
        cancelButton.Clicked += OnCancelClicked;

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
            BindingContext = viewModel,
            Spacing = 16,
            Children =
            {
                CreateLabel(viewModel.IsEditing ? "Uredi odsustvo" : "Novo odsustvo", 20, true),
                typePanel,
                breakFields,
                vacationFields,
                noteField,
                actions
            }
        };

        // The time selector overlay is created lazily on first use, so it does not
        // add to the cost of opening the popup.
        popupContent = new Grid { BindingContext = viewModel };
        popupContent.Children.Add(new ScrollView { Content = form });
        return popupContent;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StaffAvailabilityViewModel.IsFormTimeSelectorVisible) &&
            viewModel.IsFormTimeSelectorVisible &&
            timeSelectorOverlay is null &&
            popupContent is not null)
        {
            timeSelectorOverlay = CreateTimeSelectorOverlay();
            popupContent.Children.Add(timeSelectorOverlay);
        }
    }

    private static Button CreateTypeButton(string text, string backgroundProperty, string textColorProperty)
    {
        Button button = new()
        {
            Text = text,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 44,
            CornerRadius = 10
        };
        button.SetBinding(BackgroundColorProperty, backgroundProperty);
        button.SetBinding(Button.TextColorProperty, textColorProperty);
        return button;
    }

    private static Label CreateLabel(string text, double fontSize = 14, bool bold = true) => new()
    {
        Text = text,
        FontSize = fontSize,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        TextColor = GetColor("TextOnBrandBackground", Colors.Black)
    };

    private Button CreateCalendarDateButton(string displayProperty, EventHandler clicked)
    {
        Button button = new()
        {
            BackgroundColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
            TextColor = GetColor("TextOnBrandBackground", Colors.White),
            BorderColor = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
            BorderWidth = 0.8,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48,
            CornerRadius = 13,
            HorizontalOptions = LayoutOptions.Fill
        };
        button.SetBinding(Button.TextProperty, displayProperty);
        button.Clicked += clicked;
        return button;
    }

    private async void OnBreakDateClicked(object? sender, EventArgs e)
    {
        if (await PickDateAsync(viewModel.BreakDate, viewModel.Today, "Datum pauze") is DateTime date)
        {
            viewModel.BreakDate = date;
        }
    }

    private async void OnVacationStartDateClicked(object? sender, EventArgs e)
    {
        if (await PickDateAsync(viewModel.VacationStartsOn, viewModel.Tomorrow, "Početak godišnjeg odmora") is DateTime date)
        {
            viewModel.VacationStartsOn = date;
        }
    }

    private async void OnVacationEndDateClicked(object? sender, EventArgs e)
    {
        if (await PickDateAsync(viewModel.VacationEndsOn, viewModel.VacationStartsOn, "Završetak godišnjeg odmora") is DateTime date)
        {
            viewModel.VacationEndsOn = date;
        }
    }

    private async Task<DateTime?> PickDateAsync(DateTime selectedDate, DateTime minimumDate, string title)
    {
        // Ignore repeated taps while the calendar is already opening or open.
        if (isDatePickerOpen || isClosing)
        {
            return null;
        }

        isDatePickerOpen = true;
        try
        {
            var result = await parentPage.ShowPopupAsync<DateTime?>(
                new CalendarDatePickerPopup(selectedDate, minimumDate, title));
            return result.Result;
        }
        finally
        {
            isDatePickerOpen = false;
        }
    }

    private static Grid CreateTimeRow(
        string startDisplayProperty,
        IRelayCommand startCommand,
        string endDisplayProperty,
        IRelayCommand endCommand)
    {
        Button start = CreateTimeButton(startDisplayProperty, startCommand);
        Button end = CreateTimeButton(endDisplayProperty, endCommand);
        VerticalStackLayout startField = new()
        {
            Spacing = 6,
            Children = { CreateLabel("Početak", 13, true), start }
        };
        VerticalStackLayout endField = new()
        {
            Spacing = 6,
            Children = { CreateLabel("Završetak", 13, true), end }
        };

        Grid row = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12,
            Children = { startField, endField }
        };
        Grid.SetColumn(endField, 1);
        return row;
    }

    private static Button CreateTimeButton(string displayProperty, IRelayCommand command)
    {
        Button button = new()
        {
            BackgroundColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
            TextColor = GetColor("BrandButton", Color.FromArgb("#E7C45A")),
            BorderColor = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
            BorderWidth = 1,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48,
            CornerRadius = 12,
            Command = command
        };
        button.SetBinding(Button.TextProperty, displayProperty);
        return button;
    }

    private static Label CreateBoundLabel(string propertyName)
    {
        Label label = CreateLabel(string.Empty, 13, true);
        label.TextColor = GetColor("BrandButton", Color.FromArgb("#E7C45A"));
        label.SetBinding(Label.TextProperty, propertyName);
        return label;
    }

    private Grid CreateTimeSelectorOverlay()
    {
        Grid overlay = new()
        {
            BackgroundColor = Color.FromArgb("#B3000000"),
            Padding = 16,
            ZIndex = 1
        };
        overlay.SetBinding(IsVisibleProperty, nameof(viewModel.IsFormTimeSelectorVisible));

        Entry hourEntry = CreateTimeEntry(nameof(viewModel.TimeSelectorHour));
        Entry minuteEntry = CreateTimeEntry(nameof(viewModel.TimeSelectorMinute));
        Border hourField = CreateEntryField("SAT", hourEntry);
        Border minuteField = CreateEntryField("MINUTE", minuteEntry);
        Label colon = CreateLabel(":", 30, true);
        colon.VerticalOptions = LayoutOptions.End;
        Grid timeFields = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10,
            Children = { hourField, colon, minuteField }
        };
        Grid.SetColumn(colon, 1);
        Grid.SetColumn(minuteField, 2);

        Button cancelButton = CreatePopupActionButton("Odustani", isPrimary: false);
        cancelButton.Command = viewModel.CloseTimeSelectorCommand;
        Button confirmButton = CreatePopupActionButton("Potvrdi", isPrimary: true);
        confirmButton.Command = viewModel.ConfirmTimeSelectorCommand;
        Grid actions = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10,
            Children = { cancelButton, confirmButton }
        };
        Grid.SetColumn(confirmButton, 1);

        VerticalStackLayout content = new()
        {
            Spacing = 16,
            Children =
            {
                CreateBoundLabel(nameof(viewModel.TimeSelectorTitle)),
                CreateLabel("Unesite sat i minute u formatu 24 sata.", 13, false),
                timeFields,
                actions
            }
        };
        Border card = new()
        {
            MaximumWidthRequest = 420,
            Padding = 18,
            BackgroundColor = GetColor("QuickBookingPanelBackground", Color.FromArgb("#14264A")),
            Stroke = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = content
        };
        overlay.Children.Add(card);
        return overlay;
    }

    private Entry CreateTimeEntry(string propertyName)
    {
        Entry entry = new()
        {
            Keyboard = Keyboard.Numeric,
            MaxLength = 2,
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetColor("BrandButton", Color.FromArgb("#E7C45A")),
            BackgroundColor = Colors.Transparent
        };
        entry.SetBinding(Entry.TextProperty, propertyName);
        return entry;
    }

    private Border CreateEntryField(string title, Entry entry)
    {
        VerticalStackLayout field = new()
        {
            Spacing = 8,
            Children =
            {
                CreateLabel(title, 11, true),
                CreateInputBorder(entry, new Thickness(4))
            }
        };
        return new Border
        {
            BackgroundColor = Colors.Transparent,
            Stroke = Colors.Transparent,
            Content = field
        };
    }

    private static Button CreatePopupActionButton(string text, bool isPrimary) => new()
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

    private static Border CreateInputBorder(View content, Thickness padding) => new()
    {
        Padding = padding,
        BackgroundColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
        Stroke = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
        StrokeThickness = 0.8,
        StrokeShape = new RoundRectangle { CornerRadius = 13 },
        Content = content
    };

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (isClosing || isDatePickerOpen)
        {
            return;
        }

        await viewModel.SaveBlockCommand.ExecuteAsync(null);
        if (!viewModel.IsFormVisible)
        {
            await CloseOnceAsync(true);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (isClosing || isDatePickerOpen)
        {
            return;
        }

        viewModel.CancelFormCommand.Execute(null);
        await CloseOnceAsync(false);
    }

    private async Task CloseOnceAsync(bool result)
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        await CloseAsync(result);
    }

    private static Color GetColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color color
            ? color
            : fallback;
}
