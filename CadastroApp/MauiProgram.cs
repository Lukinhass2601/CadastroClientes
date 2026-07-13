using CadastroApp.Services;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
using SQLitePCL;

namespace CadastroApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            Batteries_V2.Init();
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddSingleton<PessoaService>();
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
            
#endif
            builder.Services.AddSingleton<PessoaService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<ViaCepService>();

            QuestPDF.Settings.License = LicenseType.Community;

            return builder.Build();
        }
    }
}
