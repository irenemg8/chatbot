using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public interface IServicioHistorialChats
    {
        /// <summary>
        /// Crea una nueva sesión de chat
        /// </summary>
        /// <param name="titulo">Título de la sesión</param>
        /// <returns>Nueva sesión creada</returns>
        Task<SesionChat> CrearNuevaSesionAsync(string titulo = "Nueva Conversación");

        /// <summary>
        /// Obtiene todas las sesiones de chat ordenadas por fecha
        /// </summary>
        /// <returns>Lista de sesiones</returns>
        Task<List<SesionChat>> ObtenerTodasLasSesionesAsync();

        /// <summary>
        /// Obtiene una sesión específica con sus mensajes
        /// </summary>
        /// <param name="idSesion">ID de la sesión</param>
        /// <returns>Sesión con mensajes cargados</returns>
        Task<SesionChat?> ObtenerSesionComplentaAsync(string idSesion);

        /// <summary>
        /// Agrega un mensaje a una sesión
        /// </summary>
        /// <param name="idSesion">ID de la sesión</param>
        /// <param name="contenido">Contenido del mensaje</param>
        /// <param name="tipoMensaje">Tipo de mensaje (Usuario/Asistente/Sistema)</param>
        /// <param name="rutaArchivo">Ruta opcional del archivo adjunto</param>
        /// <param name="nombreArchivo">Nombre opcional del archivo adjunto</param>
        /// <returns>Mensaje creado</returns>
        Task<MensajeChat> AgregarMensajeAsync(string idSesion, string contenido, TipoMensaje tipoMensaje, 
            string? rutaArchivo = null, string? nombreArchivo = null);

        /// <summary>
        /// Actualiza el título de una sesión
        /// </summary>
        /// <param name="idSesion">ID de la sesión</param>
        /// <param name="nuevoTitulo">Nuevo título</param>
        Task ActualizarTituloSesionAsync(string idSesion, string nuevoTitulo);

        /// <summary>
        /// Elimina una sesión y todos sus mensajes asociados
        /// </summary>
        /// <param name="idSesion">ID de la sesión</param>
        Task EliminarSesionAsync(string idSesion);

        /// <summary>
        /// Busca en el historial de mensajes
        /// </summary>
        /// <param name="terminoBusqueda">Término a buscar</param>
        /// <returns>Lista de mensajes que contienen el término</returns>
        Task<List<MensajeChat>> BuscarEnHistorialAsync(string terminoBusqueda);

        /// <summary>
        /// Obtiene estadísticas del historial
        /// </summary>
        /// <returns>Estadísticas de uso</returns>
        Task<EstadisticasHistorial> ObtenerEstadisticasAsync();

        /// <summary>
        /// Actualiza la fecha de última actividad de una sesión
        /// </summary>
        /// <param name="idSesion">ID de la sesión</param>
        Task ActualizarUltimaActividadAsync(string idSesion);
    }

    public class EstadisticasHistorial
    {
        public int TotalSesiones { get; set; }
        public int TotalMensajes { get; set; }
        public int TotalArchivos { get; set; }
        public DateTime FechaPrimerChat { get; set; }
        public DateTime FechaUltimoChat { get; set; }
        public long TamañoTotalArchivos { get; set; }
    }
} 