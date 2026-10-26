using AppointmentBooking.Mobile.Models.BookableServices;
using AppointmentBooking.Mobile.ViewModels.Staff;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;

namespace AppointmentBooking.Mobile.Views.Staff;

public partial class StaffServicesPage : ContentPage
{
    private readonly StaffServicesViewModel viewModel;
    private bool isPopupOpen;

    public StaffServicesPage(StaffServicesViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.PreloadAsync();
    }

    public Task PreloadAsync() => viewModel.PreloadAsync();

    public void ResetCachedData() => viewModel.ResetCachedData();

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        if (isPopupOpen)
        {
            return;
        }

        viewModel.BeginCreate();
        await ShowEditorAsync();
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (isPopupOpen || sender is not Button { CommandParameter: StaffServiceItem item })
        {
            return;
        }

        viewModel.BeginEdit(item);
        await ShowEditorAsync();
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (isPopupOpen || sender is not Button { CommandParameter: StaffServiceItem item })
        {
            return;
        }

        await viewModel.DeleteAsync(item);
    }

    private async Task ShowEditorAsync()
    {
        // Ignore repeated taps while the editor is already opening or open.
        if (isPopupOpen)
        {
            return;
        }

        isPopupOpen = true;
        try
        {
            await this.ShowPopupAsync<bool>(
                new StaffServicePopup(viewModel),
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
            isPopupOpen = false;
        }
    }
}
