using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Services.Dialogs;
using AppointmentBooking.Mobile.ViewModels.Staff;
using System.ComponentModel;

namespace AppointmentBooking.Mobile.Views.Staff
{
    public partial class StaffAppointmentsPage : ContentPage
    {
        private readonly StaffAppointmentsViewModel viewModel;
        private bool hasLoaded;
        private Task? preloadTask;
        private bool isDetailsModalOpen;
        private bool isCreateBookingModalOpen;
        // Set while "Detalji termina" loads and opens, so quick repeated taps open it only once.
        private bool isOpeningDetails;
        // Set while the "Otkaži" popup is open (and the cancellation runs). The popup is shown as
        // a page above this one, so closing it raises Appearing: without this flag the page would
        // jump back to the next working day and reload. Also blocks a second popup on quick taps.
        private bool isCancelPopupOpen;

        // One action at a time on this page: while "Detalji termina", "Otkaži" or "Novi termin"
        // is opening, open or running, taps on the other buttons are ignored (no two windows at once).
        private bool IsAnyActionOpen =>
            isOpeningDetails || isDetailsModalOpen || isCancelPopupOpen || isCreateBookingModalOpen;

        private Window? resumeWindow;

        // "Novi termin" is built once and reused while the app runs:
        // building the page is the slow part of opening it.
        private StaffCreateBookingPage? createBookingPage;

        public StaffAppointmentsPage(StaffAppointmentsViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            BindingContext = viewModel;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private async void OnBookingDetailsClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not EmployeeBookingSummaryResponse summary)
            {
                return;
            }

            if (IsAnyActionOpen)
            {
                return;
            }

            isOpeningDetails = true;
            try
            {
                EmployeeBookingDetailsResponse? details =
                    await viewModel.GetBookingDetailsAsync(summary.Id);

                if (details is null)
                {
                    return;
                }

                isDetailsModalOpen = true;
                await Navigation.PushModalAsync(
                    new StaffBookingDetailsPage(details));
            }
            finally
            {
                isOpeningDetails = false;
            }
        }

        // Another day (or "Prošli" / "Aktuelni i naredni") starts at the top of the list.
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not (nameof(StaffAppointmentsViewModel.SelectedDate)
                    or nameof(StaffAppointmentsViewModel.IsShowingPassedToday)))
            {
                return;
            }

            if (AppointmentsScrollView.Handler is not null && AppointmentsScrollView.ScrollY > 0)
            {
                _ = AppointmentsScrollView.ScrollToAsync(0, 0, false);
            }
        }

        private async void OnCancelStaffBookingClicked(
            object? sender,
            EventArgs e)
        {
            if (IsAnyActionOpen ||
                sender is not Button button ||
                button.CommandParameter is not EmployeeBookingSummaryResponse booking)
            {
                return;
            }

            isCancelPopupOpen = true;
            try
            {
                // "Nazad" changes nothing; a confirmed cancellation reloads only the shown day.
                await viewModel.CancelStaffBookingCommand.ExecuteAsync(booking);
            }
            finally
            {
                isCancelPopupOpen = false;
            }
        }

        private async void OnCreateStaffBookingClicked(object? sender, EventArgs e)
        {
            if (IsAnyActionOpen)
            {
                return;
            }

            isCreateBookingModalOpen = true;

            // Open at once with an empty form; the data is filled in while it is shown.
            viewModel.ResetStaffBookingForm();
            StaffCreateBookingPage page = createBookingPage ??= new StaffCreateBookingPage(viewModel);
            page.PrepareForShow();

            await Navigation.PushModalAsync(page);

            bool isReady = await viewModel.LoadStaffBookingDataAsync();

            if (!isReady && Navigation.ModalStack.LastOrDefault() == page)
            {
                await Navigation.PopModalAsync();
            }
        }

        // Builds "Novi termin" in advance, right after the list has loaded,
        // so that even the first tap opens it without waiting.
        private void PrewarmCreateBookingPage()
        {
            if (createBookingPage is not null)
            {
                return;
            }

            Dispatcher.Dispatch(() => createBookingPage ??= new StaffCreateBookingPage(viewModel));
        }

        private void OnUpcomingTodayTapped(object? sender, TappedEventArgs e)
        {
            viewModel.ShowUpcomingTodayCommand.Execute(null);
        }

        private void OnPassedTodayTapped(object? sender, TappedEventArgs e)
        {
            viewModel.ShowPassedTodayCommand.Execute(null);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            SubscribeToAppResume();

            if (isDetailsModalOpen)
            {
                isDetailsModalOpen = false;
                return;
            }

            // Back from the "Otkaži" popup or from an alert (e.g. an error message):
            // stay on the shown day without reloading.
            if (isCancelPopupOpen || AlertPanelService.IsShowing)
            {
                return;
            }

            if (isCreateBookingModalOpen)
            {
                isCreateBookingModalOpen = false;

                // A new booking was saved: show its day (already selected by the view model).
                if (viewModel.WasStaffBookingCreated)
                {
                    await viewModel.LoadAppointmentsCommand.ExecuteAsync(null);
                }

                return;
            }

            if (preloadTask is { IsCompleted: false })
            {
                // Still loading from the startup panel; don't load twice.
                await preloadTask;
                return;
            }

            if (hasLoaded)
            {
                return;
            }

            viewModel.ResetToNextWorkingDay();
            await viewModel.LoadAppointmentsCommand.ExecuteAsync(null);

            hasLoaded = true;
            PrewarmCreateBookingPage();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            UnsubscribeFromAppResume();

            // A popup above this page is not leaving it: keep the shown day.
            if (!isDetailsModalOpen &&
                !isCreateBookingModalOpen &&
                !isCancelPopupOpen &&
                !AlertPanelService.IsShowing)
            {
                hasLoaded = false;
            }
        }

        // When the app comes back from the background, load the shown day again.
        private void SubscribeToAppResume()
        {
            if (resumeWindow is not null || Window is null)
            {
                return;
            }

            resumeWindow = Window;
            resumeWindow.Resumed += OnAppResumed;
        }

        private void UnsubscribeFromAppResume()
        {
            if (resumeWindow is null)
            {
                return;
            }

            resumeWindow.Resumed -= OnAppResumed;
            resumeWindow = null;
        }

        private async void OnAppResumed(object? sender, EventArgs e)
        {
            if (!hasLoaded)
            {
                return;
            }

            await viewModel.LoadAppointmentsCommand.ExecuteAsync(null);
        }

        /// <summary>Loads the appointments while the startup panel is shown.</summary>
        public Task PreloadAsync() => preloadTask ??= PreloadCoreAsync();

        private async Task PreloadCoreAsync()
        {
            viewModel.ResetToNextWorkingDay();
            await viewModel.LoadAppointmentsCommand.ExecuteAsync(null);
            hasLoaded = true;
            PrewarmCreateBookingPage();
        }

        public void ResetCachedData()
        {
            preloadTask = null;
            hasLoaded = false;
            isDetailsModalOpen = false;
            isCreateBookingModalOpen = false;
            isCancelPopupOpen = false;

            // A different user signs in next: build "Novi termin" again for them.
            createBookingPage = null;
            viewModel.ResetCachedData();
        }
    }
}
