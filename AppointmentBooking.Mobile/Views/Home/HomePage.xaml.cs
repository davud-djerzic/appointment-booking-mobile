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
                    not nameof(HomeViewModel.IsAvailabilityModalVisible))
            {
                return;
            }

            bool isVisible =
                viewModel.IsServicePickerVisible ||
                viewModel.IsAvailabilityModalVisible;
            bool isAvailabilityVisible = viewModel.IsAvailabilityModalVisible;

            MainThread.BeginInvokeOnMainThread(
                () => SetBackdropBlur(isVisible, isAvailabilityVisible));
        }

        private void SetBackdropBlur(bool isVisible, bool isAvailabilityVisible)
        {
#if ANDROID
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                Android.Graphics.RenderEffect? effect = isVisible
                    ? Android.Graphics.RenderEffect.CreateBlurEffect(
                        5f,
                        5f,
                        Android.Graphics.Shader.TileMode.Clamp)
                    : null;

                ApplyAndroidBlur(HeaderGrid, effect);
                ApplyAndroidBlur(MainScrollView, effect);
            }
#elif IOS
            UIView? rootView = Content?.Handler?.PlatformView as UIView;
            VisualElement overlayElement = isAvailabilityVisible
                ? AvailableTimesOverlay
                : ServicePickerOverlay;
            UIView? overlayView = overlayElement.Handler?.PlatformView as UIView;

            if (rootView is null || overlayView is null)
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
                rootView.BringSubviewToFront(overlayView);
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
