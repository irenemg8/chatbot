using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatbotGomarco.Datos;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public class ServicioHistorialChats : IServicioHistorialChats
    {
        private readonly ContextoBaseDatos _contexto;
        private readonly ILogger<ServicioHistorialChats> _logger;

        public ServicioHistorialChats(ContextoBaseDatos contexto, ILogger<ServicioHistorialChats> logger)
        {
            _contexto = contexto;
            _logger = logger;
        }

        public async Task<SesionChat> CrearNuevaSesionAsync(string titulo = "Nueva Conversación")
        {
            try
            {
                var sesion = new SesionChat
                {
                    Titulo = titulo,
                    FechaCreacion = DateTime.Now,
                    FechaUltimaActividad = DateTime.Now
                };

                _contexto.SesionesChat.Add(sesion);
                await _contexto.SaveChangesAsync();

                _logger.LogInformation("Nueva sesión creada: {Id} - {Titulo}", sesion.Id, sesion.Titulo);
                return sesion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear nueva sesión");
                throw;
            }
        }

        public async Task<List<SesionChat>> ObtenerTodasLasSesionesAsync()
        {
            try
            {
                return await _contexto.SesionesChat
                    .Include(s => s.Mensajes)
                    .Include(s => s.ArchivosSubidos)
                    .OrderByDescending(s => s.FechaUltimaActividad)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesiones");
                throw;
            }
        }

        public async Task<SesionChat?> ObtenerSesionComplentaAsync(string idSesion)
        {
            try
            {
                return await _contexto.SesionesChat
                    .Include(s => s.Mensajes.OrderBy(m => m.FechaCreacion))
                    .Include(s => s.ArchivosSubidos)
                    .FirstOrDefaultAsync(s => s.Id == idSesion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesión: {Id}", idSesion);
                throw;
            }
        }

        public async Task<MensajeChat> AgregarMensajeAsync(string idSesion, string contenido, TipoMensaje tipoMensaje, 
            string? rutaArchivo = null, string? nombreArchivo = null)
        {
            try
            {
                var mensaje = new MensajeChat
                {
                    IdSesionChat = idSesion,
                    Contenido = contenido,
                    TipoMensaje = tipoMensaje,
                    FechaCreacion = DateTime.Now,
                    ArchivoAdjuntoRuta = rutaArchivo,
                    NombreArchivoAdjunto = nombreArchivo,
                    EsArchivoCifrado = !string.IsNullOrEmpty(rutaArchivo)
                };

                _contexto.MensajesChat.Add(mensaje);

                // Actualizar última actividad de la sesión
                await ActualizarUltimaActividadAsync(idSesion);

                await _contexto.SaveChangesAsync();

                _logger.LogInformation("Mensaje agregado a sesión {Sesion}: {Tipo}", idSesion, tipoMensaje);
                return mensaje;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar mensaje a sesión: {Id}", idSesion);
                throw;
            }
        }

        public async Task ActualizarTituloSesionAsync(string idSesion, string nuevoTitulo)
        {
            try
            {
                var sesion = await _contexto.SesionesChat.FindAsync(idSesion);
                if (sesion != null)
                {
                    sesion.Titulo = nuevoTitulo;
                    await _contexto.SaveChangesAsync();
                    _logger.LogInformation("Título actualizado para sesión {Id}: {Titulo}", idSesion, nuevoTitulo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar título de sesión: {Id}", idSesion);
                throw;
            }
        }

        public async Task EliminarSesionAsync(string idSesion)
        {
            try
            {
                var sesion = await _contexto.SesionesChat
                    .Include(s => s.Mensajes)
                    .Include(s => s.ArchivosSubidos)
                    .FirstOrDefaultAsync(s => s.Id == idSesion);

                if (sesion != null)
                {
                    _contexto.SesionesChat.Remove(sesion);
                    await _contexto.SaveChangesAsync();
                    _logger.LogInformation("Sesión eliminada: {Id}", idSesion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar sesión: {Id}", idSesion);
                throw;
            }
        }

        public async Task<List<MensajeChat>> BuscarEnHistorialAsync(string terminoBusqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(terminoBusqueda))
                    return new List<MensajeChat>();

                return await _contexto.MensajesChat
                    .Where(m => m.Contenido.Contains(terminoBusqueda))
                    .OrderByDescending(m => m.FechaCreacion)
                    .Take(50) // Limitar resultados
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar en historial: {Termino}", terminoBusqueda);
                throw;
            }
        }

        public async Task<EstadisticasHistorial> ObtenerEstadisticasAsync()
        {
            try
            {
                var totalSesiones = await _contexto.SesionesChat.CountAsync();
                var totalMensajes = await _contexto.MensajesChat.CountAsync();
                var totalArchivos = await _contexto.ArchivosSubidos.CountAsync();
                var tamañoTotalArchivos = await _contexto.ArchivosSubidos.SumAsync(a => a.TamañoOriginal);

                var fechaPrimerChat = await _contexto.SesionesChat
                    .OrderBy(s => s.FechaCreacion)
                    .Select(s => s.FechaCreacion)
                    .FirstOrDefaultAsync();

                var fechaUltimoChat = await _contexto.SesionesChat
                    .OrderByDescending(s => s.FechaUltimaActividad)
                    .Select(s => s.FechaUltimaActividad)
                    .FirstOrDefaultAsync();

                return new EstadisticasHistorial
                {
                    TotalSesiones = totalSesiones,
                    TotalMensajes = totalMensajes,
                    TotalArchivos = totalArchivos,
                    FechaPrimerChat = fechaPrimerChat,
                    FechaUltimoChat = fechaUltimoChat,
                    TamañoTotalArchivos = tamañoTotalArchivos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                throw;
            }
        }

        public async Task ActualizarUltimaActividadAsync(string idSesion)
        {
            try
            {
                var sesion = await _contexto.SesionesChat.FindAsync(idSesion);
                if (sesion != null)
                {
                    sesion.FechaUltimaActividad = DateTime.Now;
                    await _contexto.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar última actividad: {Id}", idSesion);
                throw;
            }
        }
    }
} 