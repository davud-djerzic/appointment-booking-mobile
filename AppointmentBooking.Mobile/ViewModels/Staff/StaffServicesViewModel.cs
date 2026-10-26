using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using AppointmentBooking.Mobile.Models.BookableServices;
using AppointmentBooking.Mobile.Models.BookableServices.Requests;
using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using AppointmentBooking.Mobile.Models.Common;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.BookingCatalog;
using AppointmentBooking.Mobile.Services.Dialogs;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppointmentBooking.Mobile.ViewModels.Staff;

/// <summary>
/// Staff "Usluge" tab: list active services, create, edit and delete them.
/// </summary>
public partial class StaffServicesViewModel : ObservableObject
{
    private const string Endpoint = "api/BookableServices";
    private const int PageSize = 100;
    private const int MaxPages = 50;

    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int DurationMin = 1;
    public const int DurationMax = 600;
    public const decimal PriceMax = 99_999_999.99m;

    private static readonly CultureInfo BosnianCulture = CultureInfo.GetCultureInfo("bs-BA");
    private static readonly StringComparer NameComparer = StringComparer.Create(BosnianCulture, ignoreCase: true);

    private readonly IApiClient apiClient;
    private readonly IErrorHandler errorHandler;
    private readonly IBookingCatalogService bookingCatalogService;
    private readonly List<StaffServiceItem> allServices = [];

    private bool hasLoaded;
    private long? editingId;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private string formName = string.Empty;

    [ObservableProperty]
    private string formDescription = string.Empty;

    [ObservableProperty]
    private string formDuration = string.Empty;

    [ObservableProperty]
    private string formPrice = string.Empty;

    [ObservableProperty]
    private string? formError;

    public StaffServicesViewModel(
        IApiClient apiClient,
        IErrorHandler errorHandler,
        IBookingCatalogService bookingCatalogService)
    {
        this.apiClient = apiClient;
        this.errorHandler = errorHandler;
        this.bookingCatalogService = bookingCatalogService;
    }

    public ObservableCollection<StaffServiceItem> VisibleServices { get; } = [];

    public string SummaryText => hasLoaded
        ? FormatServiceCount(VisibleServices.Count)
        : "Upravljanje uslugama salona";

    public bool HasNoVisibleServices => !IsLoading && VisibleServices.Count == 0;

    public string FormTitle => IsEditing ? "Uredi uslugu" : "Nova usluga";

    public bool HasFormError => !string.IsNullOrWhiteSpace(FormError);

