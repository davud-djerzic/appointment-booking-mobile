using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.ViewModels.Appointment;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Views.Appointments
{
    public partial class AppointmentsPage : ContentPage
    {
        private readonly AppointmentViewModel viewModel;
        private bool hasLoaded;
        private bool isDetailsModalOpen;

        public AppointmentsPage(AppointmentViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            BindingContext = viewModel;
        }

        private async void OnDetailsClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not BookingSummaryResponse appointment)
            {
                return;
            }

            BookingDetailsResponse? details =
                await viewModel.GetBookingDetailsAsync(appointment.Id);

            if (details is null)
            {
                return;
            }

            isDetailsModalOpen = true;
            await Navigation.PushModalAsync(
                new AppointmentDetailsPage(details));
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

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not BookingSummaryResponse appointment)
            {
                return;
            }

            bool confirmed = await DisplayAlertAsync(
                "Otkazivanje termina",
                "Da li ste sigurni da želite otkazati ovaj termin?",
                "Da, otkaži",
                "Ne");

            if (!confirmed)
            {
                return;
            }

            bool cancelled =
                await viewModel.CancelBookingAsync(appointment.Id);

            if (!cancelled)
            {
                return;
            }

            viewModel.MoveToCancelled(appointment);
        }

        private void OnUpcomingTabTapped(object? sender, TappedEventArgs e)
        {
            ShowTab("upcoming");
        }

        private void OnPreviousTabTapped(object? sender, TappedEventArgs e)
        {
            ShowTab("previous");
        }

        private void OnCancelledTabTapped(object? sender, TappedEventArgs e)
        {
            ShowTab("cancelled");
        }

        private void ShowTab(string tab)
        {
            UpcomingCollection.IsVisible = tab == "upcoming";
            PreviousCollection.IsVisible = tab == "previous";
            CancelledCollection.IsVisible = tab == "cancelled";

            bool upcomingSelected = tab == "upcoming";
            bool previousSelected = tab == "previous";
            bool cancelledSelected = tab == "cancelled";

            UpcomingTab.BackgroundColor = upcomingSelected
                ? GetResourceColor("Primary")
                : Colors.Transparent;

            PreviousTab.BackgroundColor = previousSelected
                ? GetResourceColor("Primary")
                : Colors.Transparent;

            CancelledTab.BackgroundColor = cancelledSelected
                ? GetResourceColor("Primary")
                : Colors.Transparent;

            SetTabTextColor(UpcomingTab, upcomingSelected);
            SetTabTextColor(PreviousTab, previousSelected);
            SetTabTextColor(CancelledTab, cancelledSelected);
        }

        private static void SetTabTextColor(Border tab, bool selected)
        {
            if (tab.Content is Label label)
            {
                label.TextColor = selected
                    ? Colors.White
                    : GetResourceColor("TextSecondary");
            }
        }

        private static Color GetResourceColor(string key)
        {
            return Application.Current?.Resources.TryGetValue(key, out object? value) == true
                && value is Color color
                    ? color
                    : Colors.Black;
        }
    }

}
