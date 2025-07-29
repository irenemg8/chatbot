using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ChatbotGomarco.Modelos;
using ChatbotGomarco.Servicios;
using System.Collections.Generic;

namespace ChatbotGomarco.ViewModelos
{
    public partial class ViewModeloVentanaPrincipal : ObservableObject
    {
        private readonly IServicioChatbot _servicioChatbot;
        private readonly IServicioHistorialChats _servicioHistorial;
        private readonly IServicioArchivos _servicioArchivos;
        private readonly IServicioConfiguracion _servicioConfiguracion;
        private readonly IFactoryProveedorIA _factoryProveedorIA;
        private readonly ILogger<ViewModeloVentanaPrincipal> _logger;

        [ObservableProperty]
        private string _mensajeUsuario = string.Empty;

        [ObservableProperty]
        private bool _estaEnviandoMensaje = false;

        [ObservableProperty]
        private bool _estaCargandoArchivo = false;

        [ObservableProperty]
        private bool _estaPensandoConIA = false;

        [ObservableProperty]
        private string _mensajePensamiento = string.Empty;

        [ObservableProperty]
        private SesionChat? _sesionActual;

        [ObservableProperty]
        private string _tituloVentana = "Chatbot GOMARCO - Asistente de IA Corporativo";

        [ObservableProperty]
        private bool _mostrarPanelArchivos = true;

        [ObservableProperty]
        private bool _mostrarPanelHistorial = true;

        [ObservableProperty]
        private string _mensajeBienvenida = "¡Bienvenido al asistente de IA de GOMARCO! 🛏️\n\nSoy tu asistente virtual corporativo, especializado en:\n• Análisis de documentos confidenciales\n• Soporte con procesos empresariales\n• Gestión segura de archivos\n\n¿En qué puedo ayudarte hoy?";

        public ObservableCollection<MensajeChat> MensajesChat { get; } = new();
        public ObservableCollection<SesionChat> HistorialSesiones { get; } = new();
        public ObservableCollection<ArchivoSubido> ArchivosSubidos { get; } = new();
        public ObservableCollection<string> SugerenciasRespuesta { get; } = new();

        // Nueva propiedad para el estado de IA
        private bool _iaDisponible;
        public bool IADisponible
        {
            get => _iaDisponible;
            set => SetProperty(ref _iaDisponible, value);
        }

        private string _estadoIA = "IA no configurada";
        public string EstadoIA
        {
            get => _estadoIA;
            set => SetProperty(ref _estadoIA, value);
        }

            // Comando para configurar IA
    private RelayCommand? _comandoConfigurarIA;
    public RelayCommand ComandoConfigurarIA => _comandoConfigurarIA ??= new RelayCommand(ConfigurarIA);
    
    // DEBUG: Comando temporal para probar la IA directamente
    private RelayCommand? _comandoDebugIA;
    public RelayCommand ComandoDebugIA => _comandoDebugIA ??= new RelayCommand(ProbarIADebug);

        public ViewModeloVentanaPrincipal(
            IServicioChatbot servicioChatbot,
            IServicioHistorialChats servicioHistorial,
            IServicioArchivos servicioArchivos,
            IServicioConfiguracion servicioConfiguracion,
            IFactoryProveedorIA factoryProveedorIA,
            ILogger<ViewModeloVentanaPrincipal> logger)
        {
            _servicioChatbot = servicioChatbot;
            _servicioHistorial = servicioHistorial;
            _servicioArchivos = servicioArchivos;
            _servicioConfiguracion = servicioConfiguracion;
            _factoryProveedorIA = factoryProveedorIA;
            _logger = logger;

            InicializarAsync();
        }
        
        /// <summary>
        /// Fuerza la actualización del estado de IA después de configurar API key
        /// </summary>
        private void RefrescarServicioIA()
        {
            _logger.LogInformation("🔄 DEBUG - Iniciando refresh completo del servicio IA...");
            
            // Forzar múltiples verificaciones para asegurar actualización
            try
            {
                // Primero, actualizar el estado normal
                ActualizarEstadoIA();
                
                // Agregar delay pequeño para asegurar que la configuración se propague
                System.Threading.Thread.Sleep(100);
                
                // Verificar nuevamente
                ActualizarEstadoIA();
                
                _logger.LogInformation("✅ DEBUG - Servicio IA refrescado exitosamente. Estado final: {Estado}", EstadoIA);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ DEBUG - Error al refrescar servicio IA");
            }
        }

        private async void InicializarAsync()
        {
            try
            {
                await CargarHistorialSesionesAsync();
                await CrearNuevaSesionAsync();
                
                // Cargar API key guardada al iniciar
                await CargarAPIKeyGuardadaAsync();
                
                ActualizarEstadoIA();
                
                // DEBUG: Agregar mensaje temporal de estado para diagnóstico
                var mensajeDebug = new MensajeChat
                {
                    Id = 0, // Temporal
                    Contenido = $"🔍 DEBUG - Sistema iniciado. IA Disponible: {IADisponible}. Estado: {EstadoIA}",
                    TipoMensaje = TipoMensaje.Sistema,
                    FechaCreacion = DateTime.Now,
                    IdSesionChat = SesionActual?.Id ?? ""
                };
                MensajesChat.Add(mensajeDebug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inicializar la aplicación");
                MessageBox.Show("Error al inicializar la aplicación. Verifica que tengas los permisos necesarios.", 
                    "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Carga automáticamente la API key guardada al iniciar la aplicación
        /// </summary>
        private async Task CargarAPIKeyGuardadaAsync()
        {
            try
            {
                _logger.LogInformation("🔍 DEBUG - Iniciando carga de API key guardada...");
                
                // Solo intentar cargar si el servicio de configuración está disponible
                if (_servicioConfiguracion != null)
                {
                    var claveGuardada = await _servicioConfiguracion.ObtenerClaveAPIAsync();
                    _logger.LogInformation("🔍 DEBUG - Clave obtenida del servicio: {TieneClave}", !string.IsNullOrEmpty(claveGuardada));
                    
                    if (!string.IsNullOrEmpty(claveGuardada))
                    {
                        var maskedKey = claveGuardada.Length > 10 
                            ? $"{claveGuardada[..7]}...{claveGuardada[^4..]}" 
                            : "sk-***";
                        
                        _logger.LogInformation("✅ DEBUG - Configurando clave cargada: {MaskedKey}", maskedKey);
                        
                        // CRÍTICO: Usar sistema multi-proveedor para cargar API key
                        try 
                        {
                            var configuracion = new Dictionary<string, string>
                            {
                                ["apikey"] = claveGuardada
                            };
                            
                            var proveedorOpenAI = _factoryProveedorIA.ObtenerProveedor("openai");
                            await proveedorOpenAI.ConfigurarAsync(configuracion);
                            
                            // También configurar el sistema legacy para compatibilidad
                            _servicioChatbot.ConfigurarClaveIA(claveGuardada);
                            
                            _logger.LogInformation("✅ API key cargada automáticamente desde configuración persistente");
                        }
                        catch (Exception configEx)
                        {
                            _logger.LogError(configEx, "❌ Error configurando proveedor OpenAI con clave guardada");
                            // Fallback al sistema legacy
                            _servicioChatbot.ConfigurarClaveIA(claveGuardada);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ DEBUG - No hay API key guardada, usuario necesita configurar");
                    }
                }
                else
                {
                    _logger.LogWarning("❌ DEBUG - ServicioConfiguracion es NULL");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR al cargar API key guardada - continuando sin configuración persistente");
                // No es crítico, la aplicación puede funcionar sin configuración persistente
            }
        }

        [RelayCommand]
        private async Task EnviarMensajeAsync()
        {
            if (string.IsNullOrWhiteSpace(MensajeUsuario) || EstaEnviandoMensaje || SesionActual == null)
                return;

            try
            {
                EstaEnviandoMensaje = true;
                var mensajeUsuario = MensajeUsuario.Trim();
                MensajeUsuario = string.Empty;

                // Agregar mensaje del usuario
                var mensajeUser = await _servicioHistorial.AgregarMensajeAsync(
                    SesionActual.Id, mensajeUsuario, TipoMensaje.Usuario);
                MensajesChat.Add(mensajeUser);

                // Actualizar título si es el primer mensaje
                if (SesionActual.CantidadMensajes == 1)
                {
                    var nuevoTitulo = await _servicioChatbot.GenerarTituloConversacionAsync(mensajeUsuario);
                    await _servicioHistorial.ActualizarTituloSesionAsync(SesionActual.Id, nuevoTitulo);
                    SesionActual.Titulo = nuevoTitulo;
                    ActualizarSesionEnHistorial();
                }

                // Procesar mensaje con IA
                var archivosContexto = ArchivosSubidos.ToList();
                
                // Mostrar indicador de pensamiento inteligente
                EstaPensandoConIA = true;
                var tieneArchivos = archivosContexto.Any();
                
                // 🧠 Mensaje de pensamiento contextual
                MensajePensamiento = GenerarMensajePensamientoInteligente(mensajeUsuario, archivosContexto);

                // Agregar mensaje temporal de "pensando" con estilo especial
                var mensajePensando = new MensajeChat
                {
                    Id = 0, // Temporal, no se guarda en BD
                    Contenido = MensajePensamiento,
                    TipoMensaje = TipoMensaje.Sistema, // Usar tipo especial para pensamiento
                    FechaCreacion = DateTime.Now,
                    IdSesionChat = SesionActual.Id
                };
                MensajesChat.Add(mensajePensando);

                try
                {
                    var respuestaIA = await _servicioChatbot.ProcesarMensajeAsync(
                        mensajeUsuario, SesionActual.Id, archivosContexto);

                    // Quitar mensaje de "pensando"
                    MensajesChat.Remove(mensajePensando);

                    // Agregar respuesta real del asistente
                    var mensajeAsistente = await _servicioHistorial.AgregarMensajeAsync(
                        SesionActual.Id, respuestaIA, TipoMensaje.Asistente);
                    MensajesChat.Add(mensajeAsistente);
                }
                finally
                {
                    EstaPensandoConIA = false;
                    MensajePensamiento = string.Empty;
                }

                // Generar sugerencias
                await GenerarSugerenciasAsync();
            }
            catch (Exception ex)
            {
                // Remover mensaje de "pensando" si aún está presente
                var mensajePensando = MensajesChat.FirstOrDefault(m => m.Id == 0 && m.TipoMensaje == TipoMensaje.Sistema);
                if (mensajePensando != null)
                {
                    MensajesChat.Remove(mensajePensando);
                }
                
                _logger.LogError(ex, "Error al enviar mensaje");
                
                // Determinar el tipo de error y mostrar mensaje apropiado
                string mensajeError;
                string tituloError = "⚠️ Error al Procesar Mensaje";
                
                if (ex.Message.Contains("API Key") || ex.Message.Contains("🔐"))
                {
                    mensajeError = "🔐 Problema con la configuración de OpenAI.\n\n" +
                                  "Solución: Ve a configuración y verifica tu API Key.";
                    tituloError = "🔑 Error de Configuración";
                }
                else if (ex.Message.Contains("créditos") || ex.Message.Contains("💳"))
                {
                    mensajeError = "💳 Sin créditos suficientes en OpenAI.\n\n" +
                                  "Solución: Recarga saldo en https://platform.openai.com/usage";
                    tituloError = "💰 Sin Créditos";
                }
                else if (ex.Message.Contains("conexión") || ex.Message.Contains("🌐"))
                {
                    mensajeError = "🌐 Problema de conexión a internet.\n\n" +
                                  "Solución: Verifica tu conexión e intenta nuevamente.";
                    tituloError = "📡 Sin Conexión";
                }
                else if (ex.Message.Contains("timeout") || ex.Message.Contains("⏰"))
                {
                    mensajeError = "⏰ La respuesta tardó demasiado.\n\n" +
                                  "Solución: Tu mensaje puede ser muy complejo. Intenta con uno más simple.";
                    tituloError = "⏱️ Tiempo Agotado";
                }
                else
                {
                    mensajeError = $"❌ Error inesperado:\n\n{ex.Message}\n\n" +
                                  "💡 Sugerencias:\n" +
                                  "• Intenta reformular tu mensaje\n" +
                                  "• Verifica tu conexión a internet\n" +
                                  "• Si persiste, reinicia la aplicación";
                }
                    
                // Agregar mensaje de error al chat para que el usuario lo vea
                var mensajeErrorChat = new MensajeChat
                {
                    Id = 0, // Temporal
                    Contenido = $"⚠️ {mensajeError}",
                    TipoMensaje = TipoMensaje.Sistema,
                    FechaCreacion = DateTime.Now,
                    IdSesionChat = SesionActual?.Id ?? ""
                };
                MensajesChat.Add(mensajeErrorChat);
                
                // También mostrar el MessageBox para errores críticos
                if (ex.Message.Contains("API Key") || ex.Message.Contains("créditos") || ex.Message.Contains("🔐") || ex.Message.Contains("💳"))
                {
                    MessageBox.Show(mensajeError, tituloError, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                EstaEnviandoMensaje = false;
            }
        }

        [RelayCommand]
        private async Task CargarArchivoAsync()
        {
            if (EstaCargandoArchivo || SesionActual == null)
                return;

            try
            {
                var dialogo = new OpenFileDialog
                {
                    Title = "Seleccionar archivo para cargar",
                    Filter = "Todos los archivos compatibles|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.txt;*.csv;*.json;*.xml;*.jpg;*.jpeg;*.png;*.gif;*.bmp|" +
                            "Documentos PDF|*.pdf|" +
                            "Documentos Word|*.doc;*.docx|" +
                            "Hojas de cálculo|*.xls;*.xlsx|" +
                            "Presentaciones|*.ppt;*.pptx|" +
                            "Archivos de texto|*.txt;*.csv|" +
                            "Datos estructurados|*.json;*.xml|" +
                            "Imágenes|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                    Multiselect = true
                };

                if (dialogo.ShowDialog() == true)
                {
                    EstaCargandoArchivo = true;

                    foreach (var rutaArchivo in dialogo.FileNames)
                    {
                        await ProcesarArchivoSeleccionadoAsync(rutaArchivo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar archivo");
                MessageBox.Show("Error al cargar el archivo. Verifica el formato y tamaño.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                EstaCargandoArchivo = false;
            }
        }

        private async Task ProcesarArchivoSeleccionadoAsync(string rutaArchivo)
        {
            // Validar seguridad del archivo
            var esSeguro = await _servicioChatbot.ValidarSeguridadArchivoAsync(rutaArchivo);
            if (!esSeguro)
            {
                MessageBox.Show($"El archivo {Path.GetFileName(rutaArchivo)} no es compatible o excede el tamaño máximo permitido (1GB).", 
                    "Archivo no válido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Subir y cifrar archivo
            var archivo = await _servicioArchivos.SubirArchivoAsync(
                rutaArchivo, SesionActual!.Id, "Archivo cargado por el usuario");

            ArchivosSubidos.Add(archivo);

            // Mensaje simple de confirmación
            var mensajeSistema = await _servicioHistorial.AgregarMensajeAsync(
                SesionActual.Id, 
                $"✅ Archivo cargado: {archivo.NombreOriginal}", 
                TipoMensaje.Sistema,
                archivo.RutaArchivoCifrado,
                archivo.NombreOriginal);

            MensajesChat.Add(mensajeSistema);
        }

        [RelayCommand]
        private async Task EliminarArchivoAsync(ArchivoSubido archivo)
        {
            try
            {
                var resultado = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar el archivo '{archivo.NombreOriginal}'?\n\nEsta acción no se puede deshacer.", 
                    "Confirmar eliminación", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    await _servicioArchivos.EliminarArchivoAsync(archivo.Id);
                    ArchivosSubidos.Remove(archivo);

                    // Agregar mensaje informativo
                    var mensajeSistema = await _servicioHistorial.AgregarMensajeAsync(
                        SesionActual!.Id,
                        $"🗑️ Archivo eliminado: {archivo.NombreOriginal}",
                        TipoMensaje.Sistema);

                    MensajesChat.Add(mensajeSistema);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar archivo: {Id}", archivo.Id);
                MessageBox.Show("Error al eliminar el archivo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task CrearNuevaSesionAsync()
        {
            try
            {
                var nuevaSesion = await _servicioHistorial.CrearNuevaSesionAsync();
                SesionActual = nuevaSesion;
                
                MensajesChat.Clear();
                ArchivosSubidos.Clear();
                SugerenciasRespuesta.Clear();

                // Agregar mensaje de bienvenida
                var mensajeBienvenida = await _servicioHistorial.AgregarMensajeAsync(
                    nuevaSesion.Id, MensajeBienvenida, TipoMensaje.Sistema);
                MensajesChat.Add(mensajeBienvenida);

                await CargarHistorialSesionesAsync();
                await GenerarSugerenciasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear nueva sesión");
                MessageBox.Show("Error al crear nueva conversación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task CargarSesionAsync(SesionChat sesion)
        {
            try
            {
                if (sesion.Id == SesionActual?.Id)
                    return;

                var sesionCompleta = await _servicioHistorial.ObtenerSesionComplentaAsync(sesion.Id);
                if (sesionCompleta == null)
                    return;

                SesionActual = sesionCompleta;
                
                // Cargar mensajes
                MensajesChat.Clear();
                foreach (var mensaje in sesionCompleta.Mensajes)
                {
                    MensajesChat.Add(mensaje);
                }

                // Cargar archivos
                ArchivosSubidos.Clear();
                foreach (var archivo in sesionCompleta.ArchivosSubidos)
                {
                    ArchivosSubidos.Add(archivo);
                }

                await GenerarSugerenciasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar sesión: {Id}", sesion.Id);
                MessageBox.Show("Error al cargar la conversación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task EliminarSesionAsync(SesionChat sesion)
        {
            try
            {
                var resultado = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar la conversación '{sesion.Titulo}'?\n\nSe eliminarán todos los mensajes y archivos asociados. Esta acción no se puede deshacer.", 
                    "Confirmar eliminación", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    await _servicioHistorial.EliminarSesionAsync(sesion.Id);
                    HistorialSesiones.Remove(sesion);

                    // Si es la sesión actual, crear una nueva
                    if (sesion.Id == SesionActual?.Id)
                    {
                        await CrearNuevaSesionAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar sesión: {Id}", sesion.Id);
                MessageBox.Show("Error al eliminar la conversación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AlternarPanelArchivos()
        {
            MostrarPanelArchivos = !MostrarPanelArchivos;
        }

        [RelayCommand]
        private void AlternarPanelHistorial()
        {
            MostrarPanelHistorial = !MostrarPanelHistorial;
        }

        [RelayCommand]
        private async Task UsarSugerenciaAsync(string sugerencia)
        {
            MensajeUsuario = sugerencia;
            await EnviarMensajeAsync();
        }

        private async Task CargarHistorialSesionesAsync()
        {
            try
            {
                var sesiones = await _servicioHistorial.ObtenerTodasLasSesionesAsync();
                HistorialSesiones.Clear();
                
                foreach (var sesion in sesiones)
                {
                    HistorialSesiones.Add(sesion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar historial de sesiones");
            }
        }

        private async Task GenerarSugerenciasAsync()
        {
            try
            {
                var sugerencias = await _servicioChatbot.ObtenerSugerenciasRespuestaAsync(MensajesChat.ToList());
                SugerenciasRespuesta.Clear();
                
                foreach (var sugerencia in sugerencias)
                {
                    SugerenciasRespuesta.Add(sugerencia);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar sugerencias");
            }
        }

        private void ActualizarSesionEnHistorial()
        {
            if (SesionActual == null) return;

            var sesionEnHistorial = HistorialSesiones.FirstOrDefault(s => s.Id == SesionActual.Id);
            if (sesionEnHistorial != null)
            {
                sesionEnHistorial.Titulo = SesionActual.Titulo;
                sesionEnHistorial.FechaUltimaActividad = SesionActual.FechaUltimaActividad;
            }
        }

        private async void ConfigurarIA()
        {
            try
            {
                _logger.LogInformation("Abriendo configuración multi-proveedor de IA");
                
                // Abrir directamente la ventana de configuración multi-proveedor
                try
                {
                    // Verificar si ya hay configuración existente
                    bool iaConfigurada = EstadoIA != "No configurada";
                    string apiKeyActual = "";
                    
                    var ventanaConfig = iaConfigurada 
                        ? new ChatbotGomarco.Vistas.VentanaConfiguracion(apiKeyActual, true)
                        : new ChatbotGomarco.Vistas.VentanaConfiguracion();
                    
                    var resultado = ventanaConfig.ShowDialog();
                    
                    if (resultado == true && ventanaConfig.ConfiguracionGuardada)
                    {
                        // Manejar configuración según el proveedor seleccionado
                        if (ventanaConfig.ProveedorSeleccionado == "openai" && !string.IsNullOrEmpty(ventanaConfig.ClaveAPI))
                        {
                            await ConfigurarAPIKeyAsync(ventanaConfig.ClaveAPI);
                        }
                        else if (ventanaConfig.ProveedorSeleccionado == "ollama")
                        {
                            _logger.LogInformation("Ollama configurado como proveedor activo");
                            // Actualizar estado de IA
                            EstadoIA = "Ollama Configurado";
                            OnPropertyChanged(nameof(EstadoIA));
                        }
                    }
                    else if (resultado == true && ventanaConfig.ClaveEliminada)
                    {
                        // Manejar eliminación de configuración - usar método existente
                        await ConfigurarAPIKeyAsync("");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al usar ventana de configuración multi-proveedor, usando método simple");
                    // Fallback al método simple solo para casos críticos
                    UsarConfiguracionSimple();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante configuración de IA");
                MessageBox.Show(
                    $"Error durante la configuración:\n\n{ex.Message}\n\n" +
                    "Por favor, intenta nuevamente o verifica tu conexión a internet.",
                    "Error de Configuración", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Configura y valida la API key de OpenAI
        /// </summary>
        private async Task ConfigurarAPIKeyAsync(string claveAPI)
        {
            if (string.IsNullOrWhiteSpace(claveAPI))
            {
                MessageBox.Show(
                    "⚠️ No se proporcionó ninguna clave API.\n\nPor favor ingresa una clave válida que comience con 'sk-'",
                    "Clave API Requerida", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var maskedKey = claveAPI.Length > 10 
                    ? $"{claveAPI[..7]}...{claveAPI[^4..]}" 
                    : "sk-***";
                
                _logger.LogInformation("Configurando clave API de OpenAI: {MaskedKey}", maskedKey);

                // CRÍTICO: Usar el sistema multi-proveedor en lugar del legacy
                // _servicioChatbot.ConfigurarClaveIA(claveAPI); // Sistema legacy - NO USAR
                
                // Configurar usando el nuevo sistema multi-proveedor
                var configuracion = new Dictionary<string, string>
                {
                    ["apikey"] = claveAPI
                };
                
                var proveedorOpenAI = _factoryProveedorIA.ObtenerProveedor("openai");
                await proveedorOpenAI.ConfigurarAsync(configuracion);
                
                // Validar inmediatamente con una prueba simple
                EstaPensandoConIA = true;
                MensajePensamiento = "🔍 Validando conexión con OpenAI GPT-4...";
                
                try
                {
                    // Usar un mensaje de prueba más simple y directo
                    var respuestaPrueba = await _servicioChatbot.ProcesarMensajeConIAAsync(
                        "Responde únicamente: CONEXIÓN EXITOSA", 
                        "", 
                        null);
                    
                    EstaPensandoConIA = false;
                    
                    if (!string.IsNullOrWhiteSpace(respuestaPrueba))
                    {
                        // Guardar la configuración si es exitosa
                        try
                        {
                            if (_servicioConfiguracion != null)
                            {
                                await _servicioConfiguracion.GuardarClaveAPIAsync(claveAPI);
                                _logger.LogInformation("Clave API guardada de forma persistente");
                            }
                        }
                        catch (Exception saveEx)
                        {
                            _logger.LogWarning(saveEx, "No se pudo guardar la clave API de forma persistente - continuando sin persistencia");
                        }
                        
                        // Refrescar servicio IA para asegurar actualización completa
                        RefrescarServicioIA();
                        
                        MessageBox.Show(
                            "✅ ¡OpenAI GPT-4 configurado exitosamente!\n\n" +
                            $"🔑 Clave configurada: {maskedKey}\n" +
                            $"🎯 Respuesta de prueba: {respuestaPrueba.Substring(0, Math.Min(50, respuestaPrueba.Length))}...\n\n" +
                            "🚀 Funciones disponibles:\n" +
                            "• Conversaciones naturales avanzadas con GPT-4\n" +
                            "• Análisis inteligente de documentos\n" +
                            "• Resúmenes automáticos y detallados\n" +
                            "• Protección de datos sensibles\n\n" +
                            "💡 ¡Ya puedes empezar a chatear!",
                            "🎉 Configuración Exitosa", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                            
                        _logger.LogInformation("OpenAI API configurada y validada exitosamente");
                    }
                    else
                    {
                        throw new Exception("OpenAI respondió con contenido vacío o nulo");
                    }
                }
                catch (Exception ex)
                {
                    EstaPensandoConIA = false;
                    _logger.LogError(ex, "Error al validar la conexión con OpenAI");
                    
                    // Mostrar error más detallado y amigable
                    var errorMsg = ex.Message.Contains("🔐") || ex.Message.Contains("💳") || ex.Message.Contains("⏰") 
                        ? ex.Message // Ya contiene emojis y formato amigable
                        : $"❌ Error al conectar con OpenAI:\n\n{ex.Message}";
                    
                    MessageBox.Show(
                        $"{errorMsg}\n\n" +
                        "🔧 Consejos adicionales:\n" +
                        "• Copia y pega la clave desde https://platform.openai.com/api-keys\n" +
                        "• Asegúrate de no incluir espacios antes o después\n" +
                        "• Verifica que tu cuenta OpenAI esté activa\n" +
                        "• Si persiste el problema, espera unos minutos e intenta nuevamente",
                        "⚠️ Error de Conexión", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    
                    // Refrescar servicio IA incluso después de error
                    RefrescarServicioIA();
                }
            }
            catch (Exception ex)
            {
                EstaPensandoConIA = false;
                _logger.LogError(ex, "Error al configurar API key");
                
                var errorMsg = ex.Message.Contains("API Key") ? ex.Message : $"Error de configuración: {ex.Message}";
                
                MessageBox.Show(
                    $"🚨 {errorMsg}\n\n" +
                    "📝 Formato requerido:\n" +
                    "• Debe comenzar con 'sk-'\n" +
                    "• Todos los formatos OpenAI son válidos\n" +
                    "• No debe contener espacios extra\n\n" +
                    "🔗 Obtén tu clave en:\n" +
                    "https://platform.openai.com/api-keys",
                    "❌ Error de Configuración", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                    
                ActualizarEstadoIA();
            }
        }

        private void UsarConfiguracionSimple()
        {
            // Método simple de respaldo
            var resultado = Microsoft.VisualBasic.Interaction.InputBox(
                "Ingresa tu clave de API de OpenAI:\n\n" +
                "• Formato: sk-...\n" +
                "• Obtenla en: https://platform.openai.com/api-keys",
                "Configurar OpenAI GPT-4",
                "");

            if (!string.IsNullOrEmpty(resultado))
            {
                _servicioChatbot.ConfigurarClaveIA(resultado);
                ActualizarEstadoIA();
                
                if (IADisponible)
                {
                    System.Windows.MessageBox.Show(
                        "¡OpenAI GPT-4 activado exitosamente!",
                        "Configuración Exitosa",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private void ActualizarEstadoIA()
        {
            try
            {
                IADisponible = _servicioChatbot.EstaIADisponible();
                
                // DEBUG: Log detallado del estado
                _logger.LogInformation("🔍 DEBUG - Verificando estado IA: IADisponible={IADisponible}", IADisponible);
                
                if (IADisponible)
                {
                    EstadoIA = "🤖 OpenAI GPT-4 ACTIVO";
                    TituloVentana = "Chatbot GOMARCO - IA Avanzada con OpenAI GPT-4";
                    _logger.LogInformation("✅ Estado IA actualizado: OpenAI GPT-4 disponible y funcionando");
                }
                else
                {
                    EstadoIA = "⚠️ OpenAI no configurado - Click para configurar";  
                    TituloVentana = "Chatbot GOMARCO - Configuración de IA Requerida";
                    _logger.LogWarning("❌ Estado IA actualizado: OpenAI NO disponible - requiere configuración");
                    _logger.LogWarning("🔧 DEBUG - IA no disponible. Verifica si la API key está configurada correctamente.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado de IA");
                IADisponible = false;
                EstadoIA = "❌ Error en configuración de IA";
                TituloVentana = "Chatbot GOMARCO - Error de IA";
            }
                }
        
        private async void ProbarIADebug()
        {
            try
            {
                var mensajeDebug = new MensajeChat
                {
                    Id = 0,
                    Contenido = "🔧 DEBUG - Iniciando prueba directa de IA...",
                    TipoMensaje = TipoMensaje.Sistema,
                    FechaCreacion = DateTime.Now,
                    IdSesionChat = SesionActual?.Id ?? ""
                };
                MensajesChat.Add(mensajeDebug);
                
                // Verificar estado de IA
                var iaDisponible = _servicioChatbot.EstaIADisponible();
                var mensajeEstado = new MensajeChat
                {
                    Id = 0,
                    Contenido = $"🔍 DEBUG - Estado IA: {(iaDisponible ? "✅ DISPONIBLE" : "❌ NO DISPONIBLE")}",
                    TipoMensaje = TipoMensaje.Sistema,
                    FechaCreacion = DateTime.Now,
                    IdSesionChat = SesionActual?.Id ?? ""
                };
                MensajesChat.Add(mensajeEstado);
                
                if (iaDisponible)
                {
                    EstaPensandoConIA = true;
                    MensajePensamiento = "🧪 Probando conexión directa con OpenAI...";
                    
                    try
                    {
                        var respuesta = await _servicioChatbot.ProcesarMensajeConIAAsync(
                            "DEBUG: Responde solo con 'OpenAI conectado exitosamente'", 
                            "", 
                            null);
                        
                        var mensajeRespuesta = new MensajeChat
                        {
                            Id = 0,
                            Contenido = $"🎉 DEBUG - Respuesta de OpenAI: {respuesta}",
                            TipoMensaje = TipoMensaje.Sistema,
                            FechaCreacion = DateTime.Now,
                            IdSesionChat = SesionActual?.Id ?? ""
                        };
                        MensajesChat.Add(mensajeRespuesta);
                    }
                    catch (Exception ex)
                    {
                        var mensajeError = new MensajeChat
                        {
                            Id = 0,
                            Contenido = $"❌ DEBUG - Error al probar IA: {ex.Message}",
                            TipoMensaje = TipoMensaje.Sistema,
                            FechaCreacion = DateTime.Now,
                            IdSesionChat = SesionActual?.Id ?? ""
                        };
                        MensajesChat.Add(mensajeError);
                    }
                    finally
                    {
                        EstaPensandoConIA = false;
                        MensajePensamiento = string.Empty;
                    }
                }
                else
                {
                    var mensajeNoDisponible = new MensajeChat
                    {
                        Id = 0,
                        Contenido = "⚠️ DEBUG - IA no disponible. Haz clic en el estado de IA para configurar OpenAI.",
                        TipoMensaje = TipoMensaje.Sistema,
                        FechaCreacion = DateTime.Now,
                        IdSesionChat = SesionActual?.Id ?? ""
                    };
                    MensajesChat.Add(mensajeNoDisponible);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en debug: {ex.Message}", "Error Debug", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 🧠 Genera mensajes de pensamiento contextuales e inteligentes
        /// </summary>
        private string GenerarMensajePensamientoInteligente(string mensajeUsuario, System.Collections.Generic.List<ArchivoSubido> archivos)
        {
            var mensajeLower = mensajeUsuario.ToLowerInvariant();
            var tieneArchivos = archivos.Any();
            
            if (!tieneArchivos)
            {
                // Mensajes para consultas sin archivos
                if (mensajeLower.Contains("hola") || mensajeLower.Contains("buenos") || mensajeLower.Contains("saludos"))
                    return "👋 IA de GOMARCO preparando respuesta personalizada...";
                
                return "🤖 IA de GOMARCO procesando tu consulta con GPT-4...";
            }
            
            // Análisis inteligente por tipo de archivo y consulta
            var tipoConsulta = DeterminarTipoConsulta(mensajeLower);
            var tipoArchivo = DeterminarTipoArchivo(archivos);
            var cantidad = archivos.Count;
            
            if (tipoConsulta == "calcular" && tipoArchivo == "factura")
            {
                if (cantidad == 1)
                    return "🧮 IA de GOMARCO calculando totales de tu factura...";
                else
                    return $"📊 IA de GOMARCO analizando {cantidad} facturas y calculando promedios...";
            }
            
            if (tipoConsulta == "resumir" && tipoArchivo == "informe")
                return "📈 IA de GOMARCO analizando informe financiero y preparando resumen ejecutivo...";
            
            if (tipoConsulta == "fechas")
                return "📅 IA de GOMARCO identificando fechas importantes en tus documentos...";
            
            if (tipoConsulta == "personas")
                return "👥 IA de GOMARCO buscando contactos y personas clave...";
            
            if (cantidad == 1)
                return "🔍 IA de GOMARCO analizando tu documento con GPT-4...";
            
            return $"📋 IA de GOMARCO procesando {cantidad} documentos de forma inteligente...";
        }
        
        /// <summary>
        /// 🎯 Determina el tipo de consulta del usuario
        /// </summary>
        private string DeterminarTipoConsulta(string mensajeLower)
        {
            if (mensajeLower.Contains("cuánto") || mensajeLower.Contains("total") || mensajeLower.Contains("suma") || mensajeLower.Contains("promedio"))
                return "calcular";
            
            if (mensajeLower.Contains("resumen") || mensajeLower.Contains("resume"))
                return "resumir";
            
            if (mensajeLower.Contains("cuándo") || mensajeLower.Contains("fecha") || mensajeLower.Contains("cuando"))
                return "fechas";
            
            if (mensajeLower.Contains("quién") || mensajeLower.Contains("contacto") || mensajeLower.Contains("persona"))
                return "personas";
            
            return "general";
        }
        
        /// <summary>
        /// 📄 Determina el tipo principal de archivo
        /// </summary>
        private string DeterminarTipoArchivo(System.Collections.Generic.List<ArchivoSubido> archivos)
        {
            if (!archivos.Any()) return "ninguno";
            
            var nombres = string.Join(" ", archivos.Select(a => a.NombreOriginal.ToLowerInvariant()));
            
            if (nombres.Contains("factura") || nombres.Contains("invoice"))
                return "factura";
            
            if (nombres.Contains("informe") || nombres.Contains("report"))
                return "informe";
            
            if (nombres.Contains("contrato") || nombres.Contains("contract"))
                return "contrato";
            
            return "documento";
        }
    }
} 