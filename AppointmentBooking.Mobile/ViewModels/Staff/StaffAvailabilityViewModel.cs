using AppointmentBooking.Mobile.Models.EmployeeUnavailability;
using AppointmentBooking.Mobile.Models.EmployeeWorkingHours;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AppointmentBooking.Mobile.ViewModels.Staff;

public partial class StaffAvailabilityViewModel : ObservableObject
{
    private const string Endpoint = "api/employees/me/availability-blocks";
    private const string WorkingHoursEndpoint = "api/employees/me/working-hours";

    private readonly IApiClient apiClient;
    private readonly IErrorHandler errorHandler;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private bool isFormVisible;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private EmployeeUnavailabilityType selectedType = EmployeeUnavailabilityType.Break;

    [ObservableProperty]
    private DateTime breakDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan breakStartsAt = new(12, 0, 0);

    [ObservableProperty]
    private TimeSpan breakEndsAt = new(12, 30, 0);

    [ObservableProperty]
    private DateTime vacationStartsOn = DateTime.Today;

    [ObservableProperty]
    private DateTime vacationEndsOn = DateTime.Today;

    [ObservableProperty]
    private string? note;

    [ObservableProperty]
    private bool isWorkingHoursLoading;

    [ObservableProperty]
    private bool isApplyingRecurringBreak;

    [ObservableProperty]
    private WorkingDayOption? selectedWorkingDay;

    [ObservableProperty]
    private TimeSpan recurringBreakStartsAt = new(12, 0, 0);

    [ObservableProperty]
    private TimeSpan recurringBreakEndsAt = new(13, 0, 0);

    [ObservableProperty]
    private bool isTimeSelectorVisible;

    [ObservableProperty]
    private string timeSelectorHour = "12";

    [ObservableProperty]
    private string timeSelectorMinute = "00";

    [ObservableProperty]
    private string timeSelectorTitle = "Vrijeme početka pauze";

    private TimeSelectorTarget timeSelectorTarget;

    private long? editingId;

    public ObservableCollection<EmployeeUnavailabilityResponse> Blocks { get; } = [];

    public ObservableCollection<WorkingDayOption> WorkingDays { get; } =
    [
        new(WeekDay.Monday, "Ponedjeljak"),
        new(WeekDay.Tuesday, "Utorak"),
        new(WeekDay.Wednesday, "Srijeda"),
        new(WeekDay.Thursday, "Četvrtak"),
        new(WeekDay.Friday, "Petak")
    ];

    public ObservableCollection<WeeklyWorkingHoursDayDisplay> WeeklyWorkingHours { get; } = [];

    public bool HasBlocks => Blocks.Count > 0;

    public bool HasNoBlocks => !IsLoading && Blocks.Count == 0;

    public bool CanSave => !IsSaving;

    public bool CanApplyRecurringBreak => !IsApplyingRecurringBreak;

    public bool HasWorkingHours => WeeklyWorkingHours.Count > 0;

    public string BreakStartsAtDisplay => FormatTime(BreakStartsAt);

    public string BreakEndsAtDisplay => FormatTime(BreakEndsAt);

    public string RecurringBreakStartsAtDisplay => FormatTime(RecurringBreakStartsAt);

    public string RecurringBreakEndsAtDisplay => FormatTime(RecurringBreakEndsAt);

    public DateTime Today => DateTime.Today;

    public DateTime Tomorrow => DateTime.Today.AddDays(1);

    public bool IsBreakSelected => SelectedType == EmployeeUnavailabilityType.Break;

    public bool IsVacationSelected => SelectedType == EmployeeUnavailabilityType.Vacation;

    public string FormTitle => IsEditing ? "Uredi odsustvo" : "Novo odsustvo";

    public Color BreakTypeBackground =>
        IsBreakSelected ? GetColor("Primary") : GetColor("Surface");

    public Color VacationTypeBackground =>
        IsVacationSelected ? GetColor("Primary") : GetColor("Surface");

    public Color BreakTypeTextColor =>
        IsBreakSelected ? GetColor("TextOnPrimary") : GetColor("TextPrimary");

