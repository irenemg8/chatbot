using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using ChatbotGomarco.Servicios;
using ChatbotGomarco.Datos;
using ChatbotGomarco.ViewModelos;

namespace ChatbotGomarco
{
    public partial class App : Application
    {
        private ServiceProvider? _proveedorServicios;

        protected override void OnStartup(StartupEventArgs argumentosInicio)
        {
            base.OnStartup(argumentosInicio);
            
            ConfigurarServicios();
            InicializarBaseDatos();
        }

        private void ConfigurarServicios()
        {
            var servicios = new ServiceCollection();

            // Logging
            servicios.AddLogging(constructor => 
                constructor.SetMinimumLevel(LogLevel.Information));

            // Servicios de datos
            servicios.AddSingleton<ContextoBaseDatos>();
            servicios.AddScoped<IServicioCifrado, ServicioCifrado>();
            servicios.AddScoped<IServicioArchivos, ServicioArchivos>();
            servicios.AddScoped<IServicioHistorialChats, ServicioHistorialChats>();
            servicios.AddScoped<IServicioChatbot, ServicioChatbot>();
            servicios.AddScoped<IServicioExtraccionContenido>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ServicioExtraccionContenido>>();
                var servicioIA = provider.GetService<IServicioIA>();
                return new ServicioExtraccionContenido(logger, servicioIA);
            });
            
            // Configurar HttpClient para OpenAI
            servicios.AddHttpClient();
            servicios.AddSingleton<IServicioIA, ServicioIAOpenAI>();

            // Servicios LLM modulares
            servicios.AddSingleton<ChatbotGomarco.Servicios.LLM.IAnalizadorConversacion, ChatbotGomarco.Servicios.LLM.AnalizadorConversacion>();
            servicios.AddSingleton<ChatbotGomarco.Servicios.LLM.IGeneradorRespuestas>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ChatbotGomarco.Servicios.LLM.GeneradorRespuestas>>();
                var servicioIA = provider.GetService<IServicioIA>();
                return new ChatbotGomarco.Servicios.LLM.GeneradorRespuestas(logger, servicioIA);
            });

            // ViewModels
            servicios.AddTransient<ViewModeloVentanaPrincipal>();

            _proveedorServicios = servicios.BuildServiceProvider();
        }

        private void InicializarBaseDatos()
        {
            using var alcance = _proveedorServicios!.CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoBaseDatos>();
            contexto.InicializarBaseDatos();
        }

        public T ObtenerServicio<T>() where T : class
        {
            return _proveedorServicios!.GetRequiredService<T>();
        }

        protected override void OnExit(ExitEventArgs argumentosSalida)
        {
            _proveedorServicios?.Dispose();
            base.OnExit(argumentosSalida);
        }
    }
} 