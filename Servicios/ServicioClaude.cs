using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio para Claude-Style usando Ollama como backend
    /// Especializado en conversaciones naturales y asistencia creativa
    /// </summary>
    public class ServicioClaude : IProveedorIA
    {
        private readonly ILogger<ServicioClaude> _logger;
        private readonly ServicioOllama _servicioOllama;
        
        public string NombreProveedor => "Claude-Style Llama (Conversacional)";
        public string IdProveedor => "claude";
        public bool RequiereApiKey => false;
        
        // Modelos Claude-style preferidos en orden de prioridad
        private readonly string[] _modelosClaude = {
            "llama3.1-claude:latest",
            "deepseek_r1-claude:latest",
            "llama3.1:8b",
            "llama3.1:latest",
            "llama3:latest"
        };

        public ServicioClaude(ILogger<ServicioClaude> logger, ServicioOllama servicioOllama)
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
                    _logger.LogWarning("🔍 Claude no disponible: Ollama no está ejecutándose");
                    return false;
                }

                // Verificar que al menos un modelo Claude-style esté disponible
                foreach (var modelo in _modelosClaude)
                {
                    if (await _servicioOllama.VerificarModeloDisponibleAsync(modelo))
                    {
                        _logger.LogInformation("✅ Claude disponible con modelo: {Modelo}", modelo);
                        return true;
                    }
                }

                _logger.LogWarning("⚠️ Claude no disponible: Ningún modelo Claude-style encontrado");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error verificando disponibilidad de Claude");
                return false;
            }
        }

        public async Task<EstadoProveedorIA> ObtenerEstadoAsync()
        {
            try
            {
                if (!await EstaDisponibleAsync())
                {
                    return EstadoProveedorIA.NoDisponible("Claude requiere Ollama y modelos Claude-style instalados");
                }

                // Obtener el modelo activo
                var modeloActivo = await ObtenerModeloActivoAsync();
                var mensaje = $"Claude activo con {modeloActivo} - Especializado en conversaciones naturales";
                
                return EstadoProveedorIA.Disponible(mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo estado de Claude");
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
                    throw new InvalidOperationException("Claude no está disponible");
                }

                // Obtener modelo Claude activo
                var modeloActivo = await ObtenerModeloActivoAsync();
                
                // Preparar prompt optimizado para Claude (conversacional)
                var promptOptimizado = OptimizarPromptParaClaude(mensaje);
                
                _logger.LogInformation("💬 Procesando con Claude-Style - Modo conversacional");
                
                // Delegar a Ollama con el modelo específico
                return await _servicioOllama.GenerarRespuestaConModeloAsync(promptOptimizado, modeloActivo, historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando respuesta con Claude");
                throw new InvalidOperationException($"Error en Claude: {ex.Message}", ex);
            }
        }

        private async Task<string> ObtenerModeloActivoAsync()
        {
            foreach (var modelo in _modelosClaude)
            {
                if (await _servicioOllama.VerificarModeloDisponibleAsync(modelo))
                {
                    return modelo;
                }
            }
            
            throw new InvalidOperationException("No hay modelos Claude-style disponibles");
        }

        public async Task<bool> ValidarConfiguracionAsync()
        {
            return await EstaDisponibleAsync();
        }

        public async Task ConfigurarAsync(Dictionary<string, string> configuracion)
        {
            // Claude usa Ollama como backend, delegar configuración
            await _servicioOllama.ConfigurarAsync(configuracion);
        }

        public async Task<string> ProcesarChatAsync(string mensaje, List<SesionChat>? historial = null)
        {
            try
            {
                // Verificar disponibilidad
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("Claude no está disponible");
                }

                // Optimizar prompt para conversación
                var promptOptimizado = OptimizarPromptParaClaude(mensaje);
                
                // Delegar a Ollama
                return await _servicioOllama.ProcesarChatAsync(promptOptimizado, historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en ProcesarChatAsync con Claude");
                throw new InvalidOperationException($"Error en Claude: {ex.Message}", ex);
            }
        }

        public async Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta)
        {
            try
            {
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("Claude no está disponible");
                }

                // 🆕 DETECCIÓN AUTOMÁTICA DE FACTURAS
                var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
                var analizadorFacturas = new AnalizadorFacturas(loggerFactory.CreateLogger<AnalizadorFacturas>());
                
                if (analizadorFacturas.EsFactura(contenidoArchivos))
                {
                    _logger.LogInformation("💬📄 Claude detectó FACTURA - Activando análisis especializado");
                    
                    // Usar análisis especializado de facturas
                    var analisisFactura = await analizadorFacturas.AnalizarFacturaAsync(contenidoArchivos, pregunta);
                    
                    // Si el análisis automático fue exitoso, usar prompt optimizado
                    if (analisisFactura.EsFacturaValida)
                    {
                        var promptEspecializado = analizadorFacturas.GenerarPromptAnalisisFactura(
                            contenidoArchivos, pregunta, TipoProveedorIA.Claude);
                        
                        var analisisIA = await _servicioOllama.AnalizarContenidoConIAAsync(contenidoArchivos, promptEspecializado);
                        
                        // Combinar análisis estructurado + análisis conversacional
                        return $@"{analisisFactura.AnalisisCompleto}

---

💬 **ANÁLISIS CONVERSACIONAL CLAUDE-STYLE:**

{analisisIA}";
                    }
                }

                // Análisis genérico conversacional para otros documentos
                var promptAnalisis = $@"[ANÁLISIS CONVERSACIONAL CLAUDE-STYLE]

Como Claude, analiza el siguiente contenido de manera natural y útil:

CONTENIDO:
{contenidoArchivos}

PREGUNTA:
{pregunta}

ESTILO DE ANÁLISIS CLAUDE:
- Analiza de manera conversacional y accesible
- Proporciona insights útiles y prácticos
- Estructura la respuesta de forma clara
- Incluye ejemplos o contexto cuando sea útil
- Sé honesto sobre limitaciones

ANÁLISIS:";

                return await _servicioOllama.AnalizarContenidoConIAAsync(contenidoArchivos, promptAnalisis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error analizando contenido con Claude");
                throw new InvalidOperationException($"Error en análisis Claude: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general")
        {
            try
            {
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("Claude no está disponible");
                }

                var instruccionesResumen = tipoResumen.ToLower() switch
                {
                    "ejecutivo" => "Crea un resumen ejecutivo conversacional, enfocado en puntos clave de manera accesible para liderazgo.",
                    "tecnico" => "Genera un resumen técnico claro y bien estructurado, explicando conceptos complejos de manera comprensible.",
                    "conversacional" => "Proporciona un resumen natural y amigable, como si estuvieras explicando a un colega.",
                    _ => "Crea un resumen equilibrado y conversacional que capture los puntos principales de manera clara y útil."
                };

                return await _servicioOllama.GenerarResumenInteligente(contenido, instruccionesResumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando resumen con Claude");
                throw new InvalidOperationException($"Error en resumen Claude: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos)
        {
            try
            {
                if (!await EstaDisponibleAsync())
                {
                    throw new InvalidOperationException("Claude no está disponible");
                }

                return await _servicioOllama.GenerarSugerenciasPreguntasAsync(contenidoArchivos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando sugerencias con Claude");
                return new List<string>
                {
                    "¿Puedes explicar los puntos principales?",
                    "¿Qué aspectos son más relevantes?",
                    "¿Hay algo específico que debería saber?",
                    "¿Puedes darme ejemplos prácticos?"
                };
            }
        }

        private string OptimizarPromptParaClaude(string mensajeOriginal)
        {
            // Claude es excelente para conversaciones naturales y asistencia
            return $@"[MODO CONVERSACIONAL - Claude-Style]

Como Claude, un asistente de IA útil, inofensivo y honesto, responde a la siguiente consulta de manera natural y conversacional:

CONSULTA: {mensajeOriginal}

PAUTAS DE RESPUESTA CLAUDE:
- Sé útil, preciso y detallado
- Mantén un tono amigable y profesional
- Proporciona ejemplos cuando sea apropiado
- Admite limitaciones cuando corresponda
- Estructura la respuesta de manera clara

RESPUESTA:";
        }
    }
}