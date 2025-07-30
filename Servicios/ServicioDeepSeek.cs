using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio para DeepSeek-R1 7B usando Ollama como backend
    /// Especializado en razonamiento avanzado y análisis lógico
    /// </summary>
    public class ServicioDeepSeek : IProveedorIA
    {
        private readonly ILogger<ServicioDeepSeek> _logger;
        private readonly ServicioOllama _servicioOllama;
        
        public string NombreProveedor => "DeepSeek-R1 7B (Razonamiento Avanzado)";
        public string IdProveedor => "deepseek";
        public bool RequiereApiKey => false;
        
        // Modelos DeepSeek preferidos en orden de prioridad
        private readonly string[] _modelosDeepSeek = {
            "deepseek-r1:7b",
            "deepseek-r1:latest", 
            "deepseek-v3:latest",
            "deepseek:latest"
        };

        public ServicioDeepSeek(ILogger<ServicioDeepSeek> logger, ServicioOllama servicioOllama)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _servicioOllama = servicioOllama ?? throw new ArgumentNullException(nameof(servicioOllama));
        }

        public async Task<bool> EstaDisponibleAsync()
        {
            try
            {
                // Verificar que Ollama esté disponible
                if (!await _servicioOllama.EstaDisponibleAsync())
                {
                    _logger.LogWarning("🔍 DeepSeek no disponible: Ollama no está ejecutándose");
                    return false;
                }

                // Verificar que al menos un modelo DeepSeek esté disponible
                foreach (var modelo in _modelosDeepSeek)
                {
                    if (await _servicioOllama.VerificarModeloDisponibleAsync(modelo))
                    {
                        _logger.LogInformation("✅ DeepSeek disponible con modelo: {Modelo}", modelo);
                        return true;
                    }
                }

                _logger.LogWarning("⚠️ DeepSeek no disponible: Ningún modelo DeepSeek encontrado");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error verificando disponibilidad de DeepSeek");
                return false;
            }
        }

        public async Task<EstadoProveedorIA> ObtenerEstadoAsync()
        {
            try
            {
                if (!await EstaDisponibleAsync())
                {
                    return EstadoProveedorIA.NoDisponible("DeepSeek requiere Ollama y modelos DeepSeek instalados");
                }

                // Obtener el modelo activo
                var modeloActivo = await ObtenerModeloActivoAsync();
                var mensaje = $"DeepSeek activo con {modeloActivo} - Especializado en razonamiento avanzado";
                
                return EstadoProveedorIA.Disponible(mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo estado de DeepSeek");
                return EstadoProveedorIA.NoDisponible($"Error: {ex.Message}");
            }
        }

        public async Task<string> GenerarRespuestaAsync(string mensaje, List<MensajeChat>? historial = null)
        {
            try
            {
                // Verificar disponibilidad antes de procesar
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("DeepSeek no está disponible");
                }

                // Obtener modelo DeepSeek activo
                var modeloActivo = await ObtenerModeloActivoAsync();
                
                // Preparar prompt optimizado para DeepSeek (razonamiento)
                var promptOptimizado = OptimizarPromptParaDeepSeek(mensaje);
                
                _logger.LogInformation("🧠 Procesando con DeepSeek-R1 - Modo razonamiento avanzado");
                
                // Delegar a Ollama con el modelo específico
                return await _servicioOllama.GenerarRespuestaConModeloAsync(promptOptimizado, modeloActivo, historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando respuesta con DeepSeek");
                throw new InvalidOperationException($"Error en DeepSeek: {ex.Message}", ex);
            }
        }

        private async Task<string> ObtenerModeloActivoAsync()
        {
            foreach (var modelo in _modelosDeepSeek)
            {
                if (await _servicioOllama.VerificarModeloDisponibleAsync(modelo))
                {
                    return modelo;
                }
            }
            
            throw new InvalidOperationException("No hay modelos DeepSeek disponibles");
        }

        public async Task<bool> ValidarConfiguracionAsync()
        {
            return await EstaDisponibleAsync();
        }

        public async Task ConfigurarAsync(Dictionary<string, string> configuracion)
        {
            // DeepSeek usa Ollama como backend, delegar configuración
            await _servicioOllama.ConfigurarAsync(configuracion);
        }

        public async Task<string> ProcesarChatAsync(string mensaje, List<SesionChat>? historial = null)
        {
            try
            {
                // Verificar disponibilidad
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("DeepSeek no está disponible");
                }

                // Optimizar prompt para razonamiento
                var promptOptimizado = OptimizarPromptParaDeepSeek(mensaje);
                
                // Delegar a Ollama
                return await _servicioOllama.ProcesarChatAsync(promptOptimizado, historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en ProcesarChatAsync con DeepSeek");
                throw new InvalidOperationException($"Error en DeepSeek: {ex.Message}", ex);
            }
        }

        public async Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta)
        {
            try
            {
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("DeepSeek no está disponible");
                }

                // 🆕 DETECCIÓN AUTOMÁTICA DE FACTURAS  
                var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
                var analizadorFacturas = new AnalizadorFacturas(loggerFactory.CreateLogger<AnalizadorFacturas>());
                
                if (analizadorFacturas.EsFactura(contenidoArchivos))
                {
                    _logger.LogInformation("🧠📄 DeepSeek detectó FACTURA - Activando análisis especializado");
                    
                    // Usar análisis especializado de facturas
                    var analisisFactura = await analizadorFacturas.AnalizarFacturaAsync(contenidoArchivos, pregunta);
                    
                    // Si el análisis automático fue exitoso, usar prompt optimizado
                    if (analisisFactura.EsFacturaValida)
                    {
                        var promptEspecializado = analizadorFacturas.GenerarPromptAnalisisFactura(
                            contenidoArchivos, pregunta, TipoProveedorIA.DeepSeek);
                        
                        var analisisIA = await _servicioOllama.AnalizarContenidoConIAAsync(contenidoArchivos, promptEspecializado);
                        
                        // Combinar análisis estructurado + análisis de IA
                        return $@"{analisisFactura.AnalisisCompleto}

---

🧠 **ANÁLISIS AVANZADO DEEPSEEK-R1:**

{analisisIA}";
                    }
                }

                // Análisis genérico mejorado para otros tipos de documentos
                var promptAnalisis = $@"[ANÁLISIS AVANZADO CON DEEPSEEK-R1]

Analiza el siguiente contenido usando razonamiento paso a paso:

CONTENIDO:
{contenidoArchivos}

PREGUNTA ESPECÍFICA:
{pregunta}

INSTRUCCIONES DE ANÁLISIS:
- Examina el contenido sistemáticamente
- Identifica patrones y relaciones clave  
- Responde la pregunta con razonamiento detallado
- Proporciona insights valiosos basados en los datos

ANÁLISIS:";

                return await _servicioOllama.AnalizarContenidoConIAAsync(contenidoArchivos, promptAnalisis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error analizando contenido con DeepSeek");
                throw new InvalidOperationException($"Error en análisis DeepSeek: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general")
        {
            try
            {
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("DeepSeek no está disponible");
                }

                var instruccionesResumen = tipoResumen.ToLower() switch
                {
                    "ejecutivo" => "Crea un resumen ejecutivo conciso enfocado en decisiones estratégicas y puntos clave para liderazgo.",
                    "tecnico" => "Genera un resumen técnico detallado con análisis profundo y especificaciones relevantes.",
                    "analisis" => "Proporciona un resumen analítico con razonamiento paso a paso y conclusiones fundamentadas.",
                    _ => "Crea un resumen equilibrado con análisis lógico y puntos principales bien estructurados."
                };

                return await _servicioOllama.GenerarResumenInteligente(contenido, instruccionesResumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando resumen con DeepSeek");
                throw new InvalidOperationException($"Error en resumen DeepSeek: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos)
        {
            try
            {
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("DeepSeek no está disponible");
                }

                return await _servicioOllama.GenerarSugerenciasPreguntasAsync(contenidoArchivos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando sugerencias con DeepSeek");
                return new List<string>
                {
                    "¿Puedes analizar los datos principales?",
                    "¿Qué patrones o tendencias identificas?",
                    "¿Cuáles son las conclusiones más importantes?"
                };
            }
        }

        private string OptimizarPromptParaDeepSeek(string mensajeOriginal)
        {
            // DeepSeek-R1 es excelente para razonamiento, así que optimizamos el prompt
            return $@"[MODO RAZONAMIENTO AVANZADO - DeepSeek-R1]

Por favor, analiza la siguiente consulta paso a paso usando razonamiento lógico:

CONSULTA: {mensajeOriginal}

INSTRUCCIONES:
- Descompone el problema en pasos lógicos
- Analiza cada componente sistemáticamente  
- Proporciona reasoning claro y detallado
- Concluye con una respuesta bien fundamentada

RESPUESTA:";
        }
    }
}