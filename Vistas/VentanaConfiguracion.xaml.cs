using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace ChatbotGomarco.Vistas
{
    /// <summary>
    /// Ventana de configuración simple para el chatbot GOMARCO
    /// </summary>
    public partial class VentanaConfiguracion : Window
    {
        /// <summary>
        /// Indica si se guardó la configuración exitosamente
        /// </summary>
        public bool ConfiguracionGuardada { get; private set; } = false;

        /// <summary>
        /// Clave API configurada por el usuario
        /// </summary>
        public string ClaveAPI { get; private set; } = string.Empty;

        /// <summary>
        /// Indica si el usuario eliminó la configuración
        /// </summary>
        public bool ClaveEliminada { get; private set; } = false;

        public VentanaConfiguracion()
        {
            InitializeComponent();
            InicializarVentana();
        }

        /// <summary>
        /// Constructor con configuración inicial
        /// </summary>
        public VentanaConfiguracion(string claveActual = "", bool iaConfigurada = false) : this()
        {
            if (!string.IsNullOrEmpty(claveActual))
            {
                TextBoxClaveAPI.Text = OcultarClaveAPI(claveActual);
                TextBoxClaveAPI.Tag = claveActual;
                TextBoxClaveAPI.IsReadOnly = true;
                TextBoxClaveAPI.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                TextoBotonGuardar.Text = "Actualizar IA";
                
                // Agregar botón eliminar
                AgregarBotonEliminar();
            }
            
            if (iaConfigurada)
            {
                ActualizarEstadoConfigurada();
            }
        }

        /// <summary>
        /// Oculta parcialmente la clave API
        /// </summary>
        private string OcultarClaveAPI(string claveCompleta)
        {
            if (string.IsNullOrEmpty(claveCompleta) || claveCompleta.Length < 10)
                return claveCompleta;
            
            return $"{claveCompleta[..7]}***{claveCompleta[^4..]}";
        }

        /// <summary>
        /// Inicializa la ventana
        /// </summary>
        private void InicializarVentana()
        {
            TextBoxClaveAPI.Focus();
        }

        /// <summary>
        /// Actualiza el estado cuando la IA está configurada
        /// </summary>
        private void ActualizarEstadoConfigurada()
        {
            try
            {
                IconoEstado.Kind = PackIconKind.CheckCircle;
                IconoEstado.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                TextoEstado.Text = "IA configurada y funcionando";
                TextoEstado.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            catch (Exception ex)
            {
                // Si hay error actualizando el estado visual, continuamos sin problema
                System.Diagnostics.Debug.WriteLine($"Error actualizando estado: {ex.Message}");
            }
        }

        /// <summary>
        /// Agrega el botón de eliminar
        /// </summary>
        private void AgregarBotonEliminar()
        {
            try
            {
                var panelBotones = BotonGuardar.Parent as System.Windows.Controls.StackPanel;
                if (panelBotones == null) return;

                // Verificar si ya existe
                foreach (var child in panelBotones.Children)
                {
                    if (child is System.Windows.Controls.Button btn && btn.Name == "BotonEliminar")
                        return;
                }

                var botonEliminar = new System.Windows.Controls.Button
                {
                    Name = "BotonEliminar",
                    Content = "Eliminar",
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    FontSize = 12,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                botonEliminar.Click += BotonEliminar_Click;
                
                var indiceGuardar = panelBotones.Children.IndexOf(BotonGuardar);
                panelBotones.Children.Insert(indiceGuardar, botonEliminar);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando botón eliminar: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja el click del botón Guardar
        /// </summary>
        private void BotonGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Si hay una clave existente, usarla
                if (!string.IsNullOrEmpty(TextBoxClaveAPI.Tag?.ToString()))
                {
                    ClaveAPI = TextBoxClaveAPI.Tag.ToString();
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                    return;
                }

                string claveIngresada = TextBoxClaveAPI.Text?.Trim() ?? string.Empty;

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(claveIngresada))
                {
                    MessageBox.Show("Por favor, ingresa tu clave API de OpenAI.\n\nPuedes obtenerla en: https://platform.openai.com/api-keys",
                        "Clave requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TextBoxClaveAPI.Focus();
                    return;
                }

                if (!claveIngresada.StartsWith("sk-") || claveIngresada.Length < 20)
                {
                    MessageBox.Show("La clave API debe comenzar con 'sk-' y tener al menos 20 caracteres.\n\nEjemplo: sk-ABC123def456...",
                        "Formato inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TextBoxClaveAPI.Focus();
                    TextBoxClaveAPI.SelectAll();
                    return;
                }

                // Confirmación
                var resultado = MessageBox.Show("¿Deseas guardar esta configuración?\n\n" +
                    "Se activará la IA avanzada con OpenAI GPT-4\n" +
                    "La clave se guardará de forma segura",
                    "Confirmar configuración", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    ClaveAPI = claveIngresada;
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar la configuración:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el click del botón Eliminar
        /// </summary>
        private void BotonEliminar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var resultado = MessageBox.Show("¿Estás seguro de que quieres eliminar la configuración de IA?\n\n" +
                    "Se eliminará la clave API guardada\n" +
                    "Tendrás que volver a configurarla para usar IA",
                    "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    ClaveEliminada = true;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar la configuración:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el click del botón Cancelar
        /// </summary>
        private void BotonCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 