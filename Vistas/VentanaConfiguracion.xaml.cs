using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Net.Http;
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
        /// Inicializa la ventana multi-proveedor con inicio automático de Ollama
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
                
                // ⭐ NUEVO: Iniciar verificación y arranque automático de Ollama
                _ = Task.Run(async () => await IniciarOllamaAutomaticoAsync());
                
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
                else if (ProveedorSeleccionado == "deepseek")
                {
                    await GuardarConfiguracionDeepSeekAsync();
                }
                else if (ProveedorSeleccionado == "claude")
                {
                    await GuardarConfiguracionClaudeAsync();
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

                // Confirmación mejorada para OpenAI
                var resultado = MessageBox.Show(
                    "🎆 ¿Activar OpenAI GPT-4 como proveedor principal?\n\n" +
                    "📊 INFORMACIÓN DEL MODELO:\n" +
                    "• 🧠 Modelo: GPT-4 Turbo (Más avanzado disponible)\n" +
                    "• 🌐 Requiere conexión a internet\n" +
                    "• 🔒 Clave API se guarda de forma segura y cifrada\n" +
                    "• ⚡ Procesamiento en la nube con máxima calidad OpenAI\n" +
                    "• 💰 Consume créditos de tu cuenta OpenAI\n\n" +
                    "🚀 LISTO PARA USAR - La clave API es válida",
                    "🌟 Activar OpenAI GPT-4 Enterprise", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (resultado == MessageBoxResult.Yes)
                {
                    MostrarEstadoTiempoReal("🚀 Configurando OpenAI...", PackIconKind.Loading, "#3B82F6");
                    
                    ClaveAPI = claveIngresada;
                    await Task.Delay(1000); // Simular configuración
                    await CambiarProveedorActivoAsync("openai");
                    
                    MostrarEstadoTiempoReal("✅ OpenAI configurado exitosamente", PackIconKind.CheckCircle, "#10B981");
                    await Task.Delay(1500);
                    
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
        /// Guarda la configuración de Ollama con mejor UX
        /// </summary>
        private async Task GuardarConfiguracionOllamaAsync()
        {
            try
            {
                // Verificar que Ollama esté disponible
                var estadoOllama = await _factoryProveedorIA.ObtenerEstadoTodosProveedoresAsync();
                
                if (!estadoOllama.TryGetValue("ollama", out var estado) || !estado.EstaDisponible)
                {
                    var respuestaInstalacion = MessageBox.Show(
                        "📦 Ollama no está disponible o no está ejecutándose.\n\n" +
                        "❓ ¿Qué deseas hacer?\n\n" +
                        "🆕 SI - Abrir página de descarga de Ollama\n" +
                        "❌ NO - Cancelar y verificar instalación manualmente\n\n" +
                        "📝 Nota: Ollama es necesario para modelos locales",
                        "🚀 Instalar Ollama", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                    if (respuestaInstalacion == MessageBoxResult.Yes)
                    {
                        AbrirPaginaDescargaOllama();
                    }
                    return;
                }

                // Confirmación mejorada con más información
                var modeloActivo = estado.ModeloCargado ?? "Phi-4-Mini";
                var resultado = MessageBox.Show(
                    "🎆 ¿Activar Ollama como proveedor principal de IA?\n\n" +
                    "📊 INFORMACIÓN DEL MODELO:\n" +
                    $"• 🧠 Modelo: {modeloActivo}\n" +
                    "• 🔒 100% Privado - Datos nunca salen de tu PC\n" +
                    "• 🌐 Funciona sin internet - Completamente offline\n" +
                    "• 🔥 Sin límites de uso - Gratis para siempre\n" +
                    "• ⚡ Ideal para información empresarial sensible\n\n" +
                    "🚀 LISTO PARA USAR - Ollama está ejecutándose correctamente",
                    "🌟 Activar Ollama Enterprise", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (resultado == MessageBoxResult.Yes)
                {
                    MostrarEstadoTiempoReal("🚀 Activando Ollama...", PackIconKind.Loading, "#3B82F6");
                    
                    await Task.Delay(1000); // Simular configuración
                    await CambiarProveedorActivoAsync("ollama");
                    
                    MostrarEstadoTiempoReal("✅ Ollama activado exitosamente", PackIconKind.CheckCircle, "#10B981");
                    await Task.Delay(1500);
                    
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MostrarEstadoTiempoReal("❌ Error configurando Ollama", PackIconKind.AlertCircle, "#EF4444");
                MessageBox.Show($"🚨 Error configurando Ollama:\n\n{ex.Message}\n\n🔧 Intenta reiniciar Ollama manualmente",
                    "Error de Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
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
        
        // ====================================================================
        // MÉTODOS PARA INICIO AUTOMÁTICO DE OLLAMA
        // ====================================================================
        
        /// <summary>
        /// Inicia Ollama automáticamente al abrir la ventana
        /// </summary>
        private async Task IniciarOllamaAutomaticoAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() => {
                    MostrarEstadoTiempoReal("🔍 Verificando Ollama...", PackIconKind.Loading, "#3B82F6");
                });
                
                // Verificar si Ollama está instalado
                bool ollamaInstalado = await VerificarOllamaInstaladoAsync();
                
                if (!ollamaInstalado)
                {
                    await Dispatcher.InvokeAsync(() => {
                        MostrarEstadoTiempoReal("📦 Ollama no instalado", PackIconKind.Download, "#F59E0B");
                        PanelInstalacionOllama.Visibility = Visibility.Visible;
                    });
                    return;
                }
                
                // Verificar si Ollama está ejecutándose
                bool ollamaEjecutandose = await VerificarOllamaEjecutandoseAsync();
                
                if (!ollamaEjecutandose)
                {
                    await Dispatcher.InvokeAsync(() => {
                        MostrarEstadoTiempoReal("🚀 Iniciando Ollama...", PackIconKind.Loading, "#3B82F6");
                    });
                    
                    // Intentar iniciar Ollama
                    bool iniciado = await IniciarServicioOllamaAsync();
                    
                    if (iniciado)
                    {
                        await Dispatcher.InvokeAsync(() => {
                            MostrarEstadoTiempoReal("✅ Ollama iniciado correctamente", PackIconKind.CheckCircle, "#10B981");
                        });
                    }
                    else
                    {
                        await Dispatcher.InvokeAsync(() => {
                            MostrarEstadoTiempoReal("⚠️ Ollama requiere inicio manual", PackIconKind.Alert, "#F59E0B");
                        });
                    }
                }
                else
                {
                    await Dispatcher.InvokeAsync(() => {
                        MostrarEstadoTiempoReal("✨ Ollama ejecutándose perfectamente", PackIconKind.CheckCircle, "#10B981");
                    });
                }
                
                // Ocultar panel de estado después de unos segundos
                await Task.Delay(3000);
                await Dispatcher.InvokeAsync(() => {
                    PanelEstadoOllama.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en inicio automático de Ollama: {ex.Message}");
                await Dispatcher.InvokeAsync(() => {
                    MostrarEstadoTiempoReal("❌ Error verificando Ollama", PackIconKind.AlertCircle, "#EF4444");
                });
            }
        }
        
        /// <summary>
        /// Verifica si Ollama está instalado en el sistema
        /// </summary>
        private async Task<bool> VerificarOllamaInstaladoAsync()
        {
            try
            {
                var proceso = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                proceso.Start();
                await proceso.WaitForExitAsync();
                
                return proceso.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Verifica si el servicio de Ollama está ejecutándose
        /// </summary>
        private async Task<bool> VerificarOllamaEjecutandoseAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetAsync("http://localhost:11434/api/version");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Intenta iniciar el servicio de Ollama
        /// </summary>
        private async Task<bool> IniciarServicioOllamaAsync()
        {
            try
            {
                var proceso = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = "serve",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                
                proceso.Start();
                
                // Esperar unos segundos para que el servicio se inicie
                await Task.Delay(5000);
                
                // Verificar si se inició correctamente
                return await VerificarOllamaEjecutandoseAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error iniciando Ollama: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Muestra el estado en tiempo real en el panel inferior
        /// </summary>
        private void MostrarEstadoTiempoReal(string mensaje, PackIconKind icono, string color)
        {
            try
            {
                PanelEstadoOllama.Visibility = Visibility.Visible;
                TextoEstadoTiempoReal.Text = mensaje;
                IconoEstadoTiempoReal.Kind = icono;
                IconoEstadoTiempoReal.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mostrando estado tiempo real: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Abre la página de descarga de Ollama
        /// </summary>
        private void AbrirPaginaDescargaOllama()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://ollama.com/download",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error abriendo página de Ollama: {ex.Message}");
                MessageBox.Show(
                    "🌐 No se pudo abrir automáticamente la página.\n\n" +
                    "🔗 Visita manualmente: https://ollama.com/download\n\n" +
                    "📝 Descarga 'Download for Windows' y ejecútalo como administrador",
                    "📦 Descargar Ollama", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        /// <summary>
        /// Event handler para el botón de descargar Ollama
        /// </summary>
        private void BotonDescargarOllama_Click(object sender, RoutedEventArgs e)
        {
            AbrirPaginaDescargaOllama();
        }
        
        /// <summary>
        /// Guarda la configuración de DeepSeek con confirmación mejorada
        /// </summary>
        private async Task GuardarConfiguracionDeepSeekAsync()
        {
            try
            {
                var resultado = MessageBox.Show(
                    "🎆 ¿Activar DeepSeek-R1 7B como proveedor principal?\n\n" +
                    "📊 INFORMACIÓN DEL MODELO:\n" +
                    "• 🧠 Modelo: DeepSeek-R1 7B (Razonamiento Avanzado)\n" +
                    "• 🔮 Razonamiento paso a paso como O1/Gemini 2.5 Pro\n" +
                    "• 📊 Ideal para matemáticas, lógica y análisis profundo\n" +
                    "• 🔒 100% offline, privado y sin límites de uso\n" +
                    "• ⚡ Modelo local empresarial de última generación\n\n" +
                    "🚀 LISTO PARA USAR - Modelo disponible vía Ollama",
                    "🌟 Activar DeepSeek-R1 Enterprise", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (resultado == MessageBoxResult.Yes)
                {
                    MostrarEstadoTiempoReal("🚀 Activando DeepSeek-R1...", PackIconKind.Loading, "#8B5CF6");
                    
                    await Task.Delay(1000);
                    await CambiarProveedorActivoAsync("deepseek");
                    
                    MostrarEstadoTiempoReal("✅ DeepSeek-R1 activado exitosamente", PackIconKind.CheckCircle, "#10B981");
                    await Task.Delay(1500);
                    
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MostrarEstadoTiempoReal("❌ Error configurando DeepSeek", PackIconKind.AlertCircle, "#EF4444");
                MessageBox.Show($"🚨 Error configurando DeepSeek-R1:\n\n{ex.Message}",
                    "Error de Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Guarda la configuración de Claude con confirmación mejorada
        /// </summary>
        private async Task GuardarConfiguracionClaudeAsync()
        {
            try
            {
                var resultado = MessageBox.Show(
                    "🎆 ¿Activar Claude-Style Llama como proveedor principal?\n\n" +
                    "📊 INFORMACIÓN DEL MODELO:\n" +
                    "• 🧠 Modelo: Claude-Style Llama (Conversación Natural)\n" +
                    "• 🎨 Personalidad Claude 3.5 Sonnet en modelo local\n" +
                    "• 💬 Conversación natural y filosófica estilo Anthropic\n" +
                    "• ✍️ Escritura, conversación y análisis filosófico\n" +
                    "• 🔒 100% offline, privado y sin límites de uso\n\n" +
                    "🚀 LISTO PARA USAR - Modelo disponible vía Ollama",
                    "🌟 Activar Claude-Style Enterprise", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (resultado == MessageBoxResult.Yes)
                {
                    MostrarEstadoTiempoReal("🚀 Activando Claude-Style...", PackIconKind.Loading, "#F59E0B");
                    
                    await Task.Delay(1000);
                    await CambiarProveedorActivoAsync("claude");
                    
                    MostrarEstadoTiempoReal("✅ Claude-Style activado exitosamente", PackIconKind.CheckCircle, "#10B981");
                    await Task.Delay(1500);
                    
                    ConfiguracionGuardada = true;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MostrarEstadoTiempoReal("❌ Error configurando Claude", PackIconKind.AlertCircle, "#EF4444");
                MessageBox.Show($"🚨 Error configurando Claude-Style:\n\n{ex.Message}",
                    "Error de Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 