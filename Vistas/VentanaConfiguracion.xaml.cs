using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChatbotGomarco.Modelos;
using ChatbotGomarco.Servicios;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace ChatbotGomarco.Vistas
{
    /// <summary>
    /// Ventana de configuración multi-proveedor para el chatbot GOMARCO
    /// Soporta OpenAI, Ollama y futuros proveedores de IA
    /// </summary>
    public partial class VentanaConfiguracion : Window
    {
        /// <summary>
        /// Indica si se guardó la configuración exitosamente
        /// </summary>
        public bool ConfiguracionGuardada { get; private set; } = false;

        /// <summary>
        /// Clave API configurada por el usuario (para OpenAI)
        /// </summary>
        public string ClaveAPI { get; private set; } = string.Empty;

        /// <summary>
        /// Indica si el usuario eliminó la configuración
        /// </summary>
        public bool ClaveEliminada { get; private set; } = false;

        /// <summary>
        /// Proveedor de IA seleccionado por el usuario
        /// </summary>
        public string ProveedorSeleccionado { get; private set; } = "openai";

        // Servicios enterprise
        private readonly IFactoryProveedorIA _factoryProveedorIA;
        private readonly IServiceProvider _serviceProvider;

        public VentanaConfiguracion()
        {
            InitializeComponent();
            
            // Obtener servicios del contenedor DI
            _serviceProvider = ((App)Application.Current).ObtenerProveedorServicios();
            _factoryProveedorIA = _serviceProvider.GetRequiredService<IFactoryProveedorIA>();
            
            InicializarVentana();
        }

        /// <summary>
        /// Constructor con configuración inicial multi-proveedor
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
        /// Inicializa la ventana multi-proveedor
        /// </summary>
        private async void InicializarVentana()
        {
            try
            {
                // Configurar proveedor activo inicial
                var proveedorActivo = _factoryProveedorIA.ObtenerIdProveedorActivo();
                ProveedorSeleccionado = proveedorActivo;
                
                // Seleccionar pestaña correcta
                if (proveedorActivo == "ollama")
                {
                    TabControlProveedores.SelectedItem = TabOllama;
                }
                else
                {
                    TabControlProveedores.SelectedItem = TabOpenAI;
                }
                
                // Verificar estado de todos los proveedores
                await VerificarEstadoProveedoresAsync();
                
                // Actualizar título dinámico
                ActualizarTituloVentana();
                
                // Configurar proveedores futuros
                ConfigurarProveedoresFuturos();
                
                TextBoxClaveAPI.Focus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inicializando ventana: {ex.Message}");
                // Continuar con inicialización básica
                TabControlProveedores.SelectedItem = TabOpenAI;
                TextBoxClaveAPI.Focus();
            }
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
        /// Maneja el click del botón Guardar para múltiples proveedores
        /// </summary>
        private async void BotonGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProveedorSeleccionado == "openai")
                {
                    await GuardarConfiguracionOpenAIAsync();
                }
                else if (ProveedorSeleccionado == "ollama")
                {
                    await GuardarConfiguracionOllamaAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar la configuración:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Guarda la configuración de OpenAI
        /// </summary>
        private async Task GuardarConfiguracionOpenAIAsync()
        {
            try
            {
                // Si hay una clave existente, usarla
                if (!string.IsNullOrEmpty(TextBoxClaveAPI.Tag?.ToString()))
                {
                    ClaveAPI = TextBoxClaveAPI.Tag.ToString();
                    await CambiarProveedorActivoAsync("openai");
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                    return;
                }

                string claveIngresada = TextBoxClaveAPI.Text?.Trim() ?? string.Empty;

                // Validaciones básicas para OpenAI
                if (string.IsNullOrWhiteSpace(claveIngresada))
                {
                    MessageBox.Show("Por favor, ingresa tu clave API de OpenAI.\n\nPuedes obtenerla en: https://platform.openai.com/api-keys",
                        "Clave requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TextBoxClaveAPI.Focus();
                    return;
                }

                // Validación MÍNIMA - Solo sk- y longitud básica
                if (!claveIngresada.StartsWith("sk-") || claveIngresada.Length < 10)
                {
                    MessageBox.Show("La clave API debe comenzar con 'sk-' y tener al menos 10 caracteres.\n\nTodos los formatos de OpenAI son válidos:\n• sk-proj-... (nuevo formato)\n• sk-... (formato clásico)\n• sk-org-... (organizaciones)",
                        "Formato inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TextBoxClaveAPI.Focus();
                    TextBoxClaveAPI.SelectAll();
                    return;
                }

                // SIN MÁS VALIDACIONES - Dejamos que OpenAI valide

                // Confirmación
                var resultado = MessageBox.Show("¿Deseas activar OpenAI GPT-4?\n\n" +
                    "• Se configurará como proveedor de IA activo\n" +
                    "• Requiere conexión a internet\n" +
                    "• La clave se guardará de forma segura\n" +
                    "• Procesamiento en la nube con máxima calidad",
                    "Activar OpenAI GPT-4", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    ClaveAPI = claveIngresada;
                    await CambiarProveedorActivoAsync("openai");
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error configurando OpenAI:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Guarda la configuración de Ollama
        /// </summary>
        private async Task GuardarConfiguracionOllamaAsync()
        {
            try
            {
                // Verificar que Ollama esté disponible
                var estadoOllama = await _factoryProveedorIA.ObtenerEstadoTodosProveedoresAsync();
                
                if (!estadoOllama.TryGetValue("ollama", out var estado) || !estado.EstaDisponible)
                {
                    MessageBox.Show("Ollama no está disponible.\n\n" +
                        "• Verifica que Ollama esté instalado\n" +
                        "• Asegúrate de que al menos un modelo esté descargado\n" +
                        "• Usa los botones de instalación si es necesario",
                        "Ollama no disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirmación
                var resultado = MessageBox.Show("¿Deseas activar Ollama como proveedor de IA?\n\n" +
                    "• Procesamiento 100% local y offline\n" +
                    "• Zero data leakage - datos nunca salen de tu PC\n" +
                    "• Ideal para información sensible\n" +
                    "• No requiere API Key ni conexión a internet\n" +
                    "• Modelo: Phi-3-Mini (Microsoft)",
                    "Activar Ollama Local", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    await CambiarProveedorActivoAsync("ollama");
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error configurando Ollama:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cambia el proveedor activo en el sistema
        /// </summary>
        private async Task CambiarProveedorActivoAsync(string idProveedor)
        {
            try
            {
                await _factoryProveedorIA.CambiarProveedorActivoAsync(idProveedor);
                ProveedorSeleccionado = idProveedor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cambiando proveedor activo: {ex.Message}");
                throw;
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

        // ====================================================================
        // MÉTODOS PARA SISTEMA MULTI-PROVEEDOR
        // ====================================================================

        /// <summary>
        /// Verifica el estado de todos los proveedores disponibles
        /// </summary>
        private async Task VerificarEstadoProveedoresAsync()
        {
            try
            {
                var estados = await _factoryProveedorIA.ObtenerEstadoTodosProveedoresAsync();
                
                // Actualizar estado de OpenAI
                if (estados.TryGetValue("openai", out var estadoOpenAI))
                {
                    ActualizarEstadoProveedor("openai", estadoOpenAI);
                }
                
                // Actualizar estado de Ollama
                if (estados.TryGetValue("ollama", out var estadoOllama))
                {
                    ActualizarEstadoProveedor("ollama", estadoOllama);
                    await ConfigurarInterfazOllamaAsync(estadoOllama);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error verificando estado de proveedores: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza el estado visual de un proveedor específico
        /// </summary>
        private void ActualizarEstadoProveedor(string idProveedor, EstadoProveedorIA estado)
        {
            try
            {
                if (idProveedor == "openai")
                {
                    // Actualizar estado de OpenAI
                    if (estado.EstaDisponible)
                    {
                        IconoEstado.Kind = PackIconKind.CheckCircle;
                        IconoEstado.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        TextoEstado.Text = "OpenAI configurado y funcionando";
                        TextoEstado.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        StatusOpenAI.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    }
                    else
                    {
                        IconoEstado.Kind = PackIconKind.AlertCircle;
                        IconoEstado.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                        TextoEstado.Text = estado.MensajeEstado;
                        TextoEstado.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                        StatusOpenAI.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    }
                }
                else if (idProveedor == "ollama")
                {
                    // Actualizar estado de Ollama
                    if (estado.EstaDisponible)
                    {
                        IconoEstadoOllama.Kind = PackIconKind.CheckCircle;
                        IconoEstadoOllama.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        TextoEstadoOllama.Text = estado.MensajeEstado;
                        TextoEstadoOllama.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        StatusOllama.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    }
                    else
                    {
                        IconoEstadoOllama.Kind = PackIconKind.AlertCircle;
                        IconoEstadoOllama.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        TextoEstadoOllama.Text = estado.MensajeEstado;
                        TextoEstadoOllama.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        StatusOllama.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    }
                }
                
                // Actualizar título dinámico cuando cambie el estado
                ActualizarTituloVentana();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando estado de {idProveedor}: {ex.Message}");
            }
        }

        /// <summary>
        /// Configura la interfaz específica para Ollama según su estado
        /// </summary>
        private async Task ConfigurarInterfazOllamaAsync(EstadoProveedorIA estado)
        {
            try
            {
                if (estado.EstaDisponible)
                {
                    // Ollama está disponible - mostrar información del modelo
                    PanelModeloInfo.Visibility = Visibility.Visible;
                    PanelAccionesOllama.Visibility = Visibility.Collapsed;
                    
                    // Mostrar información del modelo detectado
                    ActualizarInfoModeloDetectado(estado);
                }
                else if (estado.RequiereInstalacion)
                {
                    // Ollama no está instalado - mostrar botón de instalación
                    PanelModeloInfo.Visibility = Visibility.Collapsed;
                    PanelAccionesOllama.Visibility = Visibility.Visible;
                    BotonInstalarOllama.Visibility = Visibility.Visible;
                    BotonDescargarModelo.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Ollama instalado pero sin modelos - mostrar botón de descarga
                    PanelModeloInfo.Visibility = Visibility.Collapsed;
                    PanelAccionesOllama.Visibility = Visibility.Visible;
                    BotonInstalarOllama.Visibility = Visibility.Collapsed;
                    BotonDescargarModelo.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error configurando interfaz Ollama: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza la información del modelo detectado automáticamente
        /// </summary>
        private void ActualizarInfoModeloDetectado(EstadoProveedorIA estado)
        {
            try
            {
                var modeloDetectado = estado.ModeloCargado;
                if (!string.IsNullOrEmpty(modeloDetectado))
                {
                    // Personalizar el texto según el modelo detectado
                    if (modeloDetectado.Contains("phi3"))
                    {
                        TextModeloDetectado.Text = "Phi-3-Mini (Microsoft) ✓";
                    }
                    else if (modeloDetectado.Contains("phi4"))
                    {
                        TextModeloDetectado.Text = "Phi-4-Mini (Microsoft) ✓";
                    }
                    else
                    {
                        TextModeloDetectado.Text = $"{modeloDetectado} ✓";
                    }
                }
                else
                {
                    TextModeloDetectado.Text = "Phi-3-Mini (Microsoft) ✓";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando info modelo: {ex.Message}");
                TextModeloDetectado.Text = "Modelo Local Disponible ✓";
            }
        }

        // ====================================================================
        // EVENT HANDLERS PARA SISTEMA MULTI-PROVEEDOR
        // ====================================================================

        /// <summary>
        /// Maneja el cambio de pestaña de proveedor
        /// </summary>
        private async void TabControlProveedores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.Source != TabControlProveedores) return;
                
                var tabSeleccionada = TabControlProveedores.SelectedItem as TabItem;
                if (tabSeleccionada == TabOpenAI)
                {
                    ProveedorSeleccionado = "openai";
                }
                else if (tabSeleccionada == TabOllama)
                {
                    ProveedorSeleccionado = "ollama";
                }
                
                // Actualizar texto del botón según el proveedor
                ActualizarTextoBotonGuardar();
                
                // Actualizar título dinámico
                ActualizarTituloVentana();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cambiando pestaña: {ex.Message}");
            }
        }



        /// <summary>
        /// Maneja el click del botón Instalar Ollama
        /// </summary>
        private async void BotonInstalarOllama_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Deseas instalar Ollama para IA local?\n\n" +
                    "• Se descargará e instalará automáticamente\n" +
                    "• Permite procesamiento 100% offline\n" +
                    "• Ideal para datos sensibles\n\n" +
                    "El proceso puede tardar unos minutos.",
                    "Instalar Ollama", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Ejecutar script de instalación
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-ExecutionPolicy Bypass -File .\\ActualizarYEjecutar.ps1 -SkipGitPull",
                        UseShellExecute = true,
                        Verb = "runas" // Ejecutar como administrador
                    };
                    
                    Process.Start(psi);
                    
                    MessageBox.Show(
                        "Se ha iniciado la instalación de Ollama.\n\n" +
                        "El script se ejecutará en una ventana separada.\n" +
                        "Cierra esta ventana y vuelve a abrirla cuando termine la instalación.",
                        "Instalación en Progreso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error iniciando instalación:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el click del botón Descargar Modelo
        /// </summary>
        private async void BotonDescargarModelo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Deseas descargar el modelo Phi-3-Mini?\n\n" +
                    "• Tamaño: ~2.2GB\n" +
                    "• Modelo Microsoft optimizado\n" +
                    "• Requiere Ollama instalado\n\n" +
                    "La descarga puede tardar varios minutos según tu conexión.",
                    "Descargar Modelo", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Ejecutar comando ollama pull
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/k ollama pull phi3:mini",
                        UseShellExecute = true
                    };
                    
                    Process.Start(psi);
                    
                    MessageBox.Show(
                        "Se ha iniciado la descarga del modelo Phi-3-Mini.\n\n" +
                        "El proceso se ejecutará en una ventana de comando.\n" +
                        "Cierra esta ventana y vuelve a abrirla cuando termine la descarga.",
                        "Descarga en Progreso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error iniciando descarga:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Actualiza el texto del botón Guardar según el proveedor seleccionado
        /// </summary>
        private void ActualizarTextoBotonGuardar()
        {
            try
            {
                if (ProveedorSeleccionado == "ollama")
                {
                    TextoBotonGuardar.Text = "Usar Ollama";
                }
                else if (ProveedorSeleccionado == "deepseek")
                {
                    TextoBotonGuardar.Text = "Activar DeepSeek";
                }
                else if (ProveedorSeleccionado == "claude")
                {
                    TextoBotonGuardar.Text = "Activar Claude";
                }
                else
                {
                    TextoBotonGuardar.Text = "Activar OpenAI";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando texto botón: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza dinámicamente el título de la ventana según el proveedor activo
        /// </summary>
        private void ActualizarTituloVentana()
        {
            try
            {
                var tituloBase = "⚙️ Configuración del Chatbot GOMARCO";
                var proveedorActivo = _factoryProveedorIA?.ObtenerIdProveedorActivo() ?? ProveedorSeleccionado;
                
                var nombreProveedor = proveedorActivo switch
                {
                    "openai" => "OpenAI GPT-4",
                    "ollama" => "Ollama (Local)",
                    "deepseek" => "DeepSeek",
                    "claude" => "Claude",
                    _ => "Multi-Proveedor"
                };
                
                // Obtener estado del proveedor para mostrar si está activo
                var estadoTexto = ObtenerEstadoProveedorParaTitulo(proveedorActivo);
                
                this.Title = $"{tituloBase} - {nombreProveedor}{estadoTexto}";
                
                Debug.WriteLine($"Título actualizado: {this.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando título: {ex.Message}");
                this.Title = "⚙️ Configuración del Chatbot GOMARCO";
            }
        }

        /// <summary>
        /// Obtiene el texto del estado del proveedor para el título
        /// </summary>
        private string ObtenerEstadoProveedorParaTitulo(string idProveedor)
        {
            try
            {
                // Verificar si el proveedor está disponible y configurado
                if (idProveedor == "openai")
                {
                    var estadoOpenAI = IconoEstado?.Kind == PackIconKind.CheckCircle;
                    return estadoOpenAI ? " ✓" : "";
                }
                else if (idProveedor == "ollama")
                {
                    var estadoOllama = IconoEstadoOllama?.Kind == PackIconKind.CheckCircle;
                    return estadoOllama ? " ✓" : "";
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Obtiene el nombre amigable del proveedor para mostrar en la UI
        /// </summary>
        private string ObtenerNombreProveedorAmigable(string idProveedor)
        {
            return idProveedor switch
            {
                "openai" => "OpenAI GPT-4",
                "ollama" => "Ollama (Phi-3-Mini)",
                "deepseek" => "DeepSeek",
                "claude" => "Claude",
                _ => "Proveedor Desconocido"
            };
        }

        /// <summary>
        /// Prepara la interfaz para futuros proveedores
        /// </summary>
        private void ConfigurarProveedoresFuturos()
        {
            try
            {
                // Por ahora, las pestañas de DeepSeek y Claude están ocultas
                // Se pueden mostrar cuando estén implementadas
                TabDeepSeek.Visibility = Visibility.Collapsed;
                TabClaude.Visibility = Visibility.Collapsed;
                
                // En el futuro, esto se puede cambiar a:
                // TabDeepSeek.Visibility = Visibility.Visible;
                // TabClaude.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error configurando proveedores futuros: {ex.Message}");
            }
        }
    }
} 