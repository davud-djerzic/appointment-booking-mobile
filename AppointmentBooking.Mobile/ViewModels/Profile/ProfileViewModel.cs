using AppointmentBooking.Mobile.Models.Profile.Requests;
using AppointmentBooking.Mobile.Models.Profile.Responses;
using AppointmentBooking.Mobile.Views.Shared;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using AppointmentBooking.Mobile.Models.Salons.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using AppointmentBooking.Mobile.Services.ProfileData;
using AppointmentBooking.Mobile.Services.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppointmentBooking.Mobile.ViewModels.Profile
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IApiClient apiClient;
        private readonly IErrorHandler errorHandler;
        private readonly ISessionService sessionService;
        private readonly ProfileDataCache profileDataCache;

        private string originalFirstName = string.Empty;
        private string originalLastName = string.Empty;
        private string originalEmail = string.Empty;
        private string originalPhone = string.Empty;

        [ObservableProperty]
        private string firstName = string.Empty;

        [ObservableProperty]
        private bool isCustomerInfoExpanded;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string phone = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isSaving;

        [ObservableProperty]
        private bool isDeletingAccount;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private SalonResponse? salon;

        [ObservableProperty]
        private bool isLoadingSalon;

        public bool CanEdit =>
            !IsLoading &&
            !IsSaving &&
            !IsEditing;

        public bool CanSave =>
            !IsLoading &&
            !IsSaving &&
            IsEditing;

        public bool CanDeleteAccount => !IsDeletingAccount;

        public ProfileViewModel(
            IApiClient apiClient,
            IErrorHandler errorHandler,
            ISessionService sessionService,
            ProfileDataCache profileDataCache)
        {
            this.apiClient = apiClient;
            this.errorHandler = errorHandler;
            this.sessionService = sessionService;
            this.profileDataCache = profileDataCache;
        }

        [RelayCommand]
        private async Task LoadProfileAsync()
        {
            try
            {
                IsLoading = true;
                await profileDataCache.EnsureProfileLoadedAsync();
                MyProfileResponse? response = profileDataCache.Profile;

                if (response is null)
                {
                    return;
                }

                FirstName = response.FirstName;
                LastName = response.LastName;
                Email = response.Email;
                Phone = response.Phone;

                originalFirstName = response.FirstName;
                originalLastName = response.LastName;
                originalEmail = response.Email;
                originalPhone = response.Phone;

                IsEditing = false;

                IsLoadingSalon = true;
                try
                {
                    await profileDataCache.EnsureSalonLoadedAsync();
                    Salon = profileDataCache.Salon;
                }
                catch (Exception salonException)
                {
                    Salon = null;
                    await errorHandler.HandleAsync(salonException);
                }
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsLoading = false;
                IsLoadingSalon = false;

                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanSave));

                StartEditingCommand.NotifyCanExecuteChanged();
                SaveChangesCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private void ToggleCustomerInfo()
        {
            IsCustomerInfoExpanded = !IsCustomerInfoExpanded;
        }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void StartEditing()
        {
            IsEditing = true;

            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanSave));

            StartEditingCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveChangesAsync()
        {
            string firstName = FirstName.Trim();
            string lastName = LastName.Trim();
            string email = Email.Trim();
            string phone = Phone.Trim();

            UpdateMyProfileRequest request = new()
            {
                FirstName =
                    string.Equals(
                        firstName,
                        originalFirstName,
                        StringComparison.Ordinal)
                        ? null
                        : firstName,

                LastName =
                    string.Equals(
                        lastName,
                        originalLastName,
                        StringComparison.Ordinal)
                        ? null
                        : lastName,

                Email =
                    string.Equals(
                        email,
                        originalEmail,
                        StringComparison.OrdinalIgnoreCase)
                        ? null
                        : email,

                Phone =
                    string.Equals(
                        phone,
                        originalPhone,
                        StringComparison.Ordinal)
                        ? null
                        : phone
            };

            bool hasChanges =
                request.FirstName is not null ||
                request.LastName is not null ||
                request.Email is not null ||
                request.Phone is not null;

            if (!hasChanges)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Nema izmjena",
                    "Niste promijenili nijedan podatak.",
                    "U redu");

                return;
            }

            try
            {
                IsSaving = true;

                MyProfileResponse? response =
                    await apiClient.PatchAsync<
                        UpdateMyProfileRequest,
                        MyProfileResponse>(
                        "api/profile/me",
                        request);

                if (response is null)
                {
                    return;
                }

                FirstName = response.FirstName;
                LastName = response.LastName;
                Email = response.Email;
                Phone = response.Phone;

                originalFirstName = response.FirstName;
                originalLastName = response.LastName;
                originalEmail = response.Email;
                originalPhone = response.Phone;

                profileDataCache.UpdateProfile(response);

                IsEditing = false;

                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Profil ažuriran",
                    "Vaši podaci su uspješno ažurirani.",
                    "U redu");
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsSaving = false;

                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanSave));

                StartEditingCommand.NotifyCanExecuteChanged();
                SaveChangesCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private async Task OpenInstagramAsync()
        {
            string? instagramUrl = Salon?.InstagramUrl?.Trim();
            if (string.IsNullOrWhiteSpace(instagramUrl))
            {
                return;
            }

            if (instagramUrl.StartsWith('@'))
            {
                instagramUrl = $"https://www.instagram.com/{Uri.EscapeDataString(instagramUrl[1..])}";
            }
            else if (!instagramUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                     !instagramUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                instagramUrl = $"https://{instagramUrl.TrimStart('/')}";
            }

            if (!Uri.TryCreate(instagramUrl, UriKind.Absolute, out Uri? instagramUri) ||
                (instagramUri.Scheme != Uri.UriSchemeHttps && instagramUri.Scheme != Uri.UriSchemeHttp) ||
                !(instagramUri.Host.Equals("instagram.com", StringComparison.OrdinalIgnoreCase) ||
                  instagramUri.Host.EndsWith(".instagram.com", StringComparison.OrdinalIgnoreCase)))
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Neispravan Instagram link",
                    "Provjerite Instagram link salona i pokušajte ponovo.",
                    "U redu");
                return;
            }

            try
            {
                await Launcher.Default.OpenAsync(instagramUri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Instagram link nije moguće otvoriti: {ex}");
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Instagram nije dostupan",
                    "Nije moguće otvoriti Instagram na ovom uređaju. Pokušajte ponovo ili provjerite da li je instaliran browser.",
                    "U redu");
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            bool confirmed =
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Odjava",
                    "Da li ste sigurni da se želite odjaviti?",
                    "Odjavi se",
                    "Odustani");

            if (!confirmed)
            {
                return;
            }

            await sessionService.SignOutAsync();
        }

        [RelayCommand]
        private async Task DeleteAccountAsync()
        {
            Page? page = Shell.Current?.CurrentPage;
            if (page is null)
            {
                return;
            }

            var passwordResult = await page.ShowPopupAsync<string?>(new DeleteAccountPopup(), new PopupOptions
            {
                PageOverlayColor = Color.FromArgb("#99000000"),
                Shape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 24,
                    Fill = Color.FromArgb("#14264A"),
                    Stroke = Colors.Transparent,
                    StrokeThickness = 0
                }
            });

            string? currentPassword = passwordResult.Result;
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                return;
            }

            try
            {
                IsDeletingAccount = true;
                await apiClient.DeleteAsync(
                    "api/profile/me",
                    new DeleteMyAccountRequest { CurrentPassword = currentPassword });

                await sessionService.SignOutAsync();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsDeletingAccount = false;
            }
        }

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanSave));

            StartEditingCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsSavingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanSave));

            StartEditingCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsDeletingAccountChanged(bool value)
        {
            OnPropertyChanged(nameof(CanDeleteAccount));
        }

        partial void OnIsEditingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanSave));

            StartEditingCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private static async Task OpenChangePasswordAsync()
        {
            await Shell.Current.GoToAsync("change-password");
        }
    }
}
