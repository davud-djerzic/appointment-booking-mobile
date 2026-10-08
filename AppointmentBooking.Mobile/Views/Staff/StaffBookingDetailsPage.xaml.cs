using AppointmentBooking.Mobile.Models.Bookings.Responses;

namespace AppointmentBooking.Mobile.Views.Staff
{
    public partial class StaffBookingDetailsPage : ContentPage
    {
        public StaffBookingDetailsPage(
            EmployeeBookingDetailsResponse booking)
        {
            InitializeComponent();

            BindingContext = booking;
        }

        private async void OnCloseClicked(
            object? sender,
            EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
