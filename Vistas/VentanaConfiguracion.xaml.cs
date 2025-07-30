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
                
                // Seleccionar modelo correcto en el ComboBox
                if (proveedorActivo == "ollama")
                {
                    ComboModelos.SelectedItem = ItemOllama;
                }
                else if (proveedorActivo == "deepseek")
                {
                    ComboModelos.SelectedItem = ItemDeepSeek;
                }
                else if (proveedorActivo == "claude")
                {
                    ComboModelos.SelectedItem = ItemClaude;
                }
                else
                {
                    ComboModelos.SelectedItem = ItemOpenAI;
                }
                
                // Verificar estado de todos los proveedores
                await VerificarEstadoProveedoresAsync();
                
                // Actualizar título dinámico
                ActualizarTituloVentana();
                
                // Configurar proveedores futuros
                ConfigurarProveedoresFuturos();
                
                // Inicializar indicador del modelo activo
                ActualizarIndicadorModeloActivo();
                
                TextBoxClaveAPI.Focus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inicializando ventana: {ex.Message}");
                // Continuar con inicialización básica
                ComboModelos.SelectedItem = ItemOpenAI;
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
                // Si hay una clave existente Y el TextBox está ReadOnly, usar la clave guardada
                if (!string.IsNullOrEmpty(TextBoxClaveAPI.Tag?.ToString()) && TextBoxClaveAPI.IsReadOnly)
                {
                    ClaveAPI = TextBoxClaveAPI.Tag.ToString();
                    await CambiarProveedorActivoAsync("openai");
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                    return;
                }
                
                // Si el TextBox no está ReadOnly, significa que el usuario quiere cambiar la clave
                if (!TextBoxClaveAPI.IsReadOnly)
                {
                    // Limpiar Tag para usar la nueva clave del TextBox
                    TextBoxClaveAPI.Tag = null;
                }

                string claveIngresada = TextBoxClaveAPI.Text?.Trim() ?? string.Empty;

                // DEBUG: Verificar qué se está recibiendo exactamente
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Clave recibida: '{claveIngresada}'");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Longitud: {claveIngresada.Length}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Primeros 10 chars: '{(claveIngresada.Length >= 10 ? claveIngresada.Substring(0, 10) : claveIngresada)}'");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Comienza con sk-: {claveIngresada.StartsWith("sk-")}");

                // Validaciones básicas para OpenAI
                if (string.IsNullOrWhiteSpace(claveIngresada))
                {
                    MessageBox.Show("Por favor, ingresa tu clave API de OpenAI.\n\nPuedes obtenerla en: https://platform.openai.com/api-keys",
                        "Clave requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TextBoxClaveAPI.Focus();
                    return;
                }

                // VALIDACIÓN TEMPORAL DESHABILITADA PARA DEBUG
                // Verificar directamente si contiene "sk-" en cualquier posición
                if (!claveIngresada.Contains("sk-"))
                {
                    MessageBox.Show($"DEBUG: No contiene 'sk-' en ninguna posición.\n\nCadena recibida: '{claveIngresada}'\nLongitud: {claveIngresada.Length}\n\nPor favor reporta este error.",
                        "DEBUG - Error de validación", MessageBoxButton.OK, MessageBoxImage.Error);
                    TextBoxClaveAPI.Focus();
                    TextBoxClaveAPI.SelectAll();
                    return;
                }
                
                // Si contiene "sk-" pero no empieza con "sk-", hay un problema de caracteres invisibles
                if (!claveIngresada.StartsWith("sk-"))
                {
                    MessageBox.Show($"DEBUG: Contiene 'sk-' pero no empieza con 'sk-'.\n\nPrimeros 10 caracteres: '{(claveIngresada.Length >= 10 ? claveIngresada.Substring(0, 10) : claveIngresada)}'\n\nPosiblemente hay caracteres invisibles al inicio.",
                        "DEBUG - Caracteres invisibles detectados", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // Intentar limpiar caracteres invisibles
                    var claveLimpia = claveIngresada.Trim().Replace("\u200B", "").Replace("\u00A0", " ").Trim();
                    
                    if (claveLimpia.StartsWith("sk-"))
                    {
                        // Usar la clave limpia
                        claveIngresada = claveLimpia;
                        TextBoxClaveAPI.Text = claveLimpia;
                        MessageBox.Show("✅ Caracteres invisibles eliminados. La clave ahora es válida.", "Problema resuelto", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        TextBoxClaveAPI.Focus();
                        TextBoxClaveAPI.SelectAll();
                        return;
                    }
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
                    // Actualizar estado de OpenAI en el panel correspondiente
                    if (estado.EstaDisponible)
                    {
                        IconoEstado.Kind = PackIconKind.CheckCircle;
                        IconoEstado.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        TextoEstado.Text = "OpenAI configurado y funcionando";
                        TextoEstado.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    }
                    else
                    {
                        IconoEstado.Kind = PackIconKind.AlertCircle;
                        IconoEstado.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                        TextoEstado.Text = estado.MensajeEstado;
                        TextoEstado.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    }
                }
                else if (idProveedor == "ollama")
                {
                    // Actualizar estado de Ollama en el panel correspondiente
                    if (estado.EstaDisponible)
                    {
                        IconoEstadoOllama.Kind = PackIconKind.CheckCircle;
                        IconoEstadoOllama.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        TextoEstadoOllama.Text = estado.MensajeEstado;
                        TextoEstadoOllama.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    }
                    else
                    {
                        IconoEstadoOllama.Kind = PackIconKind.AlertCircle;
                        IconoEstadoOllama.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        TextoEstadoOllama.Text = estado.MensajeEstado;
                        TextoEstadoOllama.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
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
                    
                    // Mostrar información del modelo detectado
                    ActualizarInfoModeloDetectado(estado);
                }
                else
                {
                    // Ollama no está disponible - ocultar información del modelo
                    PanelModeloInfo.Visibility = Visibility.Collapsed;
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
        /// Maneja el cambio en el ComboBox de selección de modelos
        /// </summary>
        private async void ComboModelos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var itemSeleccionado = ComboModelos.SelectedItem as ComboBoxItem;
                if (itemSeleccionado?.Tag != null)
                {
                    ProveedorSeleccionado = itemSeleccionado.Tag.ToString();
                    
                    // Ocultar todos los paneles de configuración
                    PanelConfigOpenAI.Visibility = Visibility.Collapsed;
                    PanelConfigOllama.Visibility = Visibility.Collapsed;
                    PanelConfigDeepSeek.Visibility = Visibility.Collapsed;
                    PanelConfigClaude.Visibility = Visibility.Collapsed;
                    
                    // Mostrar el panel correspondiente al modelo seleccionado
                    switch (ProveedorSeleccionado?.ToLower())
                    {
                        case "openai":
                            PanelConfigOpenAI.Visibility = Visibility.Visible;
                            break;
                        case "ollama":
                            PanelConfigOllama.Visibility = Visibility.Visible;
                            break;
                        case "deepseek":
                            PanelConfigDeepSeek.Visibility = Visibility.Visible;
                            break;
                        case "claude":
                            PanelConfigClaude.Visibility = Visibility.Visible;
                            break;
                    }
                    
                    // Actualizar texto del botón según el proveedor
                    ActualizarTextoBotonGuardar();
                    
                    // Actualizar indicador del modelo activo
                    ActualizarIndicadorModeloActivo();
                    
                    // Actualizar título dinámico
                    ActualizarTituloVentana();
                    
                    Debug.WriteLine($"Modelo seleccionado: {ProveedorSeleccionado}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cambiando modelo: {ex.Message}");
            }
        }





        /// <summary>
        /// Actualiza el texto del botón Guardar según el proveedor seleccionado con información específica del modelo
        /// </summary>
        private void ActualizarTextoBotonGuardar()
        {
            try
            {
                switch (ProveedorSeleccionado?.ToLower())
                {
                    case "ollama":
                        // Determinar modelo específico de Ollama activo
                        var modeloOllama = ObtenerModeloOllamaActivo();
                        TextoBotonGuardar.Text = $"Activar {modeloOllama}";
                        break;
                        
                    case "deepseek":
                        // Mostrar información específica de DeepSeek
                        var modeloDeepSeek = ObtenerModeloDeepSeekSeleccionado();
                        TextoBotonGuardar.Text = $"Activar {modeloDeepSeek}";
                        break;
                        
                    case "claude":
                        // Mostrar información específica de Claude
                        var modeloClaude = ObtenerModeloClaudeSeleccionado();
                        TextoBotonGuardar.Text = $"Activar {modeloClaude}";
                        break;
                        
                    default:
                        TextoBotonGuardar.Text = "Activar OpenAI GPT-4";
                        break;
                }
                
                // Actualizar también el indicador del modelo activo
                ActualizarIndicadorModeloActivo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando texto botón: {ex.Message}");
                TextoBotonGuardar.Text = "Activar IA";
            }
        }
        
        /// <summary>
        /// Obtiene el nombre del modelo Ollama activo o seleccionado
        /// </summary>
        private string ObtenerModeloOllamaActivo()
        {
            try
            {
                // Lógica para detectar qué modelo específico de Ollama está seleccionado
                // basándose en el contenido de la pestaña actual
                if (PanelModeloInfo?.Visibility == Visibility.Visible && 
                    TextModeloDetectado?.Text?.Contains("DeepSeek-R1") == true)
                {
                    return "DeepSeek-R1 7B";
                }
                else if (TextModeloDetectado?.Text?.Contains("DeepSeek-V3") == true)
                {
                    return "DeepSeek-V3";
                }
                else if (TextModeloDetectado?.Text?.Contains("Phi") == true)
                {
                    return "Phi-4-Mini";
                }
                else
                {
                    return "Ollama Local";
                }
            }
            catch
            {
                return "Ollama Local";
            }
        }
        
        /// <summary>
        /// Obtiene el modelo DeepSeek seleccionado según la interfaz
        /// </summary>
        private string ObtenerModeloDeepSeekSeleccionado()
        {
            try
            {
                // Por defecto, usar DeepSeek-R1 como modelo principal
                return "DeepSeek-R1 7B";
            }
            catch
            {
                return "DeepSeek";
            }
        }
        
        /// <summary>
        /// Obtiene el modelo Claude seleccionado según la interfaz
        /// </summary>
        private string ObtenerModeloClaudeSeleccionado()
        {
            try
            {
                // Por defecto, usar Claude-style Llama como modelo principal
                return "Claude-Style Llama";
            }
            catch
            {
                return "Claude";
            }
        }
        
        /// <summary>
        /// Actualiza el indicador visual del modelo actualmente cargado
        /// </summary>
        private void ActualizarIndicadorModeloActivo()
        {
            try
            {
                // Usar el proveedor seleccionado actual en la interfaz
                var proveedorActivo = ProveedorSeleccionado ?? "openai";
                
                var nombreModelo = proveedorActivo.ToLower() switch
                {
                    "openai" => "OpenAI GPT-4 Turbo",
                    "ollama" => DeterminarModeloOllamaEspecifico(),
                    "deepseek" => "DeepSeek-R1 7B (Razonamiento)",
                    "claude" => "Claude-Style Llama (Conversacional)",
                    _ => "Modelo Desconocido"
                };
                
                TextoModeloActivo.Text = nombreModelo;
                IconoModeloActivo.Visibility = Visibility.Visible;
                
                // Cambiar color según el tipo de modelo
                var color = proveedorActivo.ToLower() switch
                {
                    "openai" => "#059669", // Verde para APIs externas
                    "ollama" or "deepseek" or "claude" => "#0284C7", // Azul para modelos locales
                    _ => "#6B7280"
                };
                
                TextoModeloActivo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                
                Debug.WriteLine($"Indicador actualizado: {nombreModelo} para proveedor: {proveedorActivo}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando indicador modelo activo: {ex.Message}");
                TextoModeloActivo.Text = "Error detectando modelo";
                IconoModeloActivo.Visibility = Visibility.Collapsed;
            }
        }
        
        /// <summary>
        /// Determina el modelo específico de Ollama según el estado de la interfaz
        /// </summary>
        private string DeterminarModeloOllamaEspecifico()
        {
            try
            {
                // Verificar si hay información del modelo detectado en la interfaz
                if (TextModeloDetectado?.Text?.Contains("DeepSeek-R1") == true)
                {
                    return "DeepSeek-R1 7B (Local)";
                }
                else if (TextModeloDetectado?.Text?.Contains("DeepSeek-V3") == true)
                {
                    return "DeepSeek-V3 (Local)";
                }
                else if (TextModeloDetectado?.Text?.Contains("Phi") == true)
                {
                    return "Phi-4-Mini (Microsoft)";
                }
                else
                {
                    return "Ollama Local";
                }
            }
            catch
            {
                return "Ollama Local";
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
        /// Configura la interfaz para todos los proveedores AI enterprise
        /// </summary>
        private void ConfigurarProveedoresFuturos()
        {
            try
            {
                // ✅ TODOS LOS PROVEEDORES ENTERPRISE ESTÁN IMPLEMENTADOS EN EL COMBOBOX
                // - OpenAI GPT-4 Turbo (API Externa)
                // - Ollama con Phi-4-Mini y DeepSeek-R1 (Local)  
                // - DeepSeek-R1 7B (Razonamiento avanzado)
                // - Claude-Style Llama (Conversacional)
                
                Debug.WriteLine("✅ Interfaz ComboBox configurada para todos los proveedores AI enterprise");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error configurando proveedores enterprise: {ex.Message}");
            }
        }
    }
} 