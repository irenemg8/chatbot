using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public interface IServicioChatbot
    {
        /// <summary>
        /// Procesa un mensaje del usuario con IA conversacional avanzada
        /// </summary>
        /// <param name="mensaje">Mensaje del usuario</param>
        /// <param name="idSesion">ID de la sesión actual</param>
        /// <param name="archivosContexto">Archivos subidos como contexto</param>
        /// <param name="historialConversacion">Historial de la conversación para contexto</param>
        /// <returns>Respuesta del asistente con IA</returns>
        Task<string> ProcesarMensajeAsync(string mensaje, string idSesion, List<ArchivoSubido>? archivosContexto = null, List<MensajeChat>? historialConversacion = null);

        /// <summary>
        /// Procesa un mensaje usando IA conversacional avanzada (como ChatGPT)
        /// </summary>
        /// <param name="mensaje">Mensaje del usuario</param>
        /// <param name="contextoArchivos">Contenido extraído de archivos</param>
        /// <param name="historialConversacion">Historial de mensajes previos</param>
        /// <returns>Respuesta generada por IA</returns>
        Task<string> ProcesarMensajeConIAAsync(string mensaje, string contextoArchivos = "", List<MensajeChat>? historialConversacion = null);

        /// <summary>
        /// Analiza el contenido de un archivo para extraer información
        /// </summary>
        /// <param name="archivo">Archivo a analizar</param>
        /// <returns>Resumen del contenido del archivo</returns>
        Task<string> AnalizarArchivoAsync(ArchivoSubido archivo);

        /// <summary>
        /// Analiza múltiples archivos y genera respuestas inteligentes
        /// </summary>
        /// <param name="archivos">Lista de archivos a analizar</param>
        /// <param name="pregunta">Pregunta específica sobre los archivos</param>
        /// <returns>Análisis inteligente de los archivos</returns>
        Task<string> AnalizarMultiplesArchivosConIAAsync(List<ArchivoSubido> archivos, string pregunta);

        /// <summary>
        /// Genera un título automático para una conversación basado en el contenido
        /// </summary>
        /// <param name="primerMensaje">Primer mensaje de la conversación</param>
        /// <returns>Título sugerido</returns>
        Task<string> GenerarTituloConversacionAsync(string primerMensaje);

        /// <summary>
        /// Valida si un archivo es seguro para procesar
        /// </summary>
        /// <param name="rutaArchivo">Ruta del archivo</param>
        /// <returns>True si el archivo es seguro</returns>
        Task<bool> ValidarSeguridadArchivoAsync(string rutaArchivo);

        /// <summary>
        /// Obtiene sugerencias de respuesta contextuales
        /// </summary>
        /// <param name="contextoConversacion">Contexto de la conversación</param>
        /// <returns>Lista de sugerencias</returns>
        Task<List<string>> ObtenerSugerenciasRespuestaAsync(List<MensajeChat> contextoConversacion);

        /// <summary>
        /// Procesa múltiples archivos con contexto
        /// </summary>
        /// <param name="archivos">Lista de archivos</param>
        /// <returns>Resultado del procesamiento</returns>
        Task<string> ProcesarMultiplesArchivosAsync(List<ArchivoSubido> archivos);

        /// <summary>
        /// Configura la API key de OpenAI para el servicio de IA
        /// </summary>
        /// <param name="apiKey">Clave de API</param>
        void ConfigurarClaveIA(string apiKey);

        /// <summary>
        /// Verifica si el servicio de IA está configurado y disponible
        /// </summary>
        /// <returns>True si está disponible</returns>
        bool EstaIADisponible();
    }
} 