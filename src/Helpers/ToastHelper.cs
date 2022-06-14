using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;

namespace StreamManager.Helpers
{
    public class ToastHelper
    {
        public static void Toast(string title, string message, bool isError = true)
        {
            new ToastContentBuilder()
                .AddText(title, hintMaxLines: 1)
                .AddText(message)
                .AddAppLogoOverride(
                    new Uri(Path.GetFullPath(isError ? @"Assets\error.png" : @"Assets\info.png")),
                    ToastGenericAppLogoCrop.None
                )
                .Show();
        }
    }
}