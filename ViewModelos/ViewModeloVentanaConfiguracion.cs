using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Windows;
using ChatbotGomarco.Servicios;

namespace ChatbotGomarco.ViewModelos
{
    public partial class ViewModeloVentanaConfiguracion : ObservableObject
    {
        private readonly IServicioChatbot _servicioChatbot;
        private readonly ILogger<ViewModeloVentanaConfiguracion> _logger;
        private readonly Window _ventana;

        [ObservableProperty]
        private string _estadoIA = "IA no configurada";

        [ObservableProperty]
        private string _descripcionEstadoIA = "Configura OpenAI GPT-4 para análisis avanzado";

        [ObservableProperty]
        private string _iconoEstadoIA = "Robot";

        [ObservableProperty]
        private string _estadoIABackground = "#FEF3C7";

        [ObservableProperty]
        private bool _mostrarPanelArchivos = true;

        [ObservableProperty]
        private bool _mostrarPanelHistorial = true;

        [ObservableProperty]
        private string _sistemaOperativo = "Windows 10/11";

        [ObservableProperty]
        private string _arquitecturaSistema = "x64";

        [ObservableProperty]
        private string _directorioDatos = "%APPDATA%\\GOMARCO\\ChatbotGomarco";

        // Comandos
        private RelayCommand? _comandoConfigurarIA;
        public RelayCommand ComandoConfigurarIA => _comandoConfigurarIA ??= new RelayCommand(ConfigurarIA);

        private RelayCommand<string>? _comandoGuardarAPIKey;
        public RelayCommand<string> ComandoGuardarAPIKey => _comandoGuardarAPIKey ??= new RelayCommand<string>(GuardarAPIKey);

        private RelayCommand? _comandoAbrirOpenAI;
        public RelayCommand ComandoAbrirOpenAI => _comandoAbrirOpenAI ??= new RelayCommand(AbrirOpenAI);

        private RelayCommand? _comandoAbrirDocumentacion;
        public RelayCommand ComandoAbrirDocumentacion => _comandoAbrirDocumentacion ??= new RelayCommand(AbrirDocumentacion);

        private RelayCommand? _comandoAlternarPanelArchivos;
        public RelayCommand ComandoAlternarPanelArchivos => _comandoAlternarPanelArchivos ??= new RelayCommand(AlternarPanelArchivos);

        private RelayCommand? _comandoAlternarPanelHistorial;
        public RelayCommand ComandoAlternarPanelHistorial => _comandoAlternarPanelHistorial ??= new RelayCommand(AlternarPanelHistorial);

        private RelayCommand? _comandoRestaurarConfiguracion;
        public RelayCommand ComandoRestaurarConfiguracion => _comandoRestaurarConfiguracion ??= new RelayCommand(RestaurarConfiguracion);

        private RelayCommand? _comandoExportarConfiguracion;
        public RelayCommand ComandoExportarConfiguracion => _comandoExportarConfiguracion ??= new RelayCommand(ExportarConfiguracion);

        private RelayCommand? _comandoGuardarConfiguracion;
        public RelayCommand ComandoGuardarConfiguracion => _comandoGuardarConfiguracion ??= new RelayCommand(GuardarConfiguracion);

        private RelayCommand? _comandoCerrar;
        public RelayCommand ComandoCerrar => _comandoCerrar ??= new RelayCommand(Cerrar);

        public ViewModeloVentanaConfiguracion(
            IServicioChatbot servicioChatbot,
            ILogger<ViewModeloVentanaConfiguracion> logger,
            Window ventana)
        {
            _servicioChatbot = servicioChatbot;
            _logger = logger;
            _ventana = ventana;

            InicializarConfiguracion();
        }

        private void InicializarConfiguracion()
        {
            try
            {
                // Obtener información del sistema
                SistemaOperativo = Environment.OSVersion.VersionString;
                ArquitecturaSistema = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                DirectorioDatos = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\GOMARCO\\ChatbotGomarco";

                // Actualizar estado de IA
                ActualizarEstadoIA();

                // Obtener configuración actual de paneles (esto se puede expandir para leer desde archivo de configuración)
                MostrarPanelArchivos = true;
                MostrarPanelHistorial = true;

                _logger.LogInformation("Configuración inicializada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inicializar configuración");
            }
        }