    public Color VacationTypeTextColor =>
        IsVacationSelected ? GetColor("TextOnPrimary") : GetColor("TextPrimary");

    public StaffAvailabilityViewModel(
        IApiClient apiClient,
        IErrorHandler errorHandler)
    {
        this.apiClient = apiClient;
        this.errorHandler = errorHandler;
        SelectedWorkingDay = WorkingDays[0];
    }

    partial void OnSelectedTypeChanged(EmployeeUnavailabilityType value)
    {
        OnPropertyChanged(nameof(IsBreakSelected));
        OnPropertyChanged(nameof(IsVacationSelected));
        OnPropertyChanged(nameof(BreakTypeBackground));
        OnPropertyChanged(nameof(VacationTypeBackground));
        OnPropertyChanged(nameof(BreakTypeTextColor));
        OnPropertyChanged(nameof(VacationTypeTextColor));
    }

    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(FormTitle));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoBlocks));
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnIsApplyingRecurringBreakChanged(bool value)
    {
        OnPropertyChanged(nameof(CanApplyRecurringBreak));
    }

    partial void OnBreakStartsAtChanged(TimeSpan value) =>
        OnPropertyChanged(nameof(BreakStartsAtDisplay));

    partial void OnBreakEndsAtChanged(TimeSpan value) =>
        OnPropertyChanged(nameof(BreakEndsAtDisplay));

    partial void OnRecurringBreakStartsAtChanged(TimeSpan value) =>
        OnPropertyChanged(nameof(RecurringBreakStartsAtDisplay));

    partial void OnRecurringBreakEndsAtChanged(TimeSpan value) =>
        OnPropertyChanged(nameof(RecurringBreakEndsAtDisplay));

    [RelayCommand]
    private void OpenRecurringBreakStartTimeSelector() =>
        OpenTimeSelector(RecurringBreakStartsAt, "Početak stalne pauze", TimeSelectorTarget.RecurringStart);

    [RelayCommand]
    private void OpenRecurringBreakEndTimeSelector() =>
        OpenTimeSelector(RecurringBreakEndsAt, "Kraj stalne pauze", TimeSelectorTarget.RecurringEnd);

    [RelayCommand]
    private void OpenBreakStartTimeSelector() =>
        OpenTimeSelector(BreakStartsAt, "Početak pauze", TimeSelectorTarget.DateBreakStart);

    [RelayCommand]
    private void OpenBreakEndTimeSelector() =>
        OpenTimeSelector(BreakEndsAt, "Kraj pauze", TimeSelectorTarget.DateBreakEnd);

    [RelayCommand]
    private async Task ConfirmTimeSelectorAsync()
    {
        if (!int.TryParse(TimeSelectorHour, NumberStyles.None, CultureInfo.InvariantCulture, out int hour) ||
            !int.TryParse(TimeSelectorMinute, NumberStyles.None, CultureInfo.InvariantCulture, out int minute) ||
            hour is < 0 or > 23 || minute is < 0 or > 59)
        {
            await Shell.Current.DisplayAlertAsync(
                "Neispravno vrijeme",
                "Unesite sat od 00 do 23 i minute od 00 do 59.",
                "U redu");
            return;
        }

        TimeSpan selectedTime = new(hour, minute, 0);

        switch (timeSelectorTarget)
        {
            case TimeSelectorTarget.RecurringStart:
                RecurringBreakStartsAt = selectedTime;
                break;
            case TimeSelectorTarget.RecurringEnd:
                RecurringBreakEndsAt = selectedTime;
                break;
            case TimeSelectorTarget.DateBreakStart:
                BreakStartsAt = selectedTime;
                break;
            case TimeSelectorTarget.DateBreakEnd:
                BreakEndsAt = selectedTime;
                break;
        }

        IsTimeSelectorVisible = false;
    }

    [RelayCommand]
    private void CloseTimeSelector() => IsTimeSelectorVisible = false;

    private void OpenTimeSelector(TimeSpan time, string title, TimeSelectorTarget target)
    {
        timeSelectorTarget = target;
        TimeSelectorTitle = title;
        TimeSelectorHour = time.Hours.ToString("00", CultureInfo.InvariantCulture);
        TimeSelectorMinute = time.Minutes.ToString("00", CultureInfo.InvariantCulture);
        IsTimeSelectorVisible = true;
    }

    [RelayCommand]
    private async Task LoadBlocksAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;

            IReadOnlyCollection<EmployeeUnavailabilityResponse>? response =
                await apiClient.GetAsync<IReadOnlyCollection<EmployeeUnavailabilityResponse>>(
                    Endpoint);

            Blocks.Clear();

            DateTimeOffset now = DateTimeOffset.Now;

            foreach (EmployeeUnavailabilityResponse block in
                     (response ?? []).OrderBy(x => x.EndsAt <= now)
                         .ThenBy(x => x.StartsAt))
            {
                Blocks.Add(block);
            }

            OnBlocksChanged();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadWorkingHoursAsync()
    {
        if (IsWorkingHoursLoading)
        {
            return;
        }

        try
        {
            IsWorkingHoursLoading = true;

            IReadOnlyCollection<EmployeeWorkingHoursResponse>? response =
                await apiClient.GetAsync<IReadOnlyCollection<EmployeeWorkingHoursResponse>>(
                    WorkingHoursEndpoint);

            Dictionary<WeekDay, EmployeeWorkingHoursResponse[]> hoursByDay =
                (response ?? [])
                .GroupBy(hour => hour.DayOfWeek)
                .ToDictionary(group => group.Key, group => group.OrderBy(hour => hour.StartsAt).ToArray());

            WeeklyWorkingHours.Clear();

            foreach (WorkingDayOption day in WorkingDays)
            {
                string hoursLabel = hoursByDay.TryGetValue(day.DayOfWeek, out EmployeeWorkingHoursResponse[]? intervals) &&
                                    intervals.Length > 0
                    ? string.Join("  ·  ", intervals.Select(interval =>
                        $"{interval.StartsAt:HH:mm}–{interval.EndsAt:HH:mm}"))
                    : "Neradni dan";

                WeeklyWorkingHours.Add(new WeeklyWorkingHoursDayDisplay(day.Label, hoursLabel));
            }

            OnPropertyChanged(nameof(HasWorkingHours));
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsWorkingHoursLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyRecurringBreakAsync()
    {
        if (IsApplyingRecurringBreak)
        {
            return;
        }

        if (SelectedWorkingDay is null)
        {
            await Shell.Current.DisplayAlertAsync("Provjera unosa", "Odaberite dan u sedmici.", "U redu");
            return;
        }

        if (RecurringBreakEndsAt <= RecurringBreakStartsAt)
        {
            await Shell.Current.DisplayAlertAsync(
                "Provjera unosa",
                "Vrijeme završetka pauze mora biti nakon vremena početka.",
                "U redu");
            return;
        }

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Stalna sedmična pauza",
            $"Pauza {SelectedWorkingDay.Label.ToLowerInvariant()}, " +
            $"{RecurringBreakStartsAt:hh\\:mm}–{RecurringBreakEndsAt:hh\\:mm}, " +
            "ponavljat će se svake sedmice i izmijeniti radno vrijeme tog dana. Nastaviti?",
            "Primijeni",
            "Odustani");

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsApplyingRecurringBreak = true;

            IReadOnlyCollection<EmployeeWorkingHoursResponse>? updated =
                await apiClient.PutAsync<ApplyRecurringBreakRequest, IReadOnlyCollection<EmployeeWorkingHoursResponse>>(
                    $"{WorkingHoursEndpoint}/recurring-break",
                    new ApplyRecurringBreakRequest(
                        SelectedWorkingDay.DayOfWeek,
                        TimeOnly.FromTimeSpan(RecurringBreakStartsAt),
                        TimeOnly.FromTimeSpan(RecurringBreakEndsAt)));

            if (updated is null)
            {
                throw new InvalidOperationException("API nije vratio ažurirano radno vrijeme.");
            }

            Dictionary<WeekDay, EmployeeWorkingHoursResponse[]> hoursByDay = updated
                .GroupBy(hour => hour.DayOfWeek)
                .ToDictionary(group => group.Key, group => group.OrderBy(hour => hour.StartsAt).ToArray());

            WeeklyWorkingHours.Clear();

            foreach (WorkingDayOption day in WorkingDays)
            {
                string hoursLabel = hoursByDay.TryGetValue(day.DayOfWeek, out EmployeeWorkingHoursResponse[]? intervals) &&
                                    intervals.Length > 0
                    ? string.Join("  ·  ", intervals.Select(interval =>
                        $"{interval.StartsAt:HH:mm}–{interval.EndsAt:HH:mm}"))
                    : "Neradni dan";

                WeeklyWorkingHours.Add(new WeeklyWorkingHoursDayDisplay(day.Label, hoursLabel));
            }

            OnPropertyChanged(nameof(HasWorkingHours));
            await Shell.Current.DisplayAlertAsync(
                "Radno vrijeme ažurirano",
                "Pauza je postavljena za svaku sedmicu. Raspored je odmah ažuriran.",
                "U redu");
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsApplyingRecurringBreak = false;
        }
    }

    [RelayCommand]
    private void BeginCreate()
    {
        editingId = null;
        IsEditing = false;
        SelectedType = EmployeeUnavailabilityType.Break;
        BreakDate = DateTime.Today;
        BreakStartsAt = new TimeSpan(12, 0, 0);
        BreakEndsAt = new TimeSpan(12, 30, 0);
        VacationStartsOn = Tomorrow;
        VacationEndsOn = Tomorrow;
        Note = string.Empty;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void SelectBreak()
    {
        SelectedType = EmployeeUnavailabilityType.Break;
    }

    [RelayCommand]
    private void SelectVacation()
    {
        SelectedType = EmployeeUnavailabilityType.Vacation;
    }

    [RelayCommand]
    private void CancelForm()
    {
        IsFormVisible = false;
        editingId = null;
        IsEditing = false;
    }

    [RelayCommand]
    private void EditBlock(EmployeeUnavailabilityResponse? block)
    {
        if (block is null || !block.CanEdit)
        {
            return;
        }

        editingId = block.Id;
        IsEditing = true;
        SelectedType = block.Type;
        Note = block.Note;

        DateTimeOffset localStartsAt = block.LocalStartsAt;
        DateTimeOffset localEndsAt = block.LocalEndsAt;

        if (block.Type == EmployeeUnavailabilityType.Break)
        {
            BreakDate = localStartsAt.Date;
            BreakStartsAt = localStartsAt.TimeOfDay;
            BreakEndsAt = localEndsAt.TimeOfDay;
        }
        else
        {
            VacationStartsOn = localStartsAt.Date;
            VacationEndsOn = localEndsAt.Date.AddDays(-1);
        }

        IsFormVisible = true;
    }

    [RelayCommand]
    private async Task SaveBlockAsync()
    {
        if (IsSaving)
        {
            return;
        }

        if (!TryBuildRequest(out SetEmployeeUnavailabilityRequest? request, out string? validationMessage))
        {
            await Shell.Current.DisplayAlertAsync(
                "Provjera unosa",
                validationMessage,
                "U redu");
            return;
        }

        try
        {
            IsSaving = true;

            string endpoint = editingId.HasValue
                ? $"{Endpoint}/{editingId.Value}"
                : Endpoint;

            EmployeeUnavailabilityResponse? saved = editingId.HasValue
                ? await apiClient.PutAsync<SetEmployeeUnavailabilityRequest, EmployeeUnavailabilityResponse>(
                    endpoint,
                    request!)
                : await apiClient.PostAsync<SetEmployeeUnavailabilityRequest, EmployeeUnavailabilityResponse>(
                    endpoint,
                    request!);

            if (saved is null)
            {
                throw new InvalidOperationException(
                    "API nije vratio sačuvano odsustvo.");
            }

            EmployeeUnavailabilityResponse? existing =
                Blocks.FirstOrDefault(x => x.Id == saved.Id);

            if (existing is not null)
            {
                Blocks.Remove(existing);
            }

            Blocks.Add(saved);
            SortBlocks();
            OnBlocksChanged();
            CancelForm();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task DeleteBlockAsync(EmployeeUnavailabilityResponse? block)
    {
        if (block is null)
        {
            return;
        }

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Obriši odsustvo",
            $"Želite li obrisati: {block.TypeLabel.ToLowerInvariant()}?",
            "Obriši",
            "Odustani");

        if (!confirmed)
        {
            return;
        }

        try
        {
            await apiClient.DeleteAsync($"{Endpoint}/{block.Id}");
            Blocks.Remove(block);
            OnBlocksChanged();

            if (editingId == block.Id)
            {
                CancelForm();
            }
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
    }

    private bool TryBuildRequest(
        out SetEmployeeUnavailabilityRequest? request,
        out string? validationMessage)
    {
        request = null;
        validationMessage = null;

        DateTimeOffset startsAt;
        DateTimeOffset endsAt;
        bool isAllDay = SelectedType == EmployeeUnavailabilityType.Vacation;

        if (SelectedType == EmployeeUnavailabilityType.Break)
        {
            if (BreakEndsAt <= BreakStartsAt)
            {
                validationMessage = "Vrijeme završetka pauze mora biti nakon vremena početka.";
                return false;
            }

            startsAt = CreateLocalDateTime(BreakDate, BreakStartsAt);
            endsAt = CreateLocalDateTime(BreakDate, BreakEndsAt);

            if (startsAt <= DateTimeOffset.Now)
            {
                validationMessage = "Pauza mora početi u budućnosti.";
                return false;
            }
        }
        else
        {
            if (VacationEndsOn.Date < VacationStartsOn.Date)
            {
                validationMessage = "Završni datum godišnjeg mora biti isti ili nakon početnog datuma.";
                return false;
            }

            startsAt = CreateLocalDateTime(VacationStartsOn, TimeSpan.Zero);
            endsAt = CreateLocalDateTime(VacationEndsOn.Date.AddDays(1), TimeSpan.Zero);

            if (startsAt <= DateTimeOffset.Now)
            {
                validationMessage = "Godišnji odmor mora početi od narednog dana.";
                return false;
            }
        }

        request = new SetEmployeeUnavailabilityRequest(
            SelectedType,
            startsAt,
            endsAt,
            isAllDay,
            string.IsNullOrWhiteSpace(Note) ? null : Note.Trim());

        return true;
    }

    private static DateTimeOffset CreateLocalDateTime(DateTime date, TimeSpan time)
    {
        DateTime localDateTime = DateTime.SpecifyKind(
            date.Date.Add(time),
            DateTimeKind.Unspecified);

        return new DateTimeOffset(
            localDateTime,
            TimeZoneInfo.Local.GetUtcOffset(localDateTime));
    }

    private void SortBlocks()
    {
        DateTimeOffset now = DateTimeOffset.Now;

        EmployeeUnavailabilityResponse[] sorted = Blocks
            .OrderBy(x => x.EndsAt <= now)
            .ThenBy(x => x.StartsAt)
            .ToArray();

        Blocks.Clear();

        foreach (EmployeeUnavailabilityResponse block in sorted)
        {
            Blocks.Add(block);
        }
    }

    private void OnBlocksChanged()
    {
        OnPropertyChanged(nameof(HasBlocks));
        OnPropertyChanged(nameof(HasNoBlocks));
    }

    private static string FormatTime(TimeSpan time) =>
        time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private enum TimeSelectorTarget
    {
        RecurringStart,
        RecurringEnd,
        DateBreakStart,
        DateBreakEnd
    }

    private static Color GetColor(string key)
    {
        return Application.Current?.Resources.TryGetValue(key, out object? value) == true &&
               value is Color color
            ? color
            : Colors.Black;
    }
}
