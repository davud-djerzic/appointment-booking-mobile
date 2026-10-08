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

    private async void OnEmployeeSelectionChanged(object? sender, EventArgs e)
    {
        await viewModel.LoadServicesForSelectedEmployeeAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
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
