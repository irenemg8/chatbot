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
        private readonly ILogger<ViewModeloVentanaPrincipal> _logger;

        [ObservableProperty]
        private string _mensajeUsuario = string.Empty;

        [ObservableProperty]
        private bool _estaEnviandoMensaje = false;

        [ObservableProperty]
        private bool _estaCargandoArchivo = false;

        [ObservableProperty]
        private SesionChat? _sesionActual;

        [ObservableProperty]
        private string _tituloVentana = "Chatbot GOMARCO - Asistente de IA Corporativo";

        [ObservableProperty]
        private bool _mostrarPanelArchivos = true;

        [ObservableProperty]
        private bool _mostrarPanelHistorial = true;

        [ObservableProperty]
        private string _mensajeBienvenida = "¡Bienvenido al asistente de IA de GOMARCO! 🛏️\n\nSoy tu asistente virtual corporativo, especializado en:\n• Análisis de documentos confidenciales\n• Información sobre productos GOMARCO\n• Soporte con procesos empresariales\n• Gestión segura de archivos\n\n¿En qué puedo ayudarte hoy?";

        public ObservableCollection<MensajeChat> MensajesChat { get; } = new();
        public ObservableCollection<SesionChat> HistorialSesiones { get; } = new();
        public ObservableCollection<ArchivoSubido> ArchivosSubidos { get; } = new();
        public ObservableCollection<string> SugerenciasRespuesta { get; } = new();

        public ViewModeloVentanaPrincipal(
            IServicioChatbot servicioChatbot,
            IServicioHistorialChats servicioHistorial,
            IServicioArchivos servicioArchivos,
            ILogger<ViewModeloVentanaPrincipal> logger)
        {
            _servicioChatbot = servicioChatbot;
            _servicioHistorial = servicioHistorial;
            _servicioArchivos = servicioArchivos;
            _logger = logger;

            InicializarAsync();
        }

        private async void InicializarAsync()
        {
            try
            {
                await CargarHistorialSesionesAsync();
                await CrearNuevaSesionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inicializar la aplicación");
                MessageBox.Show("Error al inicializar la aplicación. Verifica que tengas los permisos necesarios.", 
                    "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var respuestaIA = await _servicioChatbot.ProcesarMensajeAsync(
                    mensajeUsuario, SesionActual.Id, archivosContexto);

                // Agregar respuesta del asistente
                var mensajeAsistente = await _servicioHistorial.AgregarMensajeAsync(
                    SesionActual.Id, respuestaIA, TipoMensaje.Asistente);
                MensajesChat.Add(mensajeAsistente);

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

            // Analizar archivo y agregar mensaje informativo
            var analisisArchivo = await _servicioChatbot.AnalizarArchivoAsync(archivo);
            var mensajeSistema = await _servicioHistorial.AgregarMensajeAsync(
                SesionActual.Id, 
                $"📁 **Archivo cargado:** {archivo.NombreOriginal}\n\n{analisisArchivo}", 
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
    }
} 