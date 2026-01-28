using UIKit;
using Foundation;
using WebKit;
using Microsoft.Maui.ApplicationModel;
using TheDigitalJournal.Services;

namespace TheDigitalJournal.Platforms.MacCatalyst;

public class PrintService : IPrintService
{
    public void PrintHtml(string html)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var printInfo = UIPrintInfo.PrintInfo;
            printInfo.OutputType = UIPrintInfoOutputType.General;
            printInfo.JobName = "Journal Export";

            var formatter = new UIMarkupTextPrintFormatter(html)
            {
                PerPageContentInsets = new UIEdgeInsets(20, 20, 20, 20)
            };

            var controller = UIPrintInteractionController.SharedPrintController;
            controller.PrintInfo = printInfo;
            controller.PrintFormatter = formatter;

            controller.Present(true, (handler, completed, error) =>
            {
                if (!completed && error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Printing error: {error.LocalizedDescription}");
                }
            });
        });
    }
}