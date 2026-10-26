using AppointmentBooking.Mobile.Models.EmployeeUnavailability;
using AppointmentBooking.Mobile.Models.EmployeeWorkingHours;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.BookingCalendar;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using AppointmentBooking.Mobile.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AppointmentBooking.Mobile.ViewModels.Staff;

public partial class StaffAvailabilityViewModel : ObservableObject
{
    private static readonly CultureInfo BosnianCulture = CultureInfo.GetCultureInfo("bs-BA");
    private const string Endpoint = "api/employees/me/availability-blocks";
    private const string WorkingHoursEndpoint = "api/employees/me/working-hours";

    private readonly IApiClient apiClient;
    private readonly IErrorHandler errorHandler;
    private readonly IBookingCalendarService bookingCalendarService;

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

    public IReadOnlyList<EmployeeUnavailabilityResponse> UpcomingBreaks => GetBlocks(
        EmployeeUnavailabilityType.Break,
        includePast: false);

    public IReadOnlyList<EmployeeUnavailabilityResponse> PastBreaks => GetBlocks(
        EmployeeUnavailabilityType.Break,
        includePast: true);

    public IReadOnlyList<EmployeeUnavailabilityResponse> UpcomingVacations => GetBlocks(
        EmployeeUnavailabilityType.Vacation,
        includePast: false);

    public IReadOnlyList<EmployeeUnavailabilityResponse> PastVacations => GetBlocks(
        EmployeeUnavailabilityType.Vacation,
        includePast: true);

    public ObservableCollection<WorkingDayOption> WorkingDays { get; } =
    [
        new(WeekDay.Monday, "Ponedjeljak"),
        new(WeekDay.Tuesday, "Utorak"),
        new(WeekDay.Wednesday, "Srijeda"),
        new(WeekDay.Thursday, "Četvrtak"),
        new(WeekDay.Friday, "Petak")
    ];

    public ObservableCollection<RecurringBreakDayOption> RecurringBreakDays { get; } =
    [
        new(WeekDay.Monday, "Ponedjeljak", "Pon"),
        new(WeekDay.Tuesday, "Utorak", "Uto"),
        new(WeekDay.Wednesday, "Srijeda", "Sri"),
        new(WeekDay.Thursday, "Četvrtak", "Čet"),
        new(WeekDay.Friday, "Petak", "Pet")
    ];

    public ObservableCollection<WeeklyWorkingHoursDayDisplay> WeeklyBreaks { get; } = [];

    public bool HasNoUpcomingBreaks => !IsLoading && UpcomingBreaks.Count == 0;

    public bool HasNoPastBreaks => !IsLoading && PastBreaks.Count == 0;

    public bool HasNoUpcomingVacations => !IsLoading && UpcomingVacations.Count == 0;

    public bool HasNoPastVacations => !IsLoading && PastVacations.Count == 0;

    public bool CanSave => !IsSaving;

    public bool CanApplyRecurringBreak =>
        !IsApplyingRecurringBreak && RecurringBreakDays.Any(day => day.IsSelected);

    public string RecurringBreakDaySelectionDisplay =>
        $"Odabrano dana: {RecurringBreakDays.Count(day => day.IsSelected)}";

    public bool HasWeeklyBreaks => WeeklyBreaks.Count > 0;

    public string BreakStartsAtDisplay => FormatTime(BreakStartsAt);

    public string BreakEndsAtDisplay => FormatTime(BreakEndsAt);

    public string BreakDateDisplay => FormatDate(BreakDate);

    public string VacationStartsOnDisplay => FormatDate(VacationStartsOn);

    public string VacationEndsOnDisplay => FormatDate(VacationEndsOn);

    public string RecurringBreakStartsAtDisplay => FormatTime(RecurringBreakStartsAt);

    public string RecurringBreakEndsAtDisplay => FormatTime(RecurringBreakEndsAt);

    public DateTime Today => DateTime.Today;

    public DateTime Tomorrow => DateTime.Today.AddDays(1);

    /// <summary>Time selector shown on the staff panel page (recurring breaks).</summary>
    public bool IsPageTimeSelectorVisible =>
        IsTimeSelectorVisible &&
        timeSelectorTarget is TimeSelectorTarget.RecurringStart or TimeSelectorTarget.RecurringEnd;

    /// <summary>Time selector shown inside the break/vacation editor popup.</summary>
    public bool IsFormTimeSelectorVisible =>
        IsTimeSelectorVisible &&
        timeSelectorTarget is TimeSelectorTarget.DateBreakStart or TimeSelectorTarget.DateBreakEnd;

    public bool IsBreakSelected => SelectedType == EmployeeUnavailabilityType.Break;

    public bool IsVacationSelected => SelectedType == EmployeeUnavailabilityType.Vacation;

    public string FormTitle => IsEditing ? "Uredi odsustvo" : "Novo odsustvo";

    public Color BreakTypeBackground =>
        IsBreakSelected ? GetColor("BrandButton") : Colors.Transparent;

    public Color VacationTypeBackground =>
        IsVacationSelected ? GetColor("BrandButton") : Colors.Transparent;

    public Color BreakTypeTextColor =>
        IsBreakSelected ? GetColor("BrandBackground") : GetColor("TextOnBrandBackground");

    public Color VacationTypeTextColor =>
        IsVacationSelected ? GetColor("BrandBackground") : GetColor("TextOnBrandBackground");

    public StaffAvailabilityViewModel(
        IApiClient apiClient,
        IErrorHandler errorHandler,
        IBookingCalendarService bookingCalendarService)
    {
        this.apiClient = apiClient;
        this.errorHandler = errorHandler;
        this.bookingCalendarService = bookingCalendarService;
    }

    public void ResetCachedData()
    {
        Blocks.Clear();
        WeeklyBreaks.Clear();
        OnBlocksChanged();
        OnPropertyChanged(nameof(HasWeeklyBreaks));
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

    partial void OnIsTimeSelectorVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsPageTimeSelectorVisible));
        OnPropertyChanged(nameof(IsFormTimeSelectorVisible));
    }

    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(FormTitle));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoUpcomingBreaks));
        OnPropertyChanged(nameof(HasNoPastBreaks));
        OnPropertyChanged(nameof(HasNoUpcomingVacations));
        OnPropertyChanged(nameof(HasNoPastVacations));
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnIsApplyingRecurringBreakChanged(bool value)
    {
        OnPropertyChanged(nameof(CanApplyRecurringBreak));
    }

    [RelayCommand]
    private void ToggleRecurringBreakDay(RecurringBreakDayOption? day)
    {
        if (day is null)
        {
            return;
        }

        day.IsSelected = !day.IsSelected;
        OnPropertyChanged(nameof(CanApplyRecurringBreak));
        OnPropertyChanged(nameof(RecurringBreakDaySelectionDisplay));
    }

    partial void OnBreakStartsAtChanged(TimeSpan value) =>
        OnPropertyChanged(nameof(BreakStartsAtDisplay));

    partial void OnBreakEndsAtChanged(TimeSpan value) =>
        OnPropertyChanged(nameof(BreakEndsAtDisplay));

    partial void OnBreakDateChanged(DateTime value) =>
        OnPropertyChanged(nameof(BreakDateDisplay));

    partial void OnVacationStartsOnChanged(DateTime value)
    {
        if (VacationEndsOn.Date < value.Date)
        {
            VacationEndsOn = value.Date;
        }
        OnPropertyChanged(nameof(VacationStartsOnDisplay));
        OnPropertyChanged(nameof(VacationEndsOnDisplay));
    }

    partial void OnVacationEndsOnChanged(DateTime value) =>
        OnPropertyChanged(nameof(VacationEndsOnDisplay));

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
            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
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

            UpdateWeeklyBreaks(response ?? []);
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

        RecurringBreakDayOption[] selectedDays = RecurringBreakDays
            .Where(day => day.IsSelected)
            .ToArray();

        if (selectedDays.Length == 0)
        {
            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                "Odaberite dane",
                "Odaberite barem jedan dan u sedmici.",
                "U redu");
            return;
        }

        if (RecurringBreakEndsAt <= RecurringBreakStartsAt)
        {
            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                "Provjera unosa",
                "Vrijeme završetka pauze mora biti nakon vremena početka.",
                "U redu");
            return;
        }

        bool confirmed = await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
            "Stalna sedmična pauza",
            $"Pauza {RecurringBreakStartsAt:hh\\:mm}–{RecurringBreakEndsAt:hh\\:mm} " +
            $"ponavljat će se svake sedmice za: {string.Join(", ", selectedDays.Select(day => day.Label.ToLowerInvariant()))}. " +
            "Nastaviti?",
            "Primijeni",
            "Odustani");

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsApplyingRecurringBreak = true;

            IReadOnlyCollection<EmployeeWorkingHoursResponse>? latestSchedule = null;
            List<string> appliedDays = [];
            List<string> failedDays = [];

            foreach (RecurringBreakDayOption day in selectedDays)
            {
                try
                {
                    IReadOnlyCollection<EmployeeWorkingHoursResponse>? updated =
                        await apiClient.PutAsync<ApplyRecurringBreakRequest, IReadOnlyCollection<EmployeeWorkingHoursResponse>>(
                            $"{WorkingHoursEndpoint}/recurring-break",
                            new ApplyRecurringBreakRequest(
                                day.DayOfWeek,
                                TimeOnly.FromTimeSpan(RecurringBreakStartsAt),
                                TimeOnly.FromTimeSpan(RecurringBreakEndsAt)));

                    if (updated is null)
                    {
                        throw new InvalidOperationException("API nije vratio ažurirano radno vrijeme.");
                    }

                    latestSchedule = updated;
                    appliedDays.Add(day.Label);
                    day.IsSelected = false;
                }
                catch
                {
                    failedDays.Add(day.Label);
                }
            }

            if (latestSchedule is not null)
            {
                UpdateWeeklyBreaks(latestSchedule);
                if (latestSchedule.FirstOrDefault() is { } updatedHour)
                {
                    bookingCalendarService.Invalidate(updatedHour.EmployeeId);
                }
            }

            OnPropertyChanged(nameof(CanApplyRecurringBreak));
            OnPropertyChanged(nameof(RecurringBreakDaySelectionDisplay));

            string title = failedDays.Count == 0
                ? "Stalne pauze ažurirane"
                : appliedDays.Count == 0
                    ? "Pauze nisu promijenjene"
                    : "Pauze su djelimično ažurirane";
            string message = appliedDays.Count > 0
                ? $"Pauza {RecurringBreakStartsAt:hh\\:mm}–{RecurringBreakEndsAt:hh\\:mm} postavljena je za: {string.Join(", ", appliedDays)}."
                : string.Empty;

            if (failedDays.Count > 0)
            {
                message += (message.Length > 0 ? "\n\n" : string.Empty) +
                    $"Nije primijenjena za: {string.Join(", ", failedDays)}. Provjerite postojeće radno vrijeme i zakazane termine.";
            }

            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                title,
                message,
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

    private void UpdateWeeklyBreaks(IEnumerable<EmployeeWorkingHoursResponse> workingHours)
    {
        Dictionary<WeekDay, string> breaksByDay = workingHours
            .GroupBy(hour => hour.DayOfWeek)
            .Select(group => new
            {
                Day = group.Key,
                Intervals = group.OrderBy(hour => hour.StartsAt).ToArray()
            })
            .Where(day => day.Intervals.Length > 1)
            .ToDictionary(
                day => day.Day,
                day => string.Join("  ·  ", day.Intervals
                    .Zip(day.Intervals.Skip(1), (before, after) =>
                        $"{before.EndsAt:HH:mm}–{after.StartsAt:HH:mm}")));

        WeeklyBreaks.Clear();
        foreach (WorkingDayOption day in WorkingDays.Where(day => breaksByDay.ContainsKey(day.DayOfWeek)))
        {
            WeeklyBreaks.Add(new WeeklyWorkingHoursDayDisplay(day.Label, breaksByDay[day.DayOfWeek]));
        }

        if (WeeklyBreaks.Count == 0)
        {
            WeeklyBreaks.Add(new WeeklyWorkingHoursDayDisplay("Raspored", "Nema postavljenih stalnih pauza"));
        }

        OnPropertyChanged(nameof(HasWeeklyBreaks));
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
        IsTimeSelectorVisible = false;
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
            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                "Provjera unosa",
                validationMessage ?? "Unos nije ispravan.",
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

        bool confirmed = await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
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
            SalonTimeZone.Info.GetUtcOffset(localDateTime));
    }

    private void OnBlocksChanged()
    {
        OnPropertyChanged(nameof(UpcomingBreaks));
        OnPropertyChanged(nameof(PastBreaks));
        OnPropertyChanged(nameof(UpcomingVacations));
        OnPropertyChanged(nameof(PastVacations));
        OnPropertyChanged(nameof(HasNoUpcomingBreaks));
        OnPropertyChanged(nameof(HasNoPastBreaks));
        OnPropertyChanged(nameof(HasNoUpcomingVacations));
        OnPropertyChanged(nameof(HasNoPastVacations));
    }

    private IReadOnlyList<EmployeeUnavailabilityResponse> GetBlocks(
        EmployeeUnavailabilityType type,
        bool includePast)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        return Blocks
            .Where(block => block.Type == type && (block.EndsAt <= now) == includePast)
            .OrderBy(block => includePast ? block.EndsAt : block.StartsAt)
            .ToArray();
    }

    private static string FormatTime(TimeSpan time) =>
        time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static string FormatDate(DateTime date) =>
        date.ToString("dddd, d. MMMM yyyy.", BosnianCulture);

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
