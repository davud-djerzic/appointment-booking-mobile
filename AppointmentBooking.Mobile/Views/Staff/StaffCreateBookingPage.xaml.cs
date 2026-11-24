using AppointmentBooking.Mobile.ViewModels.Booking;
using AppointmentBooking.Mobile.ViewModels.Staff;
using System.ComponentModel;

namespace AppointmentBooking.Mobile.Views.Staff;

public partial class StaffCreateBookingPage : ContentPage
{
    private readonly StaffAppointmentsViewModel viewModel;

    // Set once the page starts closing ("✕" or after a saved booking), so that a second tap,
    // or "✕" together with "Potvrdi", never pops the page twice. Reset when the page is reused.
    private bool isClosing;

    public StaffCreateBookingPage(StaffAppointmentsViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateHoldConfirmationState();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(StaffAppointmentsViewModel.IsStaffHoldConfirmationVisible))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(UpdateHoldConfirmationState);
    }

    // While "Potvrda termina" is open, the form behind it cannot be tapped or scrolled
    // (same as the customer booking page). Taps used to reach the time slots through the dark
    // backdrop and try a second hold, which the API rejected as "temporarily reserved".
    private void UpdateHoldConfirmationState()
    {
        bool isOpen = viewModel.IsStaffHoldConfirmationVisible;

        HeaderLayout.InputTransparent = isOpen;
        BookingScrollView.InputTransparent = isOpen;
    }

    // Android back button closes "Potvrda termina" (like ODUSTANI, the hold is released)
    // instead of closing the whole "Novi termin" page.
    protected override bool OnBackButtonPressed()
    {
        if (viewModel.IsStaffHoldConfirmationVisible)
        {
            // While the booking is being confirmed or released, wait for the result.
            if (!viewModel.IsCreatingStaffHold)
            {
                viewModel.CancelStaffHoldCommand.Execute(null);
            }

            return true;
        }

        return base.OnBackButtonPressed();
    }

    // The page is reused; start every opening at the top.
    // (Only needed when the page was shown before and still has its native view.)
    public void PrepareForShow()
    {
        isClosing = false;

        if (BookingScrollView.Handler is not null)
        {
            _ = BookingScrollView.ScrollToAsync(0, 0, false);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
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

        if (viewModel.WasStaffBookingCreated && !isClosing)
        {
            isClosing = true;
            await Navigation.PopModalAsync();
        }
    }
}