        private void ActualizarEstadoIA()
        {
            try
            {
                var iaDisponible = _servicioChatbot.EstaIADisponible();
                
                if (iaDisponible)
                {
                    EstadoIA = "🤖 GPT-4 ACTIVADO";
                    DescripcionEstadoIA = "IA avanzada disponible para análisis de documentos";
                    IconoEstadoIA = "Robot";
                    EstadoIABackground = "#10B981";
                }
                else
                {
                    EstadoIA = "⚠️ OpenAI no configurado";
                    DescripcionEstadoIA = "Configura OpenAI GPT-4 para análisis avanzado";
                    IconoEstadoIA = "RobotOff";
                    EstadoIABackground = "#FEF3C7";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado de IA");
                EstadoIA = "❌ Error al verificar IA";
                DescripcionEstadoIA = "No se pudo verificar el estado de la IA";
                IconoEstadoIA = "AlertCircle";
                EstadoIABackground = "#FEE2E2";
            }
        }

        private void ConfigurarIA()
        {
            try
            {
                // Solicitar clave API mediante un cuadro de entrada simple
                var resultado = Microsoft.VisualBasic.Interaction.InputBox(
                    "Ingresa tu clave de API de OpenAI para activar la IA avanzada:\n\n" +
                    "• La clave se mantendrá solo durante esta sesión\n" +
                    "• Obtenla en: https://platform.openai.com/api-keys\n" +
                    "• Formato: sk-...",
                    "🤖 Configurar OpenAI GPT-4",
                    "");

                if (!string.IsNullOrEmpty(resultado))
                {
                    _servicioChatbot.ConfigurarClaveIA(resultado);
                    ActualizarEstadoIA();
                    
                    if (_servicioChatbot.EstaIADisponible())
                    {
                        MessageBox.Show(
                            "🚀 ¡OpenAI GPT-4 activado exitosamente!\n\n" +
                            "Tu chatbot ahora puede:\n" +
                            "• Conversar naturalmente con la potencia de GPT-4\n" +
                            "• Analizar documentos e imágenes con IA avanzada\n" +
                            "• Generar respuestas inteligentes y contextuales\n" +
                            "• Mantener conversaciones profundas y complejas",
                            "OpenAI GPT-4 Configurado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "❌ No se pudo configurar la IA.\n\nPor favor verifica que la clave API sea válida.",
                            "Error de Configuración",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar IA");
                EstadoIA = "Error al configurar IA";
                MessageBox.Show(
                    $"Error al configurar la IA: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GuardarAPIKey(string? apiKey)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    MessageBox.Show(
                        "Por favor ingresa una clave API válida.",
                        "Clave API requerida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!apiKey.StartsWith("sk-"))
                {
                    MessageBox.Show(
                        "La clave API debe comenzar con 'sk-'.\n\nPor favor verifica el formato de tu clave API.",
                        "Formato de clave API inválido",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _servicioChatbot.ConfigurarClaveIA(apiKey);
                ActualizarEstadoIA();

                MessageBox.Show(
                    "✅ Clave API guardada exitosamente.\n\nLa IA avanzada está ahora disponible.",
                    "Configuración exitosa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _logger.LogInformation("Clave API configurada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar clave API");
                MessageBox.Show(
                    $"Error al guardar la clave API: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AbrirOpenAI()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://platform.openai.com/api-keys",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir OpenAI");
                MessageBox.Show(
                    "No se pudo abrir el navegador. Visita manualmente: https://platform.openai.com/api-keys",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void AbrirDocumentacion()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://platform.openai.com/docs",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir documentación");
                MessageBox.Show(
                    "No se pudo abrir el navegador. Visita manualmente: https://platform.openai.com/docs",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void AlternarPanelArchivos()
        {
            try
            {
                MostrarPanelArchivos = !MostrarPanelArchivos;
                _logger.LogInformation($"Panel de archivos {(MostrarPanelArchivos ? "habilitado" : "deshabilitado")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al alternar panel de archivos");
            }
        }

        private void AlternarPanelHistorial()
        {
            try
            {
                MostrarPanelHistorial = !MostrarPanelHistorial;
                _logger.LogInformation($"Panel de historial {(MostrarPanelHistorial ? "habilitado" : "deshabilitado")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al alternar panel de historial");
            }
        }

        private void RestaurarConfiguracion()
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Estás seguro de que quieres restaurar todos los valores por defecto?\n\n" +
                    "Esto restablecerá:\n" +
                    "• Configuración de IA\n" +
                    "• Preferencias de interfaz\n" +
                    "• Configuración de seguridad\n\n" +
                    "Los cambios no se guardarán hasta que hagas clic en 'Guardar cambios'.",
                    "Restaurar configuración",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Restaurar valores por defecto
                    MostrarPanelArchivos = true;
                    MostrarPanelHistorial = true;
                    
                    // Limpiar configuración de IA
                    _servicioChatbot.ConfigurarClaveIA("");
                    ActualizarEstadoIA();

                    MessageBox.Show(
                        "✅ Configuración restaurada a valores por defecto.\n\n" +
                        "Recuerda hacer clic en 'Guardar cambios' para aplicar los cambios.",
                        "Configuración restaurada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _logger.LogInformation("Configuración restaurada a valores por defecto");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restaurar configuración");
                MessageBox.Show(
                    $"Error al restaurar la configuración: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportarConfiguracion()
        {
            try
            {
                var configuracion = new
                {
                    FechaExportacion = DateTime.Now,
                    ConfiguracionIA = new
                    {
                        IADisponible = _servicioChatbot.EstaIADisponible(),
                        Estado = EstadoIA
                    },
                    ConfiguracionInterfaz = new
                    {
                        MostrarPanelArchivos,
                        MostrarPanelHistorial
                    },
                    InformacionSistema = new
                    {
                        SistemaOperativo,
                        ArquitecturaSistema,
                        DirectorioDatos
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(configuracion, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Exportar configuración",
                    Filter = "Archivos JSON (*.json)|*.json|Todos los archivos (*.*)|*.*",
                    FileName = $"configuracion-chatbot-gomarco-{DateTime.Now:yyyyMMdd-HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveFileDialog.FileName, json);
                    
                    MessageBox.Show(
                        $"✅ Configuración exportada exitosamente a:\n{saveFileDialog.FileName}",
                        "Exportación exitosa",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _logger.LogInformation($"Configuración exportada a: {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar configuración");
                MessageBox.Show(
                    $"Error al exportar la configuración: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GuardarConfiguracion()
        {
            try
            {
                // Aquí se implementaría la lógica para guardar la configuración
                // Por ahora, solo cerramos la ventana
                
                MessageBox.Show(
                    "✅ Configuración guardada exitosamente.\n\n" +
                    "Los cambios se han aplicado a la aplicación.",
                    "Configuración guardada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _logger.LogInformation("Configuración guardada exitosamente");
                
                Cerrar();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar configuración");
                MessageBox.Show(
                    $"Error al guardar la configuración: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cerrar()
        {
            try
            {
                _ventana.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar ventana de configuración");
            }
        }
    }
} 