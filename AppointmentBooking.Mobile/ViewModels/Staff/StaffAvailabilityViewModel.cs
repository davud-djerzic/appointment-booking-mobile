using AppointmentBooking.Mobile.Models.EmployeeUnavailability;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AppointmentBooking.Mobile.ViewModels.Staff;

public partial class StaffAvailabilityViewModel : ObservableObject
{
    private const string Endpoint = "api/employees/me/availability-blocks";

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

    private long? editingId;

    public ObservableCollection<EmployeeUnavailabilityResponse> Blocks { get; } = [];

    public bool HasBlocks => Blocks.Count > 0;

    public bool HasNoBlocks => !IsLoading && Blocks.Count == 0;

    public bool CanSave => !IsSaving;

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

    private static Color GetColor(string key)
    {
        return Application.Current?.Resources.TryGetValue(key, out object? value) == true &&
               value is Color color
            ? color
            : Colors.Black;
    }
}
