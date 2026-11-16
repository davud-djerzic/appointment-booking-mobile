using AppointmentBooking.Mobile.ViewModels.Booking;
using System.ComponentModel;
using System.Runtime.Versioning;
#if IOS
using UIKit;
#endif

namespace AppointmentBooking.Mobile.Views.Booking
{
    public partial class BookingPage : ContentPage
    {
        private readonly BookingViewModel viewModel;

        public BookingPage(BookingViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            BindingContext = viewModel;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

#if IOS
            SizeChanged += (_, _) => ResizeBackdropBlur();
#endif
        }

        public Task PreloadAsync() =>
            viewModel.EnsureLoadedAsync();

        private async void OnCalendarDateClicked(object? sender, EventArgs e)
        {
            if (sender is not Button { BindingContext: CalendarDayItem day } ||
                !day.IsSelectable)
            {
                return;
            }

            double scrollY = BookingScrollView.ScrollY;
            await viewModel.SelectDateCommand.ExecuteAsync(day);

            // Updating the availability section changes the content height.
            // Restore the user's position after MAUI lays out the new slots.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Yield();
                await BookingScrollView.ScrollToAsync(0, scrollY, false);
            });
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(BookingViewModel.IsHoldConfirmationVisible))
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(UpdateHoldConfirmationState);
        }

        // While the hold confirmation panel is open it behaves like a modal window (same as on Home):
        // the page behind it is blurred, cannot be scrolled or tapped, and the tab bar is hidden.
        private void UpdateHoldConfirmationState()
        {
            bool isOpen = viewModel.IsHoldConfirmationVisible;

            HeaderLayout.InputTransparent = isOpen;
            BookingScrollView.InputTransparent = isOpen;
            Shell.SetTabBarIsVisible(this, !isOpen);

            SetBackdropBlur(isOpen);
        }

        // Android back button closes the panel (like ODUSTANI) instead of leaving the page.
        protected override bool OnBackButtonPressed()
        {
            if (viewModel.IsHoldConfirmationVisible)
            {
                // While the booking is being confirmed or released, wait for the result.
                if (!viewModel.IsCreatingHold)
                {
                    viewModel.CancelHoldCommand.Execute(null);
                }

                return true;
            }

            return base.OnBackButtonPressed();
        }

        private void SetBackdropBlur(bool isVisible)
        {
#if ANDROID
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                Android.Graphics.RenderEffect? effect = isVisible
                    ? Android.Graphics.RenderEffect.CreateBlurEffect(5f, 5f, Android.Graphics.Shader.TileMode.Clamp!)
                    : null;

                ApplyAndroidBlur(HeaderLayout, effect);
                ApplyAndroidBlur(BookingScrollView, effect);
            }
#elif IOS
            UIView? rootView = Content?.Handler?.PlatformView as UIView;
            UIView? overlayView = HoldConfirmationOverlay.Handler?.PlatformView as UIView;

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
        [SupportedOSPlatform("android31.0")]
        private static void ApplyAndroidBlur(VisualElement element, Android.Graphics.RenderEffect? effect)
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
    }
}
