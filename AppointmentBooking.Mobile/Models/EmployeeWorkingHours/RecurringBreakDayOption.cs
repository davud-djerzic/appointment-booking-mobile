using CommunityToolkit.Mvvm.ComponentModel;

namespace AppointmentBooking.Mobile.Models.EmployeeWorkingHours;

public partial class RecurringBreakDayOption(WeekDay dayOfWeek, string label, string shortLabel) : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    public WeekDay DayOfWeek { get; } = dayOfWeek;

    public int ColumnIndex => (int)DayOfWeek - 1;

    public string Label { get; } = label;

    public string ShortLabel { get; } = shortLabel;

    public string DisplayLabel => IsSelected ? $"✓ {ShortLabel}" : ShortLabel;

    public Color BackgroundColor => GetColor(IsSelected ? "BrandButton" : "BrandBackground");

    public Color TextColor => GetColor(IsSelected ? "BrandBackground" : "TextOnBrandBackground");

    public Color BorderColor => GetColor(IsSelected ? "BrandButton" : "PanelBorder");

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayLabel));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(BorderColor));
    }

    private static Color GetColor(string key) =>
        Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color color
            ? color
            : Colors.White;
}
