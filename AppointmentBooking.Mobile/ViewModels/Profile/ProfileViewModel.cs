using AppointmentBooking.Mobile.Models.Profile.Requests;
using AppointmentBooking.Mobile.Models.Profile.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.ErrorHandling;
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

        private string originalFirstName = string.Empty;
        private string originalLastName = string.Empty;
        private string originalEmail = string.Empty;
        private string originalPhone = string.Empty;

        [ObservableProperty]
        private string firstName = string.Empty;

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
        private bool isEditing;

        public bool CanEdit =>
            !IsLoading &&
            !IsSaving &&
            !IsEditing;

        public bool CanSave =>
            !IsLoading &&
            !IsSaving &&
            IsEditing;

        public ProfileViewModel(
            IApiClient apiClient,
            IErrorHandler errorHandler,
            ISessionService sessionService)
        {
            this.apiClient = apiClient;
            this.errorHandler = errorHandler;
            this.sessionService = sessionService;
        }

        [RelayCommand]
        private async Task LoadProfileAsync()
        {
            try
            {
                IsLoading = true;

                MyProfileResponse? response =
                    await apiClient.GetAsync<MyProfileResponse>(
                        "api/profile/me");

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
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsLoading = false;

                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanSave));

                StartEditingCommand.NotifyCanExecuteChanged();
                SaveChangesCommand.NotifyCanExecuteChanged();
            }
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
                await Shell.Current.DisplayAlertAsync(
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

                IsEditing = false;

                await Shell.Current.DisplayAlertAsync(
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
        private async Task LogoutAsync()
        {
            bool confirmed =
                await Shell.Current.DisplayAlertAsync(
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