    public bool CanSave => !IsSaving;

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(HasNoVisibleServices));

    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(FormTitle));

    partial void OnFormErrorChanged(string? value) => OnPropertyChanged(nameof(HasFormError));

    partial void OnIsSavingChanged(bool value) => OnPropertyChanged(nameof(CanSave));

    public Task PreloadAsync() => hasLoaded ? Task.CompletedTask : LoadAsync();

    public void ResetCachedData()
    {
        hasLoaded = false;
        allServices.Clear();
        ApplyFilter();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            await ReloadFromServerAsync();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsLoading = false;
            ApplyFilter();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            await ReloadFromServerAsync();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsRefreshing = false;
            ApplyFilter();
        }
    }

    public void BeginCreate()
    {
        editingId = null;
        IsEditing = false;
        FormName = string.Empty;
        FormDescription = string.Empty;
        FormDuration = "30";
        FormPrice = string.Empty;
        FormError = null;
    }

    public void BeginEdit(StaffServiceItem item)
    {
        editingId = item.Id;
        IsEditing = true;
        FormName = item.Name;
        FormDescription = item.Description ?? string.Empty;
        FormDuration = item.DurationMinutes.ToString(CultureInfo.InvariantCulture);
        FormPrice = item.Price.ToString("0.00", BosnianCulture);
        FormError = null;
    }

    /// <summary>Validates and saves the form. Returns true when the popup can close.</summary>
    public async Task<bool> SaveAsync()
    {
        if (IsSaving)
        {
            return false;
        }

        if (!TryBuildRequest(out SaveServiceRequest? request, out string? validationMessage))
        {
            FormError = validationMessage;
            return false;
        }

        FormError = null;

        try
        {
            IsSaving = true;

            ServiceResponse? saved = editingId is long id
                ? await apiClient.PutAsync<SaveServiceRequest, ServiceResponse>($"{Endpoint}/{id}", request!)
                : await apiClient.PostAsync<SaveServiceRequest, ServiceResponse>(Endpoint, request!);

            if (saved is null)
            {
                await ReloadFromServerAsync();
            }
            else
            {
                Upsert(saved);
            }

            bookingCatalogService.InvalidateServices();
            ApplyFilter();
            editingId = null;
            IsEditing = false;
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            FormError =
                $"Usluga s nazivom „{request!.Name}“ već postoji. Izaberite drugi naziv.";
            return false;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            FormError = "Podaci nisu ispravni. Provjerite naziv, opis, trajanje i cijenu.";
            return false;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            await AlertPanelService.DisplayAlertAsync(
                "Usluga nije pronađena",
                "Ova usluga više ne postoji. Lista je osvježena.",
                "U redu");
            await TryReloadAsync();
            return true;
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public async Task DeleteAsync(StaffServiceItem item)
    {
        if (IsBusy)
        {
            return;
        }

        bool confirmed = await AlertPanelService.DisplayAlertAsync(
            "Obriši uslugu",
            $"Usluga „{item.Name}“ više neće biti dostupna za rezervaciju i bit će uklonjena sa svih uposlenika. " +
            "Postojeći termini ostaju.",
            "Obriši",
            "Odustani");

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await apiClient.DeleteAsync($"{Endpoint}/{item.Id}");
            bookingCatalogService.InvalidateServices();
            allServices.RemoveAll(service => service.Id == item.Id);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            await AlertPanelService.DisplayAlertAsync(
                "Usluga nije pronađena",
                "Ova usluga više ne postoji. Lista je osvježena.",
                "U redu");
            await TryReloadAsync();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            IsBusy = false;
            ApplyFilter();
        }
    }

    public void CancelForm()
    {
        editingId = null;
        IsEditing = false;
        FormError = null;
    }

    private bool TryBuildRequest(out SaveServiceRequest? request, out string? validationMessage)
    {
        request = null;
        validationMessage = null;

        string name = (FormName ?? string.Empty).Trim();
        if (name.Length < NameMinLength || name.Length > NameMaxLength)
        {
            validationMessage = $"Naziv mora imati između {NameMinLength} i {NameMaxLength} znakova.";
            return false;
        }

        string description = (FormDescription ?? string.Empty).Trim();
        if (description.Length > DescriptionMaxLength)
        {
            validationMessage = $"Opis može imati najviše {DescriptionMaxLength} znakova.";
            return false;
        }

        if (!int.TryParse((FormDuration ?? string.Empty).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int duration) ||
            duration < DurationMin || duration > DurationMax)
        {
            validationMessage = $"Trajanje mora biti cijeli broj minuta od {DurationMin} do {DurationMax}.";
            return false;
        }

        if (!TryParsePrice(FormPrice, out decimal price))
        {
            validationMessage = "Unesite ispravnu cijenu, npr. 25 ili 25,50.";
            return false;
        }

        if (price < 0 || price > PriceMax)
        {
            validationMessage = "Cijena mora biti između 0 i 99.999.999,99 KM.";
            return false;
        }

        request = new SaveServiceRequest(
            name,
            description.Length == 0 ? null : description,
            duration,
            price);
        return true;
    }

    /// <summary>Accepts both "25,50" and "25.50"; at most two decimals; no thousands separators.</summary>
    private static bool TryParsePrice(string? input, out decimal price)
    {
        price = 0;
        string value = (input ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("KM", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (value.Length == 0)
        {
            return false;
        }

        value = value.Replace(',', '.');
        if (value.Count(character => character == '.') > 1)
        {
            return false;
        }

        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price))
        {
            return false;
        }

        int separatorIndex = value.IndexOf('.');
        return separatorIndex < 0 || value.Length - separatorIndex - 1 <= 2;
    }

    private async Task TryReloadAsync()
    {
        try
        {
            await ReloadFromServerAsync();
        }
        catch (Exception ex)
        {
            await errorHandler.HandleAsync(ex);
        }
        finally
        {
            ApplyFilter();
        }
    }

    private async Task ReloadFromServerAsync()
    {
        List<ServiceResponse> loaded = [];
        int page = 1;
        int totalPages = 1;

        do
        {
            PagedResponse<ServiceResponse>? response =
                await apiClient.GetAsync<PagedResponse<ServiceResponse>>(
                    $"{Endpoint}?isActive=true&page={page}&pageSize={PageSize}");

            if (response is null)
            {
                break;
            }

            loaded.AddRange(response.Items);
            totalPages = response.TotalPages;
            page++;
        }
        while (page <= totalPages && page <= MaxPages);

        allServices.Clear();
        allServices.AddRange(loaded.Select(service => new StaffServiceItem(service)));
        hasLoaded = true;
    }

    private void Upsert(ServiceResponse service)
    {
        int index = allServices.FindIndex(item => item.Id == service.Id);
        StaffServiceItem item = new(service);

        if (index >= 0)
        {
            allServices[index] = item;
        }
        else
        {
            allServices.Add(item);
        }
    }

    private void ApplyFilter()
    {
        List<StaffServiceItem> filtered = allServices
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name, NameComparer)
            .ToList();

        VisibleServices.Clear();
        foreach (StaffServiceItem item in filtered)
        {
            VisibleServices.Add(item);
        }

        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(HasNoVisibleServices));
    }

    private static string FormatServiceCount(int count)
    {
        int lastTwo = count % 100;
        int last = count % 10;
        string word = last == 1 && lastTwo != 11
            ? "usluga"
            : last is >= 2 and <= 4 && lastTwo is < 12 or > 14
                ? "usluge"
                : "usluga";
        return $"{count} {word}";
    }
}
