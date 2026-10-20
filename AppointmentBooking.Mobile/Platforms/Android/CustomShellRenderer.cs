using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace AppointmentBooking.Mobile.Platforms.Android;

public sealed class CustomShellRenderer : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem) =>
        new CustomBottomNavViewAppearanceTracker(this, shellItem);
}

internal sealed class CustomBottomNavViewAppearanceTracker : ShellBottomNavViewAppearanceTracker
{
    public CustomBottomNavViewAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
        : base(shellContext, shellItem)
    {
    }

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);

        bottomView.ItemActiveIndicatorEnabled = false;
        bottomView.ItemTextColor = ColorStateList.ValueOf(global::Android.Graphics.Color.White);
        bottomView.LabelVisibilityMode = 1;
        bottomView.ItemIconSize = ToPixels(bottomView, 18);

        if (bottomView.GetChildAt(0) is not global::Android.Views.ViewGroup menuView)
            return;

        int insetHorizontal = ToPixels(bottomView, 3);
        int insetTop = ToPixels(bottomView, 12);
        int insetBottom = ToPixels(bottomView, 0);

        for (int index = 0; index < menuView.ChildCount; index++)
        {
            if (menuView.GetChildAt(index) is global::Android.Views.View itemView)
            {
                SetIconAndLabelLayout(itemView, bottomView);

                StateListDrawable background = new();
                background.AddState(
                    [global::Android.Resource.Attribute.StateChecked],
                    CreateActiveTabBackground(bottomView));
                background.AddState([], new ColorDrawable(global::Android.Graphics.Color.Transparent));
                itemView.Background = new InsetDrawable(
                    background,
                    insetHorizontal,
                    insetTop,
                    insetHorizontal,
                    insetBottom);
            }
        }
    }

    private static GradientDrawable CreateActiveTabBackground(global::Android.Views.View view)
    {
        GradientDrawable background = new();
        background.SetColor(global::Android.Graphics.Color.Transparent);
        background.SetStroke(
            ToPixels(view, 2),
            global::Android.Graphics.Color.Rgb(231, 196, 90));
        background.SetCornerRadius(24);
        return background;
    }

    private static void SetIconAndLabelLayout(
        global::Android.Views.View view,
        global::Android.Views.View densityContext)
    {
        int labelOffset = ToPixels(densityContext, 5);

        if (view is ImageView)
        {
            return;
        }

        if (view is TextView textView)
        {
            textView.SetTextSize(ComplexUnitType.Sp, 9);
            textView.TranslationY = labelOffset;
            return;
        }

        if (view is global::Android.Views.ViewGroup group)
        {
            for (int index = 0; index < group.ChildCount; index++)
            {
                if (group.GetChildAt(index) is global::Android.Views.View child)
                    SetIconAndLabelLayout(child, densityContext);
            }
        }
    }

    private static int ToPixels(global::Android.Views.View view, int densityIndependentPixels) =>
        ToPixels(view.Resources?.DisplayMetrics?.Density ?? 1, densityIndependentPixels);

    private static int ToPixels(float density, int densityIndependentPixels) =>
        (int)(densityIndependentPixels * density + 0.5f);
}
