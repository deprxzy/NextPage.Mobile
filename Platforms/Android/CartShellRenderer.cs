using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace NextPage.Mobile.AndroidUi;

public sealed class CartShellRenderer : ShellRenderer
{
    protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem) => new CartTabRenderer(this);
}

public sealed class CartTabRenderer(IShellContext context) : ShellItemRenderer(context)
{
    protected override void OnTabReselected(ShellSection shellSection)
    {
        if (shellSection.Route != "carrinho" || ShellContext.Shell is not AppShell shell)
        {
            base.OnTabReselected(shellSection);
            return;
        }
        shell.Dispatcher.Dispatch(async () =>
        {
            try { await shell.ResetCartAsync(); }
            catch (Exception ex) { Diagnostics.AppDiagnostics.Log(ex); }
        });
    }
}
