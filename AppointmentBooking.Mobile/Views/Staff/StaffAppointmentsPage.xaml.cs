using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.ViewModels.Staff;

namespace AppointmentBooking.Mobile.Views.Staff
{
    public partial class StaffAppointmentsPage : ContentPage
    {
        private readonly StaffAppointmentsViewModel viewModel;
        private bool hasLoaded;
        private bool isDetailsModalOpen;

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
            bool isReady = await viewModel.PrepareStaffBookingAsync();
            if (!isReady)
            {
                return;
            }

            await Navigation.PushModalAsync(
                new StaffCreateBookingPage(viewModel));
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

            if (isDetailsModalOpen)
            {
                isDetailsModalOpen = false;
                return;
            }

            if (hasLoaded)
            {
                return;
            }

            viewModel.ResetToNextWorkingDay();
            await viewModel.LoadAppointmentsCommand.ExecuteAsync(null);

            hasLoaded = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (!isDetailsModalOpen)
            {
                hasLoaded = false;
            }
        }
    }
}
