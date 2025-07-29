using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChatbotGomarco.Datos;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public class ServicioArchivos : IServicioArchivos
    {
        private readonly ContextoBaseDatos _contexto;
        private readonly IServicioCifrado _servicioCifrado;
        private readonly ILogger<ServicioArchivos> _logger;
        
        private static readonly string DirectorioArchivosCifrados = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GOMARCO",
            "ChatbotGomarco",
            "ArchivosSegurosCifrados"
        );

        private static readonly string DirectorioArchivosTemporales = Path.Combine(
            Path.GetTempPath(),
            "ChatbotGomarco",
            "ArchivosTemporales"
        );

        public ServicioArchivos(
            ContextoBaseDatos contexto,
            IServicioCifrado servicioCifrado,
            ILogger<ServicioArchivos> logger)
        {
            _contexto = contexto;
            _servicioCifrado = servicioCifrado;
            _logger = logger;
            
            InicializarDirectorios();
        }

        public async Task<ArchivoSubido> SubirArchivoAsync(string rutaArchivo, string idSesionChat, string? descripcion = null)
        {
            try
            {
                _logger.LogInformation("Subiendo archivo: {Archivo} para sesión: {Sesion}", rutaArchivo, idSesionChat);

                if (!File.Exists(rutaArchivo))
                    throw new FileNotFoundException($"El archivo no existe: {rutaArchivo}");

                var infoArchivo = new FileInfo(rutaArchivo);
                var nombreArchivo = Path.GetFileName(rutaArchivo);
                var hashOriginal = await _servicioCifrado.CalcularHashArchivoAsync(rutaArchivo);
                
                // Generar nombre único para archivo cifrado
                var nombreArchivoCifrado = $"{Guid.NewGuid()}.enc";
                var rutaArchivoCifrado = Path.Combine(DirectorioArchivosCifrados, nombreArchivoCifrado);

                // Cifrar archivo
                var vectorInicializacion = await _servicioCifrado.CifrarArchivoAsync(rutaArchivo, rutaArchivoCifrado);

                // Crear registro en base de datos
                var archivoSubido = new ArchivoSubido
                {
                    IdSesionChat = idSesionChat,
                    NombreOriginal = nombreArchivo,
                    RutaArchivoCifrado = rutaArchivoCifrado,
                    HashSha256 = hashOriginal,
                    TamañoOriginal = infoArchivo.Length,
                    TipoContenido = ObtenerTipoContenido(nombreArchivo),
                    Descripcion = descripcion,
                    VectorInicializacion = vectorInicializacion,
                    EstaCifrado = true
                };

                _contexto.ArchivosSubidos.Add(archivoSubido);
                await _contexto.SaveChangesAsync();

                _logger.LogInformation("Archivo subido y cifrado exitosamente: {Id}", archivoSubido.Id);
                return archivoSubido;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo: {Archivo}", rutaArchivo);
                throw;
            }
        }

        public async Task<List<ArchivoSubido>> ObtenerArchivosSesionAsync(string idSesionChat)
        {
            return await _contexto.ArchivosSubidos
                .Where(a => a.IdSesionChat == idSesionChat)
                .OrderByDescending(a => a.FechaSubida)
                .ToListAsync();
        }

        public async Task EliminarArchivoAsync(int idArchivo)
        {
            try
            {
                var archivo = await _contexto.ArchivosSubidos.FindAsync(idArchivo);
                if (archivo == null)
                {
                    _logger.LogWarning("Intento de eliminar archivo inexistente: {Id}", idArchivo);
                    return;
                }

                // Eliminar archivo físico cifrado
                if (File.Exists(archivo.RutaArchivoCifrado))
                {
                    File.Delete(archivo.RutaArchivoCifrado);
                }

                // Eliminar registro de base de datos
                _contexto.ArchivosSubidos.Remove(archivo);
                await _contexto.SaveChangesAsync();

                _logger.LogInformation("Archivo eliminado exitosamente: {Id}", idArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar archivo: {Id}", idArchivo);
                throw;
            }
        }

        public async Task<string> DescargarArchivoTemporalAsync(int idArchivo)
        {
            try
            {
                var archivo = await _contexto.ArchivosSubidos.FindAsync(idArchivo);
                if (archivo == null)
                    throw new ArgumentException($"Archivo no encontrado: {idArchivo}");

                var rutaTemporal = Path.Combine(DirectorioArchivosTemporales, 
                    $"{Guid.NewGuid()}_{archivo.NombreOriginal}");

                await _servicioCifrado.DescifrarArchivoAsync(
                    archivo.RutaArchivoCifrado, 
                    rutaTemporal, 
                    archivo.VectorInicializacion);

                _logger.LogInformation("Archivo temporal creado: {Ruta}", rutaTemporal);
                return rutaTemporal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear archivo temporal: {Id}", idArchivo);
                throw;
            }
        }

        public async Task<ArchivoSubido?> ObtenerArchivoAsync(int idArchivo)
        {
            return await _contexto.ArchivosSubidos.FindAsync(idArchivo);
        }

        public async Task<bool> VerificarIntegridadArchivoAsync(int idArchivo)
        {
            try
            {
                var archivo = await _contexto.ArchivosSubidos.FindAsync(idArchivo);
                if (archivo == null || !File.Exists(archivo.RutaArchivoCifrado))
                    return false;

                var rutaTemporal = await DescargarArchivoTemporalAsync(idArchivo);
                var hashActual = await _servicioCifrado.CalcularHashArchivoAsync(rutaTemporal);
                
                // Limpiar archivo temporal
                if (File.Exists(rutaTemporal))
                    File.Delete(rutaTemporal);

                return hashActual.Equals(archivo.HashSha256, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar integridad del archivo: {Id}", idArchivo);
                return false;
            }
        }

        public async Task LimpiarArchivosTemporalesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(DirectorioArchivosTemporales))
                        return;

                    var archivos = Directory.GetFiles(DirectorioArchivosTemporales);
                    var eliminados = 0;

                    foreach (var archivo in archivos)
                    {
                        try
                        {
                            var infoArchivo = new FileInfo(archivo);
                            // Eliminar archivos temporales más antiguos de 1 hora
                            if (DateTime.Now - infoArchivo.CreationTime > TimeSpan.FromHours(1))
                            {
                                File.Delete(archivo);
                                eliminados++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "No se pudo eliminar archivo temporal: {Archivo}", archivo);
                        }
                    }

                    if (eliminados > 0)
                    {
                        _logger.LogInformation("Archivos temporales limpiados: {Cantidad}", eliminados);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al limpiar archivos temporales");
                }
            });
        }

        private void InicializarDirectorios()
        {
            Directory.CreateDirectory(DirectorioArchivosCifrados);
            Directory.CreateDirectory(DirectorioArchivosTemporales);
        }

        private static string ObtenerTipoContenido(string nombreArchivo)
        {
            var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
            return extension switch
            {
                // Documentos
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".rtf" => "application/rtf",
                ".odt" => "application/vnd.oasis.opendocument.text",
                ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
                ".odp" => "application/vnd.oasis.opendocument.presentation",
                
                // Texto
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".xml" => "application/xml",
                
                // Imágenes
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".tiff" or ".tif" => "image/tiff",
                
                // Audio
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".aac" => "audio/aac",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".flac" => "audio/flac",
                
                // Video
                ".mp4" => "video/mp4",
                ".avi" => "video/avi",
                ".mkv" => "video/x-matroska",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                ".webm" => "video/webm",
                ".m4v" => "video/x-m4v",
                
                // Comprimidos
                ".zip" => "application/zip",
                ".rar" => "application/vnd.rar",
                ".7z" => "application/x-7z-compressed",
                ".tar" => "application/x-tar",
                ".gz" => "application/gzip",
                
                _ => "application/octet-stream"
            };
        }
    }
} 