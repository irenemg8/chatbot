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
            servicios.AddScoped<IServicioExtraccionContenido, ServicioExtraccionContenido>();
            servicios.AddSingleton<IServicioIA, ServicioIA>();

            // Servicios LLM modulares
            servicios.AddSingleton<ChatbotGomarco.Servicios.LLM.IAnalizadorConversacion, ChatbotGomarco.Servicios.LLM.AnalizadorConversacion>();
            servicios.AddSingleton<ChatbotGomarco.Servicios.LLM.IGeneradorRespuestas, ChatbotGomarco.Servicios.LLM.GeneradorRespuestas>();

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