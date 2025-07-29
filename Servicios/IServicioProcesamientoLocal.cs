using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Interfaz para procesamiento local de datos sensibles sin transmisión externa
    /// Implementa zero data leakage para cumplimiento de políticas enterprise
    /// </summary>
    public interface IServicioProcesamientoLocal
    {
        /// <summary>
        /// Procesa mensajes con datos sensibles completamente en local
        /// Sin envío a servicios externos ni APIs de terceros
        /// </summary>
        /// <param name="mensaje">Mensaje del usuario (puede contener datos sensibles enmascarados)</param>
        /// <param name="contextoArchivos">Contenido de archivos (enmascarado)</param>
        /// <param name="historialConversacion">Historial previo de la conversación</param>
        /// <param name="tiposDatosSensibles">Tipos de datos sensibles detectados</param>
        /// <returns>Respuesta generada localmente sin exposición externa</returns>
        Task<string> GenerarRespuestaLocalAsync(
            string mensaje, 
            string contextoArchivos = "", 
            List<MensajeChat>? historialConversacion = null,
            List<string>? tiposDatosSensibles = null);

        /// <summary>
        /// Analiza contenido con datos sensibles usando algoritmos locales
        /// Enfocado en extracción de información sin procesamiento externo
        /// </summary>
        /// <param name="contenidoArchivos">Contenido enmascarado para análisis</param>
        /// <param name="pregunta">Pregunta específica del usuario</param>
        /// <param name="tiposDatosSensibles">Tipos de datos sensibles presentes</param>
        /// <returns>Análisis local del contenido</returns>
        Task<string> AnalizarContenidoLocalAsync(
            string contenidoArchivos, 
            string pregunta,
            List<string>? tiposDatosSensibles = null);

        /// <summary>
        /// Genera resumen de contenido sensible usando procesamiento local
        /// Sin envío de información a servicios externos
        /// </summary>
        /// <param name="contenido">Contenido enmascarado para resumir</param>
        /// <param name="tipoResumen">Tipo de resumen solicitado</param>
        /// <param name="tiposDatosSensibles">Tipos de datos sensibles detectados</param>
        /// <returns>Resumen generado localmente</returns>
        Task<string> GenerarResumenLocalAsync(
            string contenido, 
            string tipoResumen = "general",
            List<string>? tiposDatosSensibles = null);

        /// <summary>
        /// Genera sugerencias de preguntas basadas en análisis local
        /// Para contenido que contiene datos sensibles
        /// </summary>
        /// <param name="contenidoArchivos">Contenido enmascarado</param>
        /// <param name="tiposDatosSensibles">Tipos de datos sensibles presentes</param>
        /// <returns>Lista de preguntas sugeridas</returns>
        Task<List<string>> GenerarSugerenciasLocalesAsync(
            string contenidoArchivos,
            List<string>? tiposDatosSensibles = null);

        /// <summary>
        /// Verifica si el servicio de procesamiento local está disponible
        /// </summary>
        /// <returns>True si está disponible, false en caso contrario</returns>
        bool EstaDisponible();
    }
} 