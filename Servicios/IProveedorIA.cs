using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Interfaz base para todos los proveedores de IA del sistema
    /// Define el contrato estándar que deben cumplir todos los servicios de IA
    /// </summary>
    public interface IProveedorIA
    {
        /// <summary>
        /// Identificador único del proveedor (ej: "openai", "ollama", "deepseek")
        /// </summary>
        string IdProveedor { get; }
        
        /// <summary>
        /// Nombre visible del proveedor para la UI
        /// </summary>
        string NombreProveedor { get; }
        
        /// <summary>
        /// Indica si el proveedor requiere API Key
        /// </summary>
        bool RequiereApiKey { get; }
        
        /// <summary>
        /// Indica si el proveedor está disponible y configurado correctamente
        /// </summary>
        Task<bool> EstaDisponibleAsync();
        
        /// <summary>
        /// Verifica si la configuración del proveedor es válida
        /// </summary>
        Task<bool> ValidarConfiguracionAsync();
        
        /// <summary>
        /// Configura el proveedor con los parámetros necesarios
        /// </summary>
        Task ConfigurarAsync(Dictionary<string, string> configuracion);
        
        /// <summary>
        /// Obtiene información sobre el estado del proveedor
        /// </summary>
        Task<EstadoProveedorIA> ObtenerEstadoAsync();
        
        /// <summary>
        /// Procesa una consulta de chat básica
        /// </summary>
        Task<string> ProcesarChatAsync(string mensaje, List<SesionChat>? historial = null);
        
        /// <summary>
        /// Analiza contenido de archivos con contexto
        /// </summary>
        Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta);
        
        /// <summary>
        /// Genera resumen inteligente del contenido
        /// </summary>
        Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general");
        
        /// <summary>
        /// Genera sugerencias de preguntas basadas en el contenido
        /// </summary>
        Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos);
    }
} 