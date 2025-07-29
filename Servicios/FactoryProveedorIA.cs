using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Factory enterprise para gestión de múltiples proveedores de IA
    /// Implementa patrón Factory con soporte para configuración dinámica
    /// </summary>
    public class FactoryProveedorIA : IFactoryProveedorIA
    {
        private readonly ILogger<FactoryProveedorIA> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServicioConfiguracion _servicioConfiguracion;
        
        private readonly Dictionary<string, Type> _proveedoresRegistrados;
        private string _proveedorActivo = "openai"; // Default

        public FactoryProveedorIA(
            ILogger<FactoryProveedorIA> logger,
            IServiceProvider serviceProvider,
            IServicioConfiguracion servicioConfiguracion)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _servicioConfiguracion = servicioConfiguracion ?? throw new ArgumentNullException(nameof(servicioConfiguracion));
            
            // Registrar proveedores disponibles
            _proveedoresRegistrados = new Dictionary<string, Type>
            {
                ["openai"] = typeof(ServicioIAOpenAI),
                ["ollama"] = typeof(ServicioOllama)
                // Futuras expansiones: ["deepseek"] = typeof(ServicioDeepSeek)
                // ["claude"] = typeof(ServicioClaude)
            };
            
            InicializarProveedorActivo();
        }

        private void InicializarProveedorActivo()
        {
            try
            {
                // Leer configuración guardada del proveedor activo
                var configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "ChatbotGomarco", "proveedor_activo.txt");
                
                if (File.Exists(configFile))
                {
                    var proveedorGuardado = File.ReadAllText(configFile).Trim();
                    if (!string.IsNullOrWhiteSpace(proveedorGuardado) && 
                        _proveedoresRegistrados.ContainsKey(proveedorGuardado.ToLowerInvariant()))
                    {
                        _proveedorActivo = proveedorGuardado.ToLowerInvariant();
                    }
                }
                
                _logger.LogInformation("🔧 Proveedor IA activo configurado: {Proveedor}", _proveedorActivo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error leyendo configuración de proveedor, usando OpenAI por defecto");
                _proveedorActivo = "openai";
            }
        }

        public IProveedorIA ObtenerProveedorActivo()
        {
            return ObtenerProveedor(_proveedorActivo);
        }

        public string ObtenerIdProveedorActivo()
        {
            return _proveedorActivo;
        }

        public IProveedorIA ObtenerProveedor(string idProveedor)
        {
            if (string.IsNullOrWhiteSpace(idProveedor))
            {
                throw new ArgumentException("ID de proveedor no puede estar vacío", nameof(idProveedor));
            }

            var idNormalizado = idProveedor.ToLowerInvariant();
            
            if (!_proveedoresRegistrados.TryGetValue(idNormalizado, out var tipoProveedor))
            {
                _logger.LogError("❌ Proveedor no registrado: {IdProveedor}", idProveedor);
                throw new InvalidOperationException($"Proveedor '{idProveedor}' no está registrado");
            }

            try
            {
                // Obtener instancia del contenedor de DI
                var proveedor = _serviceProvider.GetRequiredService(tipoProveedor) as IProveedorIA;
                
                if (proveedor == null)
                {
                    throw new InvalidOperationException($"No se pudo crear instancia del proveedor '{idProveedor}'");
                }

                _logger.LogDebug("✅ Proveedor '{IdProveedor}' creado exitosamente", idProveedor);
                return proveedor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando proveedor '{IdProveedor}'", idProveedor);
                throw new InvalidOperationException($"Error creando proveedor '{idProveedor}': {ex.Message}", ex);
            }
        }

        public IEnumerable<IProveedorIA> ListarProveedores()
        {
            var proveedores = new List<IProveedorIA>();
            
            foreach (var kvp in _proveedoresRegistrados)
            {
                try
                {
                    var proveedor = ObtenerProveedor(kvp.Key);
                    proveedores.Add(proveedor);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ No se pudo obtener proveedor '{IdProveedor}'", kvp.Key);
                }
            }
            
            return proveedores;
        }

        public async Task CambiarProveedorActivoAsync(string idProveedor)
        {
            if (string.IsNullOrWhiteSpace(idProveedor))
            {
                throw new ArgumentException("ID de proveedor no puede estar vacío", nameof(idProveedor));
            }

            var idNormalizado = idProveedor.ToLowerInvariant();
            
            if (!_proveedoresRegistrados.ContainsKey(idNormalizado))
            {
                throw new InvalidOperationException($"Proveedor '{idProveedor}' no está registrado");
            }

            try
            {
                // Verificar que el nuevo proveedor esté disponible
                var nuevoProveedor = ObtenerProveedor(idNormalizado);
                var disponible = await nuevoProveedor.EstaDisponibleAsync();
                
                if (!disponible)
                {
                    _logger.LogWarning("⚠️ Proveedor '{IdProveedor}' no está disponible", idProveedor);
                    // No lanzar excepción, solo advertir - el usuario puede querer configurarlo
                }

                // Cambiar proveedor activo
                var proveedorAnterior = _proveedorActivo;
                _proveedorActivo = idNormalizado;
                
                // Guardar configuración
                try
                {
                    var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatbotGomarco");
                    Directory.CreateDirectory(configDir);
                    var configFile = Path.Combine(configDir, "proveedor_activo.txt");
                    await File.WriteAllTextAsync(configFile, _proveedorActivo);
                }
                catch (Exception configEx)
                {
                    _logger.LogWarning(configEx, "⚠️ No se pudo guardar configuración de proveedor");
                }
                
                _logger.LogInformation("🔄 Proveedor IA cambiado: {Anterior} → {Nuevo}", 
                    proveedorAnterior, _proveedorActivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cambiando proveedor IA a '{IdProveedor}'", idProveedor);
                throw new InvalidOperationException($"Error cambiando proveedor a '{idProveedor}': {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<string, EstadoProveedorIA>> ObtenerEstadoTodosProveedoresAsync()
        {
            var estados = new Dictionary<string, EstadoProveedorIA>();
            
            foreach (var kvp in _proveedoresRegistrados)
            {
                try
                {
                    var proveedor = ObtenerProveedor(kvp.Key);
                    var estado = await proveedor.ObtenerEstadoAsync();
                    estados[kvp.Key] = estado;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error obteniendo estado del proveedor '{IdProveedor}'", kvp.Key);
                    estados[kvp.Key] = EstadoProveedorIA.NoDisponible($"Error: {ex.Message}");
                }
            }
            
            return estados;
        }

        public async Task<Dictionary<string, bool>> VerificarDisponibilidadProveedoresAsync()
        {
            var disponibilidades = new Dictionary<string, bool>();
            
            var tasks = _proveedoresRegistrados.Keys.Select(async idProveedor =>
            {
                try
                {
                    var proveedor = ObtenerProveedor(idProveedor);
                    var disponible = await proveedor.EstaDisponibleAsync();
                    return new KeyValuePair<string, bool>(idProveedor, disponible);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Error verificando disponibilidad de '{IdProveedor}'", idProveedor);
                    return new KeyValuePair<string, bool>(idProveedor, false);
                }
            });

            var resultados = await Task.WhenAll(tasks);
            
            foreach (var resultado in resultados)
            {
                disponibilidades[resultado.Key] = resultado.Value;
            }
            
            return disponibilidades;
        }

        /// <summary>
        /// Registra un nuevo proveedor dinámicamente
        /// </summary>
        public void RegistrarProveedor<T>(string idProveedor) where T : class, IProveedorIA
        {
            if (string.IsNullOrWhiteSpace(idProveedor))
            {
                throw new ArgumentException("ID de proveedor no puede estar vacío", nameof(idProveedor));
            }

            var idNormalizado = idProveedor.ToLowerInvariant();
            _proveedoresRegistrados[idNormalizado] = typeof(T);
            
            _logger.LogInformation("📋 Proveedor '{IdProveedor}' registrado: {Tipo}", 
                idProveedor, typeof(T).Name);
        }

        /// <summary>
        /// Obtiene información de los proveedores registrados
        /// </summary>
        public Dictionary<string, string> ObtenerInformacionProveedores()
        {
            var informacion = new Dictionary<string, string>();
            
            foreach (var kvp in _proveedoresRegistrados)
            {
                try
                {
                    var proveedor = ObtenerProveedor(kvp.Key);
                    informacion[kvp.Key] = proveedor.NombreProveedor;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Error obteniendo información de '{IdProveedor}'", kvp.Key);
                    informacion[kvp.Key] = $"Error: {ex.Message}";
                }
            }
            
            return informacion;
        }
    }
} 