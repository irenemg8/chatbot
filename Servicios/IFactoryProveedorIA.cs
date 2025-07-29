using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Factory para crear y gestionar proveedores de IA
    /// Proporciona una abstracción para el manejo de múltiples servicios de IA
    /// </summary>
    public interface IFactoryProveedorIA
    {
        /// <summary>
        /// Obtiene el proveedor de IA actualmente configurado
        /// </summary>
        /// <returns>Instancia del proveedor activo</returns>
        IProveedorIA ObtenerProveedorActivo();
        
        /// <summary>
        /// Obtiene el ID del proveedor actualmente configurado
        /// </summary>
        /// <returns>ID del proveedor activo (openai, ollama, etc.)</returns>
        string ObtenerIdProveedorActivo();
        
        /// <summary>
        /// Obtiene un proveedor específico por su ID
        /// </summary>
        /// <param name="idProveedor">ID del proveedor (openai, ollama, deepseek, etc.)</param>
        /// <returns>Instancia del proveedor solicitado</returns>
        IProveedorIA ObtenerProveedor(string idProveedor);
        
        /// <summary>
        /// Lista todos los proveedores disponibles en el sistema
        /// </summary>
        /// <returns>Lista de proveedores registrados</returns>
        IEnumerable<IProveedorIA> ListarProveedores();
        
        /// <summary>
        /// Cambia el proveedor activo
        /// </summary>
        /// <param name="idProveedor">ID del nuevo proveedor activo</param>
        Task CambiarProveedorActivoAsync(string idProveedor);
        
        /// <summary>
        /// Obtiene el estado de todos los proveedores
        /// </summary>
        Task<Dictionary<string, EstadoProveedorIA>> ObtenerEstadoTodosProveedoresAsync();
        
        /// <summary>
        /// Verifica qué proveedores están disponibles y configurados
        /// </summary>
        Task<Dictionary<string, bool>> VerificarDisponibilidadProveedoresAsync();
    }
} 