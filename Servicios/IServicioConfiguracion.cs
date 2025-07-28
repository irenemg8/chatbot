using System.Threading.Tasks;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio para gestionar configuraciones persistentes de la aplicación
    /// </summary>
    public interface IServicioConfiguracion
    {
        /// <summary>
        /// Guarda la clave API de OpenAI de forma persistente
        /// </summary>
        Task GuardarClaveAPIAsync(string claveAPI);

        /// <summary>
        /// Obtiene la clave API guardada
        /// </summary>
        Task<string?> ObtenerClaveAPIAsync();

        /// <summary>
        /// Elimina la clave API guardada
        /// </summary>
        Task EliminarClaveAPIAsync();

        /// <summary>
        /// Verifica si existe una clave API guardada
        /// </summary>
        Task<bool> TieneClaveAPIAsync();
    }
} 