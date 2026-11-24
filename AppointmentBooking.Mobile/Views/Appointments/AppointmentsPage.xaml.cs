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
        private bool isDetailsModalOpen;
        private bool isCancellationConfirmationOpen;
        // Set while "Detalji termina" loads and opens, so quick repeated taps open it only once.
        private bool isOpeningDetails;

        // One action at a time on this page: while "Detalji termina" or "Otkaži" is opening,
        // open or running, taps on the other buttons are ignored (no two windows at once).
        private bool IsAnyActionOpen =>
            isOpeningDetails || isDetailsModalOpen || isCancellationConfirmationOpen;

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

            if (IsAnyActionOpen)
            {
                return;
            }

            isOpeningDetails = true;
            try
            {
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
            finally
            {
                isOpeningDetails = false;
            }
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (isDetailsModalOpen)
            {
                isDetailsModalOpen = false;
                return;
            }

            // Loaded at app start; only loads here if that failed.
            await viewModel.EnsureLoadedAsync();
        }

        /// <summary>Loads the customer's appointments while the startup panel is shown.</summary>
        public Task PreloadAsync() => viewModel.EnsureLoadedAsync();

        public void ResetCachedData() => viewModel.ResetCachedData();

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            if (IsAnyActionOpen ||
                sender is not Button button ||
                button.CommandParameter is not BookingSummaryResponse appointment)
            {
                return;
            }

            // Stays set during the confirmation and the cancellation request, so a second tap
            // (on this or another button) does nothing until it is finished.
            isCancellationConfirmationOpen = true;
            try
            {
                bool confirmed = await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
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
            finally
            {
                isCancellationConfirmationOpen = false;
            }
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
                ? GetResourceColor("BrandButton")
                : Colors.Transparent;

            PreviousTab.BackgroundColor = previousSelected
                ? GetResourceColor("BrandButton")
                : Colors.Transparent;

            CancelledTab.BackgroundColor = cancelledSelected
                ? GetResourceColor("BrandButton")
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
                    ? GetResourceColor("BrandBackground")
                    : GetResourceColor("TextOnBrandBackground");
                label.Opacity = selected ? 1.0 : 0.7;
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
