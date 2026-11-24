using AppointmentBooking.Mobile.Models.Bookings.Responses;

namespace AppointmentBooking.Mobile.Views.Staff
{
    public partial class StaffBookingDetailsPage : ContentPage
    {
        // Both "Zatvori" and "✕" close the page; a second tap must not pop a second time.
        private bool isClosing;

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
            if (isClosing)
            {
                return;
            }

            isClosing = true;
            await Navigation.PopModalAsync();
        }
    }
}
