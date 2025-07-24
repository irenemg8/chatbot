using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public interface IServicioIA
    {
        /// <summary>
        /// Genera una respuesta usando IA conversacional
        /// </summary>
        /// <param name="mensaje">Mensaje del usuario</param>
        /// <param name="contextoArchivos">Contenido extraído de archivos como contexto</param>
        /// <param name="historialConversacion">Historial de la conversación</param>
        /// <returns>Respuesta generada por IA</returns>
        Task<string> GenerarRespuestaAsync(string mensaje, string contextoArchivos = "", List<MensajeChat>? historialConversacion = null);

        /// <summary>
        /// Analiza y responde preguntas específicas sobre archivos usando IA
        /// </summary>
        /// <param name="contenidoArchivos">Contenido extraído de los archivos</param>
        /// <param name="pregunta">Pregunta específica</param>
        /// <returns>Análisis inteligente usando IA</returns>
        Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta);

        /// <summary>
        /// Genera un resumen inteligente usando IA
        /// </summary>
        /// <param name="contenido">Contenido a resumir</param>
        /// <param name="tipoResumen">Tipo de resumen (ejecutivo, detallado, técnico)</param>
        /// <returns>Resumen generado por IA</returns>
        Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general");

        /// <summary>
        /// Configura la clave de API
        /// </summary>
        /// <param name="apiKey">Clave de API</param>
        void ConfigurarClave(string apiKey);

        /// <summary>
        /// Verifica si el servicio está disponible
        /// </summary>
        /// <returns>True si está configurado y disponible</returns>
        bool EstaDisponible();

        /// <summary>
        /// Genera sugerencias de preguntas basadas en el contenido
        /// </summary>
        /// <param name="contenidoArchivos">Contenido de los archivos</param>
        /// <returns>Lista de preguntas sugeridas</returns>
        Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos);
    }
} 