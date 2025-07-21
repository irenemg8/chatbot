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
        private ServiceProvider? _proveeedorServicios;

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

            // ViewModels
            servicios.AddTransient<ViewModeloVentanaPrincipal>();

            _proveeedorServicios = servicios.BuildServiceProvider();
        }

        private void InicializarBaseDatos()
        {
            using var alcance = _proveeedorServicios!.CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoBaseDatos>();
            contexto.InicializarBaseDatos();
        }

        public T ObtenerServicio<T>() where T : class
        {
            return _proveeedorServicios!.GetRequiredService<T>();
        }

        protected override void OnExit(ExitEventArgs argumentosSalida)
        {
            _proveeedorServicios?.Dispose();
            base.OnExit(argumentosSalida);
        }
    }
} 