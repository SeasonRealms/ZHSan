using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Platforms;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Window = Microsoft.UI.Xaml.Window;
using Thickness = Microsoft.UI.Xaml.Thickness;

namespace Zhsan.GameManager;

internal static class NativeTextEditor
{
    public static Task<string?> Edit(string title, string description, string text, bool password)
    {
        if (!password)
            return global::Season.Basic.DeviceServices.Dialog.ShowKeyboard(title, description, ["OK", "Cancel"], text);

        // The engine's RichEditBox dialog does not mask passwords. Keep this small
        // native password window application-owned instead of exposing credentials.
        var result = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!WinUI.App.Dispatcher.TryEnqueue(() =>
        {
            Window? window = null;
            IntPtr owner = IntPtr.Zero;
            bool disabled = false;
            try
            {
                owner = PlatformBase.Game1.WindowHandle;
                window = new Window { Title = title };
                var editor = new PasswordBox { Password = text ?? string.Empty };
                var accept = new Button { Content = "OK" };
                var cancel = new Button { Content = "Cancel" };
                var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                buttons.Children.Add(accept);
                buttons.Children.Add(cancel);
                var content = new StackPanel { Padding = new Thickness(20), Spacing = 12 };
                content.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap });
                content.Children.Add(editor);
                content.Children.Add(buttons);
                window.Content = content;
                var dialogWindow = window;
                window.Closed += (_, _) =>
                {
                    if (disabled) EnableWindow(owner, true);
                    result.TrySetResult(null);
                };
                accept.Click += (_, _) => { result.TrySetResult(editor.Password); dialogWindow.Close(); };
                cancel.Click += (_, _) => dialogWindow.Close();
                var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle));
                appWindow.Resize(new Windows.Graphics.SizeInt32(480, 250));
                if (owner != IntPtr.Zero)
                {
                    EnableWindow(owner, false);
                    disabled = true;
                }
                window.Activate();
                editor.Focus(FocusState.Programmatic);
            }
            catch (Exception error)
            {
                if (disabled) EnableWindow(owner, true);
                result.TrySetException(error);
                window?.Close();
            }
        }))
            result.TrySetException(new InvalidOperationException("The UI dispatcher is shutting down."));
        return result.Task;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool enable);
}
