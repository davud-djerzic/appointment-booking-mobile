using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using System.Globalization;

namespace AppointmentBooking.Mobile.Views.Shared;

/// <summary>
/// Date picker popup with a month calendar.
/// The weekday header and day grid are rendered by a single <see cref="GraphicsView"/>
/// instead of one native button per day, so opening the popup and switching months
/// does not create or destroy native views.
/// </summary>
public sealed class CalendarDatePickerPopup : Popup<DateTime?>
{
    private const float TapSlop = 12f;

    private static readonly CultureInfo BosnianCulture = CultureInfo.GetCultureInfo("bs-BA");
    private static readonly string[] WeekdayLabels = ["Pon", "Uto", "Sri", "Čet", "Pet", "Sub", "Ned"];

    private readonly DateTime minimumDate;
    private readonly Label monthLabel;
    private readonly GraphicsView calendarView;
    private readonly CalendarMonthDrawable calendarDrawable;
    private DateTime displayedMonth;
    private DateTime selectedDate;
    private PointF? interactionStart;
    private bool isClosing;

    public CalendarDatePickerPopup(DateTime selectedDate, DateTime minimumDate, string title)
    {
        this.minimumDate = minimumDate.Date;
        this.selectedDate = selectedDate.Date < this.minimumDate
            ? this.minimumDate
            : selectedDate.Date;
        displayedMonth = new DateTime(this.selectedDate.Year, this.selectedDate.Month, 1);

        CanBeDismissedByTappingOutsideOfPopup = false;
        BackgroundColor = Colors.Transparent;
        Padding = 0;

        monthLabel = new Label
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetColor("TextOnBrandBackground", Colors.White),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        calendarDrawable = new CalendarMonthDrawable(new CalendarPalette(
            Background: GetColor("BrandBackground", Color.FromArgb("#07152F")),
            Text: GetColor("TextOnBrandBackground", Colors.White),
            Accent: GetColor("BrandButton", Color.FromArgb("#E7C45A")),
            Border: GetColor("PanelBorder", Color.FromArgb("#2B3D5C"))));

        calendarView = new GraphicsView
        {
            Drawable = calendarDrawable,
            HeightRequest = CalendarMonthDrawable.TotalHeight,
            HorizontalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Transparent
        };
        calendarView.StartInteraction += OnCalendarStartInteraction;
        calendarView.EndInteraction += OnCalendarEndInteraction;
        calendarView.CancelInteraction += OnCalendarCancelInteraction;

        Content = new Border
        {
            WidthRequest = 340,
            MaximumWidthRequest = 420,
            Padding = 18,
            BackgroundColor = GetColor("QuickBookingPanelBackground", Color.FromArgb("#14264A")),
            Stroke = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Content = CreateContent(title)
        };

        RefreshCalendar();
    }

    private View CreateContent(string title)
    {
        Button previousMonthButton = CreateMonthButton("‹");
        previousMonthButton.Clicked += OnPreviousMonthClicked;
        Button nextMonthButton = CreateMonthButton("›");
        nextMonthButton.Clicked += OnNextMonthClicked;

        Grid monthNavigation = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Star),
                new(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        monthNavigation.Children.Add(previousMonthButton);
        monthNavigation.Children.Add(monthLabel);
        monthNavigation.Children.Add(nextMonthButton);
        Grid.SetColumn(monthLabel, 1);
        Grid.SetColumn(nextMonthButton, 2);

        Button cancelButton = CreateActionButton("Odustani", isPrimary: false);
        cancelButton.Clicked += OnCancelClicked;
        Button confirmButton = CreateActionButton("Odaberi", isPrimary: true);
        confirmButton.Clicked += OnConfirmClicked;
        Grid actions = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star),
                new(GridLength.Star)
            },
            ColumnSpacing = 10,
            Children = { cancelButton, confirmButton }
        };
        Grid.SetColumn(confirmButton, 1);

        return new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = title,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = GetColor("TextOnBrandBackground", Colors.White)
                },
                monthNavigation,
                calendarView,
                actions
            }
        };
    }

    private void RefreshCalendar()
    {
        monthLabel.Text = BosnianCulture.TextInfo.ToTitleCase(
            displayedMonth.ToString("MMMM yyyy", BosnianCulture));

        calendarDrawable.Update(displayedMonth, selectedDate, minimumDate);
        SemanticProperties.SetDescription(
            calendarView,
            $"Kalendar, odabrani datum {selectedDate.ToString("d. MMMM yyyy.", BosnianCulture)}");
        calendarView.Invalidate();
    }

    private void OnCalendarStartInteraction(object? sender, TouchEventArgs e) =>
        interactionStart = e.Touches.Length > 0 ? e.Touches[0] : null;

    private void OnCalendarCancelInteraction(object? sender, EventArgs e) =>
        interactionStart = null;

    private void OnCalendarEndInteraction(object? sender, TouchEventArgs e)
    {
        PointF? start = interactionStart;
        interactionStart = null;

        if (start is not PointF startPoint || !e.IsInsideBounds || e.Touches.Length == 0)
        {
            return;
        }

        PointF endPoint = e.Touches[0];
        if (Math.Abs(endPoint.X - startPoint.X) > TapSlop ||
            Math.Abs(endPoint.Y - startPoint.Y) > TapSlop)
        {
            return;
        }

        DateTime? tappedDate = calendarDrawable.HitTest(endPoint, (float)calendarView.Width);
        if (tappedDate is not DateTime date || date < minimumDate || date == selectedDate)
        {
            return;
        }

        selectedDate = date;
        RefreshCalendar();
    }

    private void OnPreviousMonthClicked(object? sender, EventArgs e)
    {
        DateTime previousMonth = displayedMonth.AddMonths(-1);
        if (previousMonth.Year < minimumDate.Year ||
            (previousMonth.Year == minimumDate.Year && previousMonth.Month < minimumDate.Month))
        {
            return;
        }

        displayedMonth = previousMonth;
        RefreshCalendar();
    }

    private void OnNextMonthClicked(object? sender, EventArgs e)
    {
        displayedMonth = displayedMonth.AddMonths(1);
        RefreshCalendar();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        await CloseAsync(null);
    }

    private async void OnConfirmClicked(object? sender, EventArgs e)
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        await CloseAsync(selectedDate);
    }

    private static Button CreateMonthButton(string text) => new()
    {
        Text = text,
        WidthRequest = 42,
        HeightRequest = 40,
        Padding = 0,
        FontSize = 24,
        FontAttributes = FontAttributes.Bold,
        CornerRadius = 12,
        BackgroundColor = GetColor("BrandBackground", Color.FromArgb("#07152F")),
        TextColor = GetColor("BrandButton", Color.FromArgb("#E7C45A")),
        BorderColor = GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
        BorderWidth = 1
    };

    private static Button CreateActionButton(string text, bool isPrimary) => new()
    {
        Text = text,
        HeightRequest = 48,
        CornerRadius = 14,
        FontAttributes = FontAttributes.Bold,
        BackgroundColor = isPrimary
            ? GetColor("BrandButton", Color.FromArgb("#E7C45A"))
            : GetColor("BrandBackground", Color.FromArgb("#07152F")),
        TextColor = isPrimary
            ? GetColor("BrandBackground", Color.FromArgb("#07152F"))
            : GetColor("TextOnBrandBackground", Colors.White),
        BorderColor = isPrimary
            ? GetColor("BrandButton", Color.FromArgb("#E7C45A"))
            : GetColor("PanelBorder", Color.FromArgb("#2B3D5C")),
        BorderWidth = isPrimary ? 0 : 1
    };

    private static Color GetColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out object? value) == true &&
        value is Color color
            ? color
            : fallback;

    private sealed record CalendarPalette(Color Background, Color Text, Color Accent, Color Border);

    private sealed class CalendarMonthDrawable(CalendarPalette palette) : IDrawable
    {
        private const int Columns = 7;
        private const int Rows = 6;
        private const float CellHeight = 38f;
        private const float CellSpacing = 4f;
        private const float HeaderHeight = 22f;
        private const float CornerRadius = 10f;
        private const float DisabledAlpha = 0.32f;
        private const float WeekdayAlpha = 0.65f;

        // Same alias as AppFontFamilyBold in Resources/Styles/Styles.xaml (calendar text is bold).
        private static readonly Microsoft.Maui.Graphics.Font AppFont = new("PlayfairDisplayBold");

        public const float TotalHeight =
            HeaderHeight + CellSpacing + (Rows * CellHeight) + ((Rows - 1) * CellSpacing);

        private DateTime displayedMonth;
        private DateTime selectedDate;
        private DateTime minimumDate;

        public void Update(DateTime month, DateTime selected, DateTime minimum)
        {
            displayedMonth = new DateTime(month.Year, month.Month, 1);
            selectedDate = selected.Date;
            minimumDate = minimum.Date;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float cellWidth = GetCellWidth(dirtyRect.Width);
            if (cellWidth <= 0)
            {
                return;
            }

            canvas.Font = AppFont;

            canvas.FontSize = 10;
            canvas.FontColor = palette.Text;
            canvas.Alpha = WeekdayAlpha;
            for (int column = 0; column < Columns; column++)
            {
                canvas.DrawString(
                    WeekdayLabels[column],
                    column * (cellWidth + CellSpacing),
                    0,
                    cellWidth,
                    HeaderHeight,
                    Microsoft.Maui.Graphics.HorizontalAlignment.Center,
                    Microsoft.Maui.Graphics.VerticalAlignment.Center);
            }

            canvas.FontSize = 13;
            canvas.StrokeSize = 1;

            int firstDayOffset = GetFirstDayOffset();
            int daysInMonth = DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                int cellIndex = firstDayOffset + day - 1;
                float x = (cellIndex % Columns) * (cellWidth + CellSpacing);
                float y = HeaderHeight + CellSpacing + ((cellIndex / Columns) * (CellHeight + CellSpacing));

                DateTime date = new(displayedMonth.Year, displayedMonth.Month, day);
                bool isSelected = date == selectedDate;

                canvas.Alpha = date >= minimumDate ? 1f : DisabledAlpha;

                canvas.FillColor = isSelected ? palette.Accent : palette.Background;
                canvas.FillRoundedRectangle(x, y, cellWidth, CellHeight, CornerRadius);

                canvas.StrokeColor = isSelected ? palette.Accent : palette.Border;
                canvas.DrawRoundedRectangle(x + 0.5f, y + 0.5f, cellWidth - 1f, CellHeight - 1f, CornerRadius);

                canvas.FontColor = isSelected ? palette.Background : palette.Text;
                canvas.DrawString(
                    day.ToString(BosnianCulture),
                    x,
                    y,
                    cellWidth,
                    CellHeight,
                    Microsoft.Maui.Graphics.HorizontalAlignment.Center,
                    Microsoft.Maui.Graphics.VerticalAlignment.Center);
            }

            canvas.Alpha = 1f;
        }

        public DateTime? HitTest(PointF point, float width)
        {
            float cellWidth = GetCellWidth(width);
            float gridTop = HeaderHeight + CellSpacing;
            if (cellWidth <= 0 || point.X < 0 || point.Y < gridTop)
            {
                return null;
            }

            int column = (int)(point.X / (cellWidth + CellSpacing));
            int row = (int)((point.Y - gridTop) / (CellHeight + CellSpacing));
            if (column is < 0 or >= Columns || row is < 0 or >= Rows)
            {
                return null;
            }

            int day = (row * Columns) + column - GetFirstDayOffset() + 1;
            int daysInMonth = DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month);
            if (day < 1 || day > daysInMonth)
            {
                return null;
            }

            return new DateTime(displayedMonth.Year, displayedMonth.Month, day);
        }

        private int GetFirstDayOffset() => ((int)displayedMonth.DayOfWeek + 6) % 7;

        private static float GetCellWidth(float width) =>
            (width - ((Columns - 1) * CellSpacing)) / Columns;
    }
}
