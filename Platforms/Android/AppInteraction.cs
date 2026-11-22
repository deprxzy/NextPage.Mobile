using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Handlers;
using NextPage.Mobile.Core;

namespace NextPage.Mobile.AndroidUi;

public sealed class GentleScrollViewHandler : ScrollViewHandler
{
    protected override Microsoft.Maui.Platform.MauiScrollView CreatePlatformView() => new GentleScrollView(Context);
}
public sealed class GentleScrollView(Android.Content.Context context) : Microsoft.Maui.Platform.MauiScrollView(context)
{
    public override bool OnGenericMotionEvent(MotionEvent? motion)
    {
        if (motion?.Action == MotionEventActions.Scroll && motion.GetAxisValue(Axis.Vscroll) is var axis && axis != 0)
        {
            var pixels = (int)(-Math.Clamp(axis, -1f, 1f) * 18 * (Resources?.DisplayMetrics?.Density ?? 1));
            ScrollBy(0, pixels);
            return true;
        }
        return base.OnGenericMotionEvent(motion);
    }
}

public static class AppInteraction
{
    public static void ConfigureMouse()
    {
        ViewHandler.ViewMapper.AppendToMapping("NextPageMouseWheel", (handler, virtualView) =>
        {
            if (virtualView is CollectionView && handler.PlatformView is Android.Views.View platform)
                platform.Post(() => platform.SetOnGenericMotionListener(new GentleWheel()));
        });
    }
    private sealed class GentleWheel : Java.Lang.Object, Android.Views.View.IOnGenericMotionListener
    {
        public bool OnGenericMotion(Android.Views.View? view, MotionEvent? motion)
        {
            if (motion?.Action != MotionEventActions.Scroll || view is null) return false;
            var axis = motion.GetAxisValue(Axis.Vscroll);
            if (axis == 0) return false;
            var pixels = (int)(-Math.Clamp(axis, -1f, 1f) * 18 * (view.Resources?.DisplayMetrics?.Density ?? 1));
            if (view is Android.Widget.ScrollView scroll) scroll.ScrollBy(0, pixels);
            else if (view is RecyclerView list) list.ScrollBy(0, pixels);
            else return false;
            return true;
        }
    }
    public static void AttachCartBadge(Shell shell, CartService cart)
    {
        void Update() => shell.Dispatcher.Dispatch(() =>
        {
            try
            {
                var nav = FindNavigation(Platform.CurrentActivity?.Window?.DecorView);
                if (nav is null || nav.Menu.Size() < 2) return;
                var item = nav.Menu.GetItem(1);
                if (item is null) return;
                var badge = nav.GetOrCreateBadge(item.ItemId);
                badge.BackgroundColor = Android.Graphics.Color.ParseColor("#B91C1C");
                badge.BadgeTextColor = Android.Graphics.Color.White;
                badge.Number = cart.Count;
                badge.SetVisible(cart.Count > 0, false);
            }
            catch (Exception ex) { Diagnostics.AppDiagnostics.Log(ex); }
        });
        cart.Changed += Update;
        shell.Loaded += (_, _) => Update();
        shell.Navigated += (_, _) => Update();
    }
    private static BottomNavigationView? FindNavigation(Android.Views.View? view)
    {
        if (view is BottomNavigationView navigation) return navigation;
        if (view is ViewGroup group)
            for (var i = 0; i < group.ChildCount; i++)
                if (FindNavigation(group.GetChildAt(i)) is { } child) return child;
        return null;
    }
}
