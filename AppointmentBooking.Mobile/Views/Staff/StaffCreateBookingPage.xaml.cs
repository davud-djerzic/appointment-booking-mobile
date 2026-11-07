using AppointmentBooking.Mobile.ViewModels.Booking;
using AppointmentBooking.Mobile.ViewModels.Staff;

namespace AppointmentBooking.Mobile.Views.Staff;

public partial class StaffCreateBookingPage : ContentPage
{
    private readonly StaffAppointmentsViewModel viewModel;

    public StaffCreateBookingPage(StaffAppointmentsViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    // The page is reused; start every opening at the top.
    // (Only needed when the page was shown before and still has its native view.)
    public void PrepareForShow()
    {
        if (BookingScrollView.Handler is not null)
        {
            _ = BookingScrollView.ScrollToAsync(0, 0, false);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnBookingCalendarDateClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: CalendarDayItem day } ||
            !day.IsSelectable)
        {
            return;
        }

        double scrollY = BookingScrollView.ScrollY;
        await viewModel.SelectBookingDateCommand.ExecuteAsync(day);

        // Loading the free slots changes the content height; keep the user's position
        // (same as the customer booking page).
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
            await BookingScrollView.ScrollToAsync(0, scrollY, false);
        });
    }

    private async void OnConfirmStaffBookingClicked(object? sender, EventArgs e)
    {
        await viewModel.ConfirmStaffBookingCommand.ExecuteAsync(null);

        if (viewModel.WasStaffBookingCreated)
        {
            await Navigation.PopModalAsync();
        }
    }
}
