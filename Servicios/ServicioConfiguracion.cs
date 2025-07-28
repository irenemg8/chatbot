using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio para gestionar configuraciones persistentes de la aplicación
    /// Guarda la API key de forma cifrada en el sistema de archivos local
    /// </summary>
    public class ServicioConfiguracion : IServicioConfiguracion
    {
        private readonly ILogger<ServicioConfiguracion> _logger;
        private readonly string _rutaConfiguracion;
        private readonly string _archivoClaveAPI;

        public ServicioConfiguracion(ILogger<ServicioConfiguracion> logger)
        {
            _logger = logger;
            
            // Crear carpeta de configuración en AppData del usuario
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _rutaConfiguracion = Path.Combine(appData, "GOMARCO", "ChatbotGomarco");
            _archivoClaveAPI = Path.Combine(_rutaConfiguracion, "config.dat");
            
            // Crear directorio si no existe
            Directory.CreateDirectory(_rutaConfiguracion);
        }

        public async Task GuardarClaveAPIAsync(string claveAPI)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(claveAPI))
                {
                    throw new ArgumentException("La clave API no puede estar vacía");
                }

                // Cifrar la clave API antes de guardarla
                var clavesCifradas = CifrarTexto(claveAPI);
                await File.WriteAllTextAsync(_archivoClaveAPI, clavesCifradas);
                
                _logger.LogInformation("Clave API guardada de forma persistente y cifrada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la clave API");
                throw;
            }
        }

        public async Task<string?> ObtenerClaveAPIAsync()
        {
            try
            {
                if (!File.Exists(_archivoClaveAPI))
                {
                    return null;
                }

                var textoCifrado = await File.ReadAllTextAsync(_archivoClaveAPI);
                if (string.IsNullOrWhiteSpace(textoCifrado))
                {
                    return null;
                }

                // Descifrar la clave API
                var claveDescifrada = DescifrarTexto(textoCifrado);
                
                _logger.LogInformation("Clave API cargada desde configuración persistente");
                return claveDescifrada;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al cargar la clave API guardada");
                return null;
            }
        }

        public async Task EliminarClaveAPIAsync()
        {
            try
            {
                if (File.Exists(_archivoClaveAPI))
                {
                    File.Delete(_archivoClaveAPI);
                    _logger.LogInformation("Clave API eliminada de la configuración persistente");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la clave API");
                throw;
            }
            
            await Task.CompletedTask;
        }

        public async Task<bool> TieneClaveAPIAsync()
        {
            try
            {
                if (!File.Exists(_archivoClaveAPI))
                {
                    return false;
                }

                var contenido = await File.ReadAllTextAsync(_archivoClaveAPI);
                return !string.IsNullOrWhiteSpace(contenido);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al verificar existencia de clave API");
                return false;
            }
        }

        /// <summary>
        /// Cifra un texto usando protección de datos del usuario actual
        /// </summary>
        private string CifrarTexto(string textoPlano)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(textoPlano);
                var bytesCifrados = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(bytesCifrados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cifrar el texto");
                throw;
            }
        }

        /// <summary>
        /// Descifra un texto usando protección de datos del usuario actual
        /// </summary>
        private string DescifrarTexto(string textoCifrado)
        {
            try
            {
                var bytesCifrados = Convert.FromBase64String(textoCifrado);
                var bytes = ProtectedData.Unprotect(bytesCifrados, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descifrar el texto");
                throw;
            }
        }
    }
} 