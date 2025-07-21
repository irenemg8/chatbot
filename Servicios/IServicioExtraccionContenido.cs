using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public interface IServicioExtraccionContenido
    {
        /// <summary>
        /// Extrae el texto completo de un archivo según su tipo
        /// </summary>
        /// <param name="rutaArchivo">Ruta del archivo a analizar</param>
        /// <param name="tipoContenido">Tipo MIME del archivo</param>
        /// <returns>Texto extraído del documento</returns>
        Task<string> ExtraerTextoAsync(string rutaArchivo, string tipoContenido);

        /// <summary>
        /// Extrae metadatos específicos de un archivo
        /// </summary>
        /// <param name="rutaArchivo">Ruta del archivo</param>
        /// <param name="tipoContenido">Tipo MIME del archivo</param>
        /// <returns>Metadatos del documento</returns>
        Task<DocumentoMetadatos> ExtraerMetadatosAsync(string rutaArchivo, string tipoContenido);

        /// <summary>
        /// Analiza la estructura de un documento
        /// </summary>
        /// <param name="rutaArchivo">Ruta del archivo</param>
        /// <param name="tipoContenido">Tipo MIME del archivo</param>
        /// <returns>Análisis estructural del documento</returns>
        Task<DocumentoEstructura> AnalizarEstructuraAsync(string rutaArchivo, string tipoContenido);

        /// <summary>
        /// Verifica si un tipo de archivo es compatible para extracción
        /// </summary>
        /// <param name="tipoContenido">Tipo MIME del archivo</param>
        /// <returns>True si es compatible</returns>
        bool EsTipoCompatible(string tipoContenido);
    }

    /// <summary>
    /// Metadatos extraídos de un documento
    /// </summary>
    public class DocumentoMetadatos
    {
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string Tema { get; set; } = string.Empty;
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int NumeroPaginas { get; set; }
        public int NumeroPalabras { get; set; }
        public string Idioma { get; set; } = string.Empty;
        public string Aplicacion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Estructura analizada de un documento
    /// </summary>
    public class DocumentoEstructura
    {
        public List<string> Encabezados { get; set; } = new();
        public List<string> TablaContenidos { get; set; } = new();
        public int NumeroTablas { get; set; }
        public int NumeroImagenes { get; set; }
        public int NumeroEnlaces { get; set; }
        public List<string> SeccionesPrincipales { get; set; } = new();
        public string ResumenEstructural { get; set; } = string.Empty;
    }
} 