using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Models.EmployeeUnavailability;
using AppointmentBooking.Mobile.Models.EmployeeWorkingHours;
using AppointmentBooking.Mobile.ViewModels.Staff;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

namespace AppointmentBooking.Mobile.Views.Staff
{
    public partial class StaffPanelPage : ContentPage
    {
        private readonly StaffAvailabilityViewModel viewModel;
        private readonly ISessionService sessionService;
        private Task? preloadTask;
        private bool isEditorPopupOpen;
        // Set while a "Obriši" or "Odjava" confirmation is open or the action runs.
        private bool isConfirmationOpen;

        // One action at a time on this page: while an editor, a delete or the logout confirmation
        // is open, taps on the other buttons are ignored (no two windows at once).
        private bool IsAnyActionOpen => isEditorPopupOpen || isConfirmationOpen;

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
            await PreloadAsync();
        }

        public Task PreloadAsync() => preloadTask ??= LoadInitialDataAsync();

        public void ResetCachedData()
        {
            preloadTask = null;
            viewModel.ResetCachedData();
        }

        private async Task LoadInitialDataAsync()
        {
            await Task.WhenAll(
                viewModel.LoadBlocksCommand.ExecuteAsync(null),
                viewModel.LoadWorkingHoursCommand.ExecuteAsync(null));
        }

        private async void OnAddClicked(object? sender, EventArgs e)
        {
            if (IsAnyActionOpen)
            {
                return;
            }

            viewModel.BeginCreateCommand.Execute(null);
            await ShowEditorPopupAsync();
        }

        private async void OnAddVacationClicked(object? sender, EventArgs e)
        {
            if (IsAnyActionOpen)
            {
                return;
            }

            viewModel.BeginCreateCommand.Execute(null);
            viewModel.SelectVacationCommand.Execute(null);
            await ShowEditorPopupAsync();
        }

        private void OnRecurringBreakDayClicked(object? sender, EventArgs e)
        {
            if (sender is Button { CommandParameter: RecurringBreakDayOption day })
            {
                viewModel.ToggleRecurringBreakDayCommand.Execute(day);
            }
        }

        private async void OnEditClicked(object? sender, EventArgs e)
        {
            if (IsAnyActionOpen ||
                sender is not Button { CommandParameter: EmployeeUnavailabilityResponse block })
            {
                return;
            }

            viewModel.EditBlockCommand.Execute(block);
            if (viewModel.IsFormVisible)
            {
                await ShowEditorPopupAsync();
            }
        }

        private async Task ShowEditorPopupAsync()
        {
            // Ignore repeated taps while the editor is already opening or open.
            if (isEditorPopupOpen)
            {
                return;
            }

            isEditorPopupOpen = true;
            try
            {
                await this.ShowPopupAsync<bool>(
                    new StaffAvailabilityPopup(viewModel, this),
                    new PopupOptions
                    {
                        PageOverlayColor = Color.FromArgb("#99000000"),
                        Shape = new RoundRectangle
                        {
                            CornerRadius = 22,
                            Fill = Color.FromArgb("#14264A"),
                            Stroke = Colors.Transparent,
                            StrokeThickness = 0
                        }
                    });
            }
            finally
            {
                isEditorPopupOpen = false;
            }
        }

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (IsAnyActionOpen ||
                sender is not Button { CommandParameter: EmployeeUnavailabilityResponse block })
            {
                return;
            }

            isConfirmationOpen = true;
            try
            {
                await viewModel.DeleteBlockCommand.ExecuteAsync(block);
            }
            finally
            {
                isConfirmationOpen = false;
            }
        }

        private async void OnLogoutClicked(object? sender, EventArgs e)
        {
            if (IsAnyActionOpen)
            {
                return;
            }

            isConfirmationOpen = true;
            try
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
            finally
            {
                isConfirmationOpen = false;
            }
        }
    }
}
