using AppointmentBooking.Mobile.ViewModels.Staff;
using AppointmentBooking.Mobile.Views.Shared;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

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

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnStaffBookingDateClicked(object? sender, EventArgs e)
    {
        var result = await this.ShowPopupAsync<DateTime?>(
            new CalendarDatePickerPopup(
                viewModel.StaffBookingDate,
                viewModel.StaffBookingToday,
                "Odaberite datum termina"));

        if (result.WasDismissedByTappingOutsideOfPopup is false && result.Result is DateTime date)
        {
            viewModel.StaffBookingDate = date;
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        await viewModel.CreateStaffBookingCommand.ExecuteAsync(null);

        if (viewModel.WasStaffBookingCreated)
        {
            await Navigation.PopModalAsync();
        }
    }
}
