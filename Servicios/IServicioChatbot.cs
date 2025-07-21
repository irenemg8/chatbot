using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public interface IServicioChatbot
    {
        /// <summary>
        /// Procesa un mensaje del usuario y genera una respuesta
        /// </summary>
        /// <param name="mensaje">Mensaje del usuario</param>
        /// <param name="idSesion">ID de la sesión actual</param>
        /// <param name="archivosContexto">Archivos subidos como contexto</param>
        /// <returns>Respuesta del asistente</returns>
        Task<string> ProcesarMensajeAsync(string mensaje, string idSesion, List<ArchivoSubido>? archivosContexto = null);

        /// <summary>
        /// Analiza el contenido de un archivo para extraer información
        /// </summary>
        /// <param name="archivo">Archivo a analizar</param>
        /// <returns>Resumen del contenido del archivo</returns>
        Task<string> AnalizarArchivoAsync(ArchivoSubido archivo);

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
        /// Obtiene sugerencias de respuesta rápida basadas en el contexto
        /// </summary>
        /// <param name="contextoConversacion">Últimos mensajes de la conversación</param>
        /// <returns>Lista de sugerencias</returns>
        Task<List<string>> ObtenerSugerenciasRespuestaAsync(List<MensajeChat> contextoConversacion);

        /// <summary>
        /// Procesa múltiples archivos de contexto y genera un resumen
        /// </summary>
        /// <param name="archivos">Lista de archivos</param>
        /// <returns>Resumen consolidado</returns>
        Task<string> ProcesarMultiplesArchivosAsync(List<ArchivoSubido> archivos);
    }
} 