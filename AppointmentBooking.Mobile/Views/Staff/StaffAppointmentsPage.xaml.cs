using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.ViewModels.Staff;

namespace AppointmentBooking.Mobile.Views.Staff
{
    public partial class StaffAppointmentsPage : ContentPage
    {
        private readonly StaffAppointmentsViewModel viewModel;
        private bool hasLoaded;
        private Task? preloadTask;
        private bool isDetailsModalOpen;
        private bool isCreateBookingModalOpen;
        private Window? resumeWindow;

        // "Novi termin" is built once and reused while the app runs:
        // building the page is the slow part of opening it.
        private StaffCreateBookingPage? createBookingPage;

        public StaffAppointmentsPage(StaffAppointmentsViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            BindingContext = viewModel;
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

        private async void OnCancelStaffBookingClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not EmployeeBookingSummaryResponse booking)
            {
                return;
            }

            await viewModel.CancelStaffBookingCommand.ExecuteAsync(booking);
        }

        private async void OnCreateStaffBookingClicked(object? sender, EventArgs e)
        {
            if (isCreateBookingModalOpen)
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

            if (!isDetailsModalOpen && !isCreateBookingModalOpen)
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

            // A different user signs in next: build "Novi termin" again for them.
            createBookingPage = null;
            viewModel.ResetCachedData();
        }
    }
}
