using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TheDigitalJournal.Data;
using TheDigitalJournal.Services;
using TheDigitalJournal.ViewModels;

namespace TheDigitalJournal;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        
        // Register Services
        builder.Services.AddSingleton<JournalDbContext>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddScoped<ISecurityService, SecurityService>();
        builder.Services.AddScoped<IJournalService, JournalService>();
        builder.Services.AddScoped<IExportService, ExportService>();

        #if MACCATALYST
        builder.Services.AddSingleton<IPrintService, TheDigitalJournal.Platforms.MacCatalyst.PrintService>();
        #elif ANDROID
        builder.Services.AddSingleton<IPrintService, TheDigitalJournal.Platforms.Android.PrintService>();
        #elif IOS
        builder.Services.AddSingleton<IPrintService, TheDigitalJournal.Platforms.iOS.PrintService>();
        #elif WINDOWS
        builder.Services.AddSingleton<IPrintService, TheDigitalJournal.Platforms.Windows.PrintService>();
        #endif

        // Register ViewModels
        builder.Services.AddScoped<JournalEditorViewModel>();
        builder.Services.AddScoped<EntriesViewModel>();
        builder.Services.AddScoped<CalendarViewModel>();
        builder.Services.AddScoped<DashboardViewModel>();
        builder.Services.AddScoped<AnalyticsViewModel>();

        // Set Unhandled Exception Handler
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = (Exception)args.ExceptionObject;
            string message = $"CRITICAL UNHANDLED ERROR: {ex.Message}";
            Debug.WriteLine(message);
            if (ex.StackTrace != null)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        };

        var app = builder.Build();
        
        return app;
    }
}