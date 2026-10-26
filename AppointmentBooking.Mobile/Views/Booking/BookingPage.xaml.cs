using AppointmentBooking.Mobile.ViewModels.Booking;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Views.Booking
{
    public partial class BookingPage : ContentPage
    {
        private readonly BookingViewModel viewModel;

        public BookingPage(BookingViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            BindingContext = viewModel;
        }

        public Task PreloadAsync() =>
            viewModel.EnsureLoadedAsync();

        private async void OnCalendarDateClicked(object? sender, EventArgs e)
        {
            if (sender is not Button { BindingContext: CalendarDayItem day } ||
                !day.IsSelectable)
            {
                return;
            }

            double scrollY = BookingScrollView.ScrollY;
            await viewModel.SelectDateCommand.ExecuteAsync(day);

            // Updating the availability section changes the content height.
            // Restore the user's position after MAUI lays out the new slots.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Yield();
                await BookingScrollView.ScrollToAsync(0, scrollY, false);
            });
        }
    }
}
