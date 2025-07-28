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

namespace ChatbotGomarco.ViewModelos
{
    public partial class ViewModeloVentanaPrincipal : ObservableObject
    {
        private readonly IServicioChatbot _servicioChatbot;
        private readonly IServicioHistorialChats _servicioHistorial;
        private readonly IServicioArchivos _servicioArchivos;
        private readonly IServicioConfiguracion _servicioConfiguracion;
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

        public ViewModeloVentanaPrincipal(
            IServicioChatbot servicioChatbot,
            IServicioHistorialChats servicioHistorial,
            IServicioArchivos servicioArchivos,
            IServicioConfiguracion servicioConfiguracion,
            ILogger<ViewModeloVentanaPrincipal> logger)
        {
            _servicioChatbot = servicioChatbot;
            _servicioHistorial = servicioHistorial;
            _servicioArchivos = servicioArchivos;
            _servicioConfiguracion = servicioConfiguracion;
            _logger = logger;

            InicializarAsync();
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
                // Solo intentar cargar si el servicio de configuración está disponible
                if (_servicioConfiguracion != null)
                {
                    var claveGuardada = await _servicioConfiguracion.ObtenerClaveAPIAsync();
                    if (!string.IsNullOrEmpty(claveGuardada))
                    {
                        _servicioChatbot.ConfigurarClaveIA(claveGuardada);
                        _logger.LogInformation("API key cargada automáticamente desde configuración persistente");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al cargar API key guardada - continuando sin configuración persistente");
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
                _logger.LogError(ex, "Error al enviar mensaje");
                MessageBox.Show("Error al procesar tu mensaje. Intenta nuevamente.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // Obtener clave actual si existe
                var claveActual = "";
                try
                {
                    claveActual = await _servicioConfiguracion.ObtenerClaveAPIAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo obtener la clave guardada");
                    // Continuamos con clave vacía
                }

                // Intentar abrir la ventana moderna
                try
                {
                    var ventanaPrincipal = System.Windows.Application.Current.MainWindow;
                    var ventanaConfiguracion = new ChatbotGomarco.Vistas.VentanaConfiguracion(claveActual, IADisponible);
                    ventanaConfiguracion.Owner = ventanaPrincipal;
                    
                    if (ventanaConfiguracion.ShowDialog() == true)
                    {
                        if (ventanaConfiguracion.ConfiguracionGuardada)
                        {
                            await ProcesarConfiguracionGuardada(ventanaConfiguracion.ClaveAPI);
                        }
                        else if (ventanaConfiguracion.ClaveEliminada)
                        {
                            await ProcesarConfiguracionEliminada();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error en ventana moderna, usando método simple");
                    
                    // Fallback al método simple
                    UsarConfiguracionSimple();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar IA");
                EstadoIA = "Error al configurar IA";
                System.Windows.MessageBox.Show(
                    $"Error al configurar la IA: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task ProcesarConfiguracionGuardada(string claveAPI)
        {
            if (string.IsNullOrEmpty(claveAPI)) return;

            try
            {
                // Guardar la clave de forma persistente
                await _servicioConfiguracion.GuardarClaveAPIAsync(claveAPI);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo guardar la configuración persistente");
            }

            // Configurar la IA
            _servicioChatbot.ConfigurarClaveIA(claveAPI);
            ActualizarEstadoIA();

            if (IADisponible)
            {
                System.Windows.MessageBox.Show(
                    "🚀 ¡OpenAI GPT-4 activado exitosamente!\n\n" +
                    "Tu chatbot ahora está listo para todos los chats",
                    "OpenAI GPT-4 Configurado",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "❌ No se pudo configurar la IA.\n\n" +
                    "Por favor verifica que la clave API sea válida.",
                    "Error de Configuración",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        private async Task ProcesarConfiguracionEliminada()
        {
            try
            {
                await _servicioConfiguracion.EliminarClaveAPIAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar la configuración persistente");
            }

            ActualizarEstadoIA();
            
            System.Windows.MessageBox.Show(
                "Configuración eliminada\n\n" +
                "La clave API ha sido eliminada.",
                "Configuración Eliminada",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
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
            IADisponible = _servicioChatbot.EstaIADisponible();
            EstadoIA = IADisponible ? "🤖 GPT-4 ACTIVADO" : "⚠️ OpenAI no configurado";
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