using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public interface IServicioArchivos
    {
        /// <summary>
        /// Sube y cifra un archivo para una sesión de chat
        /// </summary>
        /// <param name="rutaArchivo">Ruta del archivo a subir</param>
        /// <param name="idSesionChat">ID de la sesión de chat</param>
        /// <param name="descripcion">Descripción opcional del archivo</param>
        /// <returns>Información del archivo subido</returns>
        Task<ArchivoSubido> SubirArchivoAsync(string rutaArchivo, string idSesionChat, string? descripcion = null);

        /// <summary>
        /// Obtiene la lista de archivos de una sesión
        /// </summary>
        /// <param name="idSesionChat">ID de la sesión de chat</param>
        /// <returns>Lista de archivos subidos</returns>
        Task<List<ArchivoSubido>> ObtenerArchivosSesionAsync(string idSesionChat);

        /// <summary>
        /// Elimina un archivo cifrado del sistema
        /// </summary>
        /// <param name="idArchivo">ID del archivo a eliminar</param>
        Task EliminarArchivoAsync(int idArchivo);

        /// <summary>
        /// Descarga y descifra un archivo temporalmente para uso
        /// </summary>
        /// <param name="idArchivo">ID del archivo</param>
        /// <returns>Ruta del archivo temporal descifrado</returns>
        Task<string> DescargarArchivoTemporalAsync(int idArchivo);

        /// <summary>
        /// Obtiene información de un archivo específico
        /// </summary>
        /// <param name="idArchivo">ID del archivo</param>
        /// <returns>Información del archivo</returns>
        Task<ArchivoSubido?> ObtenerArchivoAsync(int idArchivo);

        /// <summary>
        /// Verifica la integridad de un archivo cifrado
        /// </summary>
        /// <param name="idArchivo">ID del archivo</param>
        /// <returns>True si el archivo está íntegro</returns>
        Task<bool> VerificarIntegridadArchivoAsync(int idArchivo);

        /// <summary>
        /// Limpia archivos temporales antiguos
        /// </summary>
        Task LimpiarArchivosTemporalesAsync();
    }
} 