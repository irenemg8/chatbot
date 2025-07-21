using System;
using System.Windows;
using ChatbotGomarco.ViewModelos;

namespace ChatbotGomarco.Vistas
{
    public partial class VentanaPrincipal : Window
    {
        public VentanaPrincipal()
        {
            InitializeComponent();
            
            // Versión temporal simplificada - manejo de errores mejorado
            try 
            {
                var app = (App)Application.Current;
                if (app != null)
                {
                    DataContext = app.ObtenerServicio<ViewModeloVentanaPrincipal>();
                }
                else
                {
                    MostrarErrorInicializacion("La aplicación no se inicializó correctamente.");
                }
            }
            catch (Exception ex)
            {
                MostrarErrorInicializacion($"Error al cargar servicios: {ex.Message}");
            }
        }
        
        private void MostrarErrorInicializacion(string mensaje)
        {
            MessageBox.Show($"Error de inicialización:\n{mensaje}\n\nLa aplicación se cerrará.", 
                "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }

        private void ScrollViewerMensajes_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Auto-scroll hacia abajo cuando se agregan nuevos mensajes
            if (ScrollViewerMensajes != null)
            {
                ScrollViewerMensajes.ScrollToEnd();
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            // Limpiar archivos temporales al cerrar la aplicación
            try
            {
                var app = (App)Application.Current;
                var servicioArchivos = app.ObtenerServicio<Servicios.IServicioArchivos>();
                _ = servicioArchivos.LimpiarArchivosTemporalesAsync();
            }
            catch
            {
                // Ignorar errores de limpieza al cerrar
            }

            base.OnClosed(e);
        }
    }
} 