using System;
using System.Windows;
using ChatbotGomarco.ViewModelos;

namespace ChatbotGomarco.Vistas
{
    public partial class VentanaConfiguracion : Window
    {
        public VentanaConfiguracion()
        {
            InitializeComponent();
            
            try 
            {
                var app = (App)Application.Current;
                if (app != null)
                {
                    DataContext = app.ObtenerServicio<ViewModeloVentanaConfiguracion>(this);
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
            MessageBox.Show($"Error de inicialización:\n{mensaje}\n\nLa ventana de configuración se cerrará.", 
                "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Centrar la ventana respecto a la ventana principal
            if (Application.Current.MainWindow != null)
            {
                var mainWindow = Application.Current.MainWindow;
                Left = mainWindow.Left + (mainWindow.Width - Width) / 2;
                Top = mainWindow.Top + (mainWindow.Height - Height) / 2;
            }
        }
    }
} 