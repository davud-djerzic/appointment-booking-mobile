using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Models.EmployeeUnavailability;
using AppointmentBooking.Mobile.ViewModels.Staff;

namespace AppointmentBooking.Mobile.Views.Staff
{
    public partial class StaffPanelPage : ContentPage
    {
        private readonly StaffAvailabilityViewModel viewModel;
        private readonly ISessionService sessionService;

        public StaffPanelPage(
            StaffAvailabilityViewModel viewModel,
            ISessionService sessionService)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.sessionService = sessionService;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await Task.WhenAll(
                viewModel.LoadBlocksCommand.ExecuteAsync(null),
                viewModel.LoadWorkingHoursCommand.ExecuteAsync(null));
        }

        private async void OnAddClicked(object? sender, EventArgs e)
        {
            viewModel.BeginCreateCommand.Execute(null);
            await ContentScroll.ScrollToAsync(EditorCard, ScrollToPosition.Start, true);
        }

        private async void OnEditClicked(object? sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: EmployeeUnavailabilityResponse block })
            {
                return;
            }

            viewModel.EditBlockCommand.Execute(block);
            if (viewModel.IsFormVisible)
            {
                await ContentScroll.ScrollToAsync(EditorCard, ScrollToPosition.Start, true);
            }
        }

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (sender is Button { CommandParameter: EmployeeUnavailabilityResponse block })
            {
                await viewModel.DeleteBlockCommand.ExecuteAsync(block);
            }
        }

        private async void OnLogoutClicked(object? sender, EventArgs e)
        {
            bool confirmed =
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Odjava",
                    "Da li ste sigurni da se želite odjaviti?",
                    "Odjavi se",
                    "Odustani");

            if (!confirmed)
            {
                return;
            }

            await sessionService.SignOutAsync();
        }
    }
}
