using System.Windows;
using ChatbotGomarco.ViewModelos;

namespace ChatbotGomarco.Vistas
{
    public partial class VentanaPrincipal : Window
    {
        public VentanaPrincipal()
        {
            InitializeComponent();
            
            // Obtener el ViewModel desde el contenedor de servicios
            var app = (App)Application.Current;
            DataContext = app.ObtenerServicio<ViewModeloVentanaPrincipal>();
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