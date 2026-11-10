using AppointmentBooking.Mobile.ViewModels.Home;
using AppointmentBooking.Mobile.Views.Booking;
using System.ComponentModel;
#if ANDROID
using Android.Graphics;
#endif
#if IOS
using UIKit;
#endif

namespace AppointmentBooking.Mobile.Views.Home
{
    public partial class HomePage : ContentPage
    {
        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

#if IOS
            SizeChanged += (_, _) => ResizeBackdropBlur();
#endif
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not HomeViewModel viewModel ||
                e.PropertyName is not nameof(HomeViewModel.IsServicePickerVisible) and
                    not nameof(HomeViewModel.IsAvailabilityModalVisible) and
                    not nameof(HomeViewModel.IsHoldConfirmationVisible))
            {
                return;
            }

            // The state is read when the update runs, so switching from one popup to the next
            // (e.g. free slots -> confirmation) is applied as one change, without a flicker.
            MainThread.BeginInvokeOnMainThread(() => UpdateQuickBookingPopupState(viewModel));
        }

        // While a quick-booking popup is open it behaves like a modal window: the page behind
        // it is blurred and does not react to touches, and the tab bar is hidden so the user
        // cannot leave Home with a half-finished booking (or an active hold).
        private void UpdateQuickBookingPopupState(HomeViewModel viewModel)
        {
            VisualElement? openPopup =
                viewModel.IsHoldConfirmationVisible ? HoldConfirmationOverlay :
                viewModel.IsAvailabilityModalVisible ? AvailableTimesOverlay :
                viewModel.IsServicePickerVisible ? ServicePickerOverlay :
                null;

            bool isPopupOpen = openPopup is not null;

            HeaderGrid.InputTransparent = isPopupOpen;
            MainScrollView.InputTransparent = isPopupOpen;
            Shell.SetTabBarIsVisible(this, !isPopupOpen);

            SetBackdropBlur(openPopup);
        }

        // Android back button closes the open popup (like its cancel button) instead of
        // leaving the page; an active hold is released through the same command.
        protected override bool OnBackButtonPressed()
        {
            if (BindingContext is not HomeViewModel viewModel)
            {
                return base.OnBackButtonPressed();
            }

            if (viewModel.IsHoldConfirmationVisible)
            {
                // While the booking is being confirmed, wait for the result.
                if (!viewModel.IsLoading)
                {
                    viewModel.CancelHoldConfirmationCommand.Execute(null);
                }

                return true;
            }

            if (viewModel.IsAvailabilityModalVisible)
            {
                viewModel.CancelAvailabilityCommand.Execute(null);
                return true;
            }

            if (viewModel.IsServicePickerVisible)
            {
                viewModel.CloseServicePickerCommand.Execute(null);
                return true;
            }

            return base.OnBackButtonPressed();
        }

        private void SetBackdropBlur(VisualElement? openPopup)
        {
#if ANDROID
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                Android.Graphics.RenderEffect? effect = openPopup is not null
                    ? Android.Graphics.RenderEffect.CreateBlurEffect(
                        5f,
                        5f,
                        Android.Graphics.Shader.TileMode.Clamp)
                    : null;

                ApplyAndroidBlur(HeaderGrid, effect);
                ApplyAndroidBlur(MainScrollView, effect);
            }
#elif IOS
            bool isVisible = openPopup is not null;
            UIView? rootView = Content?.Handler?.PlatformView as UIView;
            UIView? overlayView = openPopup?.Handler?.PlatformView as UIView;

            if (rootView is null || (isVisible && overlayView is null))
            {
                return;
            }

            if (isVisible)
            {
                if (backdropBlurView is null)
                {
                    backdropBlurView = new UIVisualEffectView(
                        UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemUltraThinMaterialDark))
                    {
                        UserInteractionEnabled = false,
                        Alpha = 0.45f
                    };

                    int overlayIndex = Array.IndexOf(rootView.Subviews, overlayView);
                    rootView.InsertSubview(backdropBlurView, Math.Max(0, overlayIndex));
                }

                ResizeBackdropBlur();
                rootView.BringSubviewToFront(overlayView!);
            }
            else
            {
                backdropBlurView?.RemoveFromSuperview();
                backdropBlurView?.Dispose();
                backdropBlurView = null;
            }
#endif
        }

#if ANDROID
        private static void ApplyAndroidBlur(
            VisualElement element,
            Android.Graphics.RenderEffect? effect)
        {
            if (element.Handler?.PlatformView is Android.Views.View platformView)
            {
                platformView.SetRenderEffect(effect);
            }
        }
#endif

#if IOS
        private UIVisualEffectView? backdropBlurView;

        private void ResizeBackdropBlur()
        {
            if (backdropBlurView is not null && Content?.Handler?.PlatformView is UIView rootView)
            {
                backdropBlurView.Frame = rootView.Bounds;
            }
        }
#endif

        private void OnBookingClicked(object? sender, TappedEventArgs e)
        {
            if (Shell.Current is AppShell appShell)
            {
                appShell.ShowBookingTab();
            }
        }
    }
}
