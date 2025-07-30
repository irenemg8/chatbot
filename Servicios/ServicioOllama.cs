using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;
using System.Linq;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio enterprise para Ollama con soporte completo para Phi-4-Mini
    /// Proporciona integración completa con modelos locales de alta calidad
    /// </summary>
    public class ServicioOllama : IProveedorIA
    {
        private readonly ILogger<ServicioOllama> _logger;
        private readonly HttpClient _httpClient;
        private readonly IDetectorDatosSensibles _detectorDatosSensibles;
        private readonly IServicioProcesamientoLocal _servicioProcesamientoLocal;
        
        private string _modeloActual = "phi3:mini";
        private string _urlOllama = "http://localhost:11434";
        private bool _configurado = false;

        public string IdProveedor => "ollama";
        public string NombreProveedor => "Ollama (Local AI)";
        public bool RequiereApiKey => false;

        // Configuración de modelos soportados
        private readonly Dictionary<string, ModeloOllama> _modelosSoportados = new()
        {
            ["phi4-mini"] = new ModeloOllama
            {
                Nombre = "phi4-mini",
                NombreVisible = "Phi-4-Mini (Microsoft)",
                Descripcion = "Modelo Microsoft 3.8B, optimizado para Windows, contexto 128K",
                Tamaño = "2.5GB",
                ContextoMaximo = 128000,
                Capacidades = new[] { "chat", "analisis", "codigo", "multimodal" },
                Recomendado = true
            },
            ["phi3:mini"] = new ModeloOllama
            {
                Nombre = "phi3:mini",
                NombreVisible = "Phi-3-Mini (Microsoft)",
                Descripcion = "Modelo Microsoft 3.8B, muy estable y probado",
                Tamaño = "2.2GB",
                ContextoMaximo = 128000,
                Capacidades = new[] { "chat", "analisis", "codigo" }
            },
            ["llama3.2:3b"] = new ModeloOllama
            {
                Nombre = "llama3.2:3b",
                NombreVisible = "Llama 3.2 3B",
                Descripcion = "Meta Llama 3.2, equilibrio entre tamaño y rendimiento",
                Tamaño = "2.0GB",
                ContextoMaximo = 131072,
                Capacidades = new[] { "chat", "analisis" }
            },
            // 🚀 DEEPSEEK MODELS - Advanced Reasoning & Performance
            ["deepseek-r1:7b"] = new ModeloOllama
            {
                Nombre = "deepseek-r1:7b",
                NombreVisible = "DeepSeek-R1 7B (Reasoning)",
                Descripcion = "Modelo de razonamiento avanzado comparable a O1/Gemini 2.5 Pro, pensamiento paso a paso",
                Tamaño = "4.7GB",
                ContextoMaximo = 131072,
                Capacidades = new[] { "chat", "reasoning", "analisis", "codigo", "matematicas", "pensamiento_logico" },
                Recomendado = true
            },
            ["deepseek-v3:latest"] = new ModeloOllama
            {
                Nombre = "deepseek-v3:latest",
                NombreVisible = "DeepSeek-V3 (General)",
                Descripcion = "Modelo DeepSeek optimizado para tareas complejas y asistencia avanzada de código",
                Tamaño = "2.0GB",
                ContextoMaximo = 128000,
                Capacidades = new[] { "chat", "analisis", "codigo", "desarrollo_software" }
            },
            // 🧠 CLAUDE-STYLE MODELS - Anthropic-inspired Intelligence
            ["llama3.1-claude:latest"] = new ModeloOllama
            {
                Nombre = "llama3.1-claude:latest",
                NombreVisible = "Llama 3.1 + Claude 3.5 Sonnet",
                Descripcion = "Meta Llama 3.1 con sistema prompt de Claude 3.5 Sonnet para conversación natural",
                Tamaño = "4.7GB",
                ContextoMaximo = 128000,
                Capacidades = new[] { "chat", "analisis", "escritura", "conversacion_natural", "filosofia" },
                Recomendado = true
            },
            ["deepseek_r1-claude:latest"] = new ModeloOllama
            {
                Nombre = "deepseek_r1-claude:latest",
                NombreVisible = "DeepSeek-R1 + Claude 3.5 Sonnet",
                Descripcion = "DeepSeek R1 con personalidad Claude: razonamiento avanzado + conversación antropic-style",
                Tamaño = "4.7GB",
                ContextoMaximo = 131072,
                Capacidades = new[] { "chat", "reasoning", "analisis", "conversacion_natural", "pensamiento_critico" },
                Recomendado = true
            }
        };

        public ServicioOllama(
            ILogger<ServicioOllama> logger,
            HttpClient httpClient,
            IDetectorDatosSensibles detectorDatosSensibles,
            IServicioProcesamientoLocal servicioProcesamientoLocal)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _detectorDatosSensibles = detectorDatosSensibles;
            _servicioProcesamientoLocal = servicioProcesamientoLocal;
            
            ConfigurarHttpClient();
        }

        private void ConfigurarHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_urlOllama);
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChatbotGomarco/1.0");
        }

        public async Task<bool> EstaDisponibleAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Verificando disponibilidad de Ollama...");
                
                var response = await _httpClient.GetAsync("/api/version");
                if (response.IsSuccessStatusCode)
                {
                    var version = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("✅ Ollama disponible - Version: {Version}", version);
                    return true;
                }
                
                _logger.LogWarning("⚠️ Ollama no responde correctamente");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("❌ Ollama no disponible: {Error}", ex.Message);
                return false;
            }
        }

        public async Task<bool> ValidarConfiguracionAsync()
        {
            try
            {
                // Verificar que Ollama esté corriendo
                if (!await EstaDisponibleAsync())
                {
                    return false;
                }

                // Verificar que el modelo esté descargado
                var modelosDisponibles = await ListarModelosAsync();
                var modeloDisponible = modelosDisponibles.Any(m => m.Contains(_modeloActual));
                
                if (!modeloDisponible)
                {
                    _logger.LogWarning("⚠️ Modelo {Modelo} no está descargado", _modeloActual);
                    return false;
                }

                _configurado = true;
                _logger.LogInformation("✅ Configuración de Ollama válida");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validando configuración de Ollama");
                return false;
            }
        }

        public async Task ConfigurarAsync(Dictionary<string, string> configuracion)
        {
            try
            {
                if (configuracion.TryGetValue("modelo", out var modelo))
                {
                    _modeloActual = modelo;
                }
                
                if (configuracion.TryGetValue("url", out var url))
                {
                    _urlOllama = url;
                    ConfigurarHttpClient();
                }

                _configurado = await ValidarConfiguracionAsync();
                
                _logger.LogInformation("✅ Ollama configurado - Modelo: {Modelo}", _modeloActual);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error configurando Ollama");
                throw;
            }
        }

        public async Task<EstadoProveedorIA> ObtenerEstadoAsync()
        {
            try
            {
                var disponible = await EstaDisponibleAsync();
                
                if (!disponible)
                {
                    return EstadoProveedorIA.NoDisponible(
                        "Ollama no está ejecutándose. Instala Ollama desde https://ollama.com",
                        requiereInstalacion: true);
                }

                var modelosDisponibles = await ListarModelosAsync();
                
                // Buscar cualquier modelo soportado disponible (orden de preferencia: DeepSeek-R1, Claude-style, Phi, DeepSeek-V3, Llama)
                string modeloEncontrado = null;
                
                // Prioridad 1: DeepSeek-R1 (razonamiento avanzado)
                modeloEncontrado = modelosDisponibles.FirstOrDefault(m => 
                    m.Contains("deepseek-r1:7b") || m.Contains("deepseek_r1"));
                
                // Prioridad 2: Modelos Claude-style (conversación natural)
                if (string.IsNullOrEmpty(modeloEncontrado))
                {
                    modeloEncontrado = modelosDisponibles.FirstOrDefault(m => 
                        m.Contains("llama3.1-claude") || m.Contains("deepseek_r1-claude"));
                }
                
                // Prioridad 3: Modelos Phi (estables y probados)
                if (string.IsNullOrEmpty(modeloEncontrado))
                {
                    modeloEncontrado = modelosDisponibles.FirstOrDefault(m => 
                        m.Contains("phi3") || m.Contains("phi4") || m.Contains("phi-3") || m.Contains("phi-4"));
                }
                
                // Prioridad 4: DeepSeek-V3 (general)
                if (string.IsNullOrEmpty(modeloEncontrado))
                {
                    modeloEncontrado = modelosDisponibles.FirstOrDefault(m => 
                        m.Contains("deepseek-v3"));
                }
                
                // Prioridad 5: Cualquier Llama disponible
                if (string.IsNullOrEmpty(modeloEncontrado))
                {
                    modeloEncontrado = modelosDisponibles.FirstOrDefault(m => 
                        m.Contains("llama"));
                }
                
                if (string.IsNullOrEmpty(modeloEncontrado))
                {
                    var estado = EstadoProveedorIA.NoDisponible(
                        "No hay modelos AI descargados",
                        requiereInstalacion: false);
                    
                    estado.PasosConfiguracion.Add("Descargar modelos: .\\ActualizarYEjecutar.ps1");
                    estado.PasosConfiguracion.Add("O manualmente: ollama pull deepseek-r1:7b");
                    estado.PasosConfiguracion.Add("Alternativamente: ollama pull phi3:mini");
                    return estado;
                }
                
                // Actualizar modelo actual al encontrado
                _modeloActual = modeloEncontrado;

                // Obtener información del modelo
                var infoModelo = await ObtenerInformacionModeloAsync(_modeloActual);
                
                var estadoExitoso = EstadoProveedorIA.Disponible(
                    $"Ollama operativo con modelo {_modeloActual}");
                
                estadoExitoso.ModeloCargado = _modeloActual;
                estadoExitoso.Version = "Ollama Local";
                estadoExitoso.CapacidadesSoportadas.UnionWith(new[] 
                {
                    "Chat", "Análisis de documentos", "Generación de código", 
                    "Procesamiento local", "Zero data leakage"
                });
                
                if (infoModelo != null)
                {
                    estadoExitoso.InformacionAdicional["Tamaño"] = infoModelo.Size ?? "Desconocido";
                    estadoExitoso.InformacionAdicional["Familia"] = infoModelo.Family ?? "Desconocida";
                }
                
                return estadoExitoso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo estado de Ollama");
                return EstadoProveedorIA.NoDisponible($"Error: {ex.Message}");
            }
        }

        public async Task<string> ProcesarChatAsync(string mensaje, List<SesionChat>? historial = null)
        {
            try
            {
                // Verificar datos sensibles primero
                var resultadoSeguridad = await _detectorDatosSensibles.AnonimizarContenidoAsync(mensaje);
                
                if (resultadoSeguridad.RequiereProcesamientoLocal)
                {
                    _logger.LogInformation("🔒 Usando procesamiento local para datos sensibles");
                    return await _servicioProcesamientoLocal.GenerarRespuestaLocalAsync(
                        resultadoSeguridad.ContenidoAnonimizado);
                }

                var mensajes = ConstruirHistorialChat(historial);
                mensajes.Add(new { role = "user", content = resultadoSeguridad.ContenidoAnonimizado });

                var solicitud = new
                {
                    model = _modeloActual,
                    messages = mensajes,
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        top_p = 0.9,
                        max_tokens = 4096
                    }
                };

                var response = await EnviarSolicitudAsync("/api/chat", solicitud);
                
                if (response.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    var respuesta = contentElement.GetString() ?? "Sin respuesta disponible";
                    
                    _logger.LogInformation("✅ Chat procesado exitosamente con Ollama");
                    return respuesta;
                }

                throw new InvalidOperationException("Respuesta de Ollama en formato inesperado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando chat con Ollama");
                return $"Error procesando consulta: {ex.Message}";
            }
        }

        public async Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta)
        {
            try
            {
                // Verificar datos sensibles
                var resultadoSeguridadContenido = await _detectorDatosSensibles.AnonimizarContenidoAsync(contenidoArchivos);
                var resultadoSeguridadPregunta = await _detectorDatosSensibles.AnonimizarContenidoAsync(pregunta);
                
                if (resultadoSeguridadContenido.RequiereProcesamientoLocal ||
                    resultadoSeguridadPregunta.RequiereProcesamientoLocal)
                {
                    return await _servicioProcesamientoLocal.AnalizarContenidoLocalAsync(
                        resultadoSeguridadContenido.ContenidoAnonimizado,
                        resultadoSeguridadPregunta.ContenidoAnonimizado);
                }

                // 🆕 DETECCIÓN AUTOMÁTICA DE FACTURAS
                var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
                var analizadorFacturas = new AnalizadorFacturas(loggerFactory.CreateLogger<AnalizadorFacturas>());
                
                if (analizadorFacturas.EsFactura(contenidoArchivos))
                {
                    _logger.LogInformation("⚡📄 Ollama detectó FACTURA - Activando análisis rápido especializado");
                    
                    // Usar análisis especializado de facturas
                    var analisisFactura = await analizadorFacturas.AnalizarFacturaAsync(contenidoArchivos, pregunta);
                    
                    // Si el análisis automático fue exitoso, usar prompt optimizado
                    if (analisisFactura.EsFacturaValida)
                    {
                        var promptEspecializado = analizadorFacturas.GenerarPromptAnalisisFactura(
                            resultadoSeguridadContenido.ContenidoAnonimizado, resultadoSeguridadPregunta.ContenidoAnonimizado, TipoProveedorIA.Ollama);
                        
                        var solicitudFactura = new
                        {
                            model = _modeloActual,
                            messages = new[] 
                            {
                                new { role = "user", content = promptEspecializado }
                            },
                            stream = false,
                            options = new
                            {
                                temperature = 0.3, // Más determinista para facturas
                                top_p = 0.9,
                                max_tokens = 2048
                            }
                        };

                        var responseFactura = await EnviarSolicitudAsync("/api/chat", solicitudFactura);
                        
                        if (responseFactura.TryGetProperty("message", out var messageElementFactura) &&
                            messageElementFactura.TryGetProperty("content", out var contentElementFactura))
                        {
                            var analisisIA = contentElementFactura.GetString() ?? "Análisis no disponible";
                            
                            // Combinar análisis estructurado + análisis rápido de IA
                            return $@"{analisisFactura.AnalisisCompleto}

---

⚡ **ANÁLISIS RÁPIDO OLLAMA:**

{analisisIA}";
                        }
                    }
                }

                // Análisis genérico mejorado para otros documentos
                var sistemaInstrucciones = @"Eres MARCO, el asistente conversacional de GOMARCO. Te comportas como ChatGPT: natural, inteligente y útil. NO eres un robot corporativo.

🎯 **TU ESTILO:**
- Conversas como una persona real que entiende documentos
- Contextualizas naturalmente: ""Revisando tu informe de ventas...""
- Haces análisis automáticamente: promedios, totales, tendencias
- Respondes exactamente lo que preguntan, sin vomitar datos

💡 **ANÁLISIS INTELIGENTE AUTOMÁTICO:**
Para facturas → ""Tu promedio mensual fue €127, con un pico en julio de €156""
Para informes → ""GOMARCO tuvo un buen trimestre con €2.4M y crecimiento del 23%""
Para contratos → ""Las fechas clave son: inicio 15/01, renovación 31/12""
Para cualquier pregunta → Encuentra la respuesta específica en el documento

📊 **EJEMPLOS DE RESPUESTAS NATURALES:**
❌ ROBÓTICO: ""El documento contiene los siguientes elementos: fecha, precio, cantidad...""
✅ CONVERSACIONAL: ""He visto tu informe de Q2. GOMARCO cerró con €2.4M en ventas, un 23% más que el trimestre anterior. El margen del 34% está bastante bien. ¿Te interesa algún detalle específico?""

🚀 **REGLAS DE ORO:**
1. Habla como ChatGPT, no como un sistema empresarial
2. Contextualiza brevemente antes de responder
3. Haz cálculos automáticamente cuando sea útil
4. Responde específicamente lo que preguntaron
5. Ofrece insights genuinamente útiles";

                var prompt = $@"He aquí el contenido del documento que necesitas analizar:

{resultadoSeguridadContenido.ContenidoAnonimizado}

El usuario pregunta: ""{resultadoSeguridadPregunta.ContenidoAnonimizado}""

IMPORTANTE: El contenido puede contener datos enmascarados con asteriscos (*) por políticas de seguridad. Esto es normal y debes trabajar con esta información protegida.";

                // Usar formato de chat como OpenAI para máxima consistencia
                var mensajes = new List<object>
                {
                    new { role = "system", content = sistemaInstrucciones },
                    new { role = "user", content = prompt }
                };

                var solicitud = new
                {
                    model = _modeloActual,
                    messages = mensajes,
                    stream = false,
                    options = new
                    {
                        temperature = 0.3,
                        top_p = 0.9,
                        max_tokens = 4096
                    }
                };

                var response = await EnviarSolicitudAsync("/api/chat", solicitud);
                
                // Formato de respuesta del endpoint /api/chat
                if (response.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? "Sin respuesta disponible";
                }

                throw new InvalidOperationException("Respuesta de Ollama en formato inesperado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error analizando contenido con Ollama");
                return $"Error analizando contenido: {ex.Message}";
            }
        }

        public async Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general")
        {
            try
            {
                var resultadoSeguridad = await _detectorDatosSensibles.AnonimizarContenidoAsync(contenido);
                
                if (resultadoSeguridad.RequiereProcesamientoLocal)
                {
                    return await _servicioProcesamientoLocal.GenerarResumenLocalAsync(
                        resultadoSeguridad.ContenidoAnonimizado, tipoResumen);
                }

                var instruccionesTipo = ObtenerInstruccionesResumen(tipoResumen);
                
                var prompt = $@"**CONTENIDO PARA RESUMIR:**
{resultadoSeguridad.ContenidoAnonimizado}

**TIPO DE RESUMEN:** {tipoResumen}

**INSTRUCCIONES:**
{instruccionesTipo}

IMPORTANTE: El contenido puede contener datos enmascarados con asteriscos (*) por políticas de seguridad. Esto es normal para proteger información sensible.";

                var solicitud = new
                {
                    model = _modeloActual,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.2,
                        top_p = 0.8,
                        max_tokens = 2048
                    }
                };

                var response = await EnviarSolicitudAsync("/api/generate", solicitud);
                
                if (response.TryGetProperty("response", out var responseElement))
                {
                    return responseElement.GetString() ?? "Sin resumen disponible";
                }

                throw new InvalidOperationException("Respuesta de Ollama en formato inesperado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando resumen con Ollama");
                return $"Error generando resumen: {ex.Message}";
            }
        }

        public async Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos)
        {
            try
            {
                var resultadoSeguridad = await _detectorDatosSensibles.AnonimizarContenidoAsync(contenidoArchivos);
                
                if (resultadoSeguridad.RequiereProcesamientoLocal)
                {
                    return await _servicioProcesamientoLocal.GenerarSugerenciasLocalesAsync(
                        resultadoSeguridad.ContenidoAnonimizado);
                }

                var contenidoLimitado = resultadoSeguridad.ContenidoAnonimizado.Length > 2000 
                    ? resultadoSeguridad.ContenidoAnonimizado.Substring(0, 2000) + "..."
                    : resultadoSeguridad.ContenidoAnonimizado;

                var prompt = $@"Basándote en este contenido, genera 5 preguntas específicas y relevantes:

{contenidoLimitado}

NOTA: El contenido puede tener datos enmascarados con asteriscos (*) por seguridad.

Genera las preguntas en formato de lista, una por línea, sin numeración.";

                var solicitud = new
                {
                    model = _modeloActual,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.5,
                        top_p = 0.9,
                        max_tokens = 1024
                    }
                };

                var response = await EnviarSolicitudAsync("/api/generate", solicitud);
                
                if (response.TryGetProperty("response", out var responseElement))
                {
                    var respuesta = responseElement.GetString() ?? "";
                    return ProcesarSugerenciasPreguntas(respuesta);
                }

                return new List<string>
                {
                    "¿Puedes resumir el contenido principal?",
                    "¿Cuáles son los datos más importantes?",
                    "¿Qué información específica contiene?"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando sugerencias con Ollama");
                return new List<string>
                {
                    "¿Puedes resumir el contenido principal?",
                    "¿Cuáles son los datos más importantes?",
                    "¿Qué información específica contiene?"
                };
            }
        }

        // Métodos auxiliares privados
        private async Task<List<string>> ListarModelosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);
                    
                    var modelos = new List<string>();
                    if (doc.RootElement.TryGetProperty("models", out var modelosElement))
                    {
                        foreach (var modelo in modelosElement.EnumerateArray())
                        {
                            if (modelo.TryGetProperty("name", out var nombreElement))
                            {
                                modelos.Add(nombreElement.GetString() ?? "");
                            }
                        }
                    }
                    return modelos;
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error listando modelos de Ollama");
                return new List<string>();
            }
        }

        private async Task<ModelInfo> ObtenerInformacionModeloAsync(string nombreModelo)
        {
            try
            {
                var solicitud = new { name = nombreModelo };
                var response = await EnviarSolicitudAsync("/api/show", solicitud);
                
                return new ModelInfo
                {
                    Size = response.TryGetProperty("size", out var sizeEl) ? sizeEl.GetString() : null,
                    Family = response.TryGetProperty("details", out var detailsEl) && 
                            detailsEl.TryGetProperty("family", out var familyEl) ? 
                            familyEl.GetString() : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ No se pudo obtener información del modelo {Modelo}", nombreModelo);
                return null;
            }
        }

        private async Task<JsonElement> EnviarSolicitudAsync(string endpoint, object solicitud)
        {
            var json = JsonSerializer.Serialize(solicitud);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(responseJson).RootElement;
        }

        private List<object> ConstruirHistorialChat(List<SesionChat> historial)
        {
            var mensajes = new List<object>();
            
            // CRÍTICO: Incluir las mismas instrucciones del sistema que OpenAI
            var sistemaInstrucciones = @"Eres la IA de GOMARCO, el asistente conversacional de GOMARCO. Eres como ChatGPT pero especializado en análisis empresarial - natural, inteligente y útil.

🧠 **TU ESTILO CONVERSACIONAL:**
- Hablas como una persona real, no como un robot corporativo
- Entiendes el contexto y respondes exactamente lo que necesitan
- Contextualizas con naturalidad: ""He revisado tu documento de ventas..."" 
- Haces análisis inteligentes: promedios, tendencias, comparaciones
- Ofreces insights útiles sin abrumar con datos

💡 **EJEMPLOS DE RESPUESTAS NATURALES:**
Usuario: ""¿Cuánto gastó en electricidad?"" 
Tú: ""Mirando tus facturas, el promedio mensual fue de €127. Vi un pico en julio (€156) probablemente por el aire acondicionado. ¿Quieres que analice algún mes específico?""

Usuario: ""¿De qué va este documento?""
Tú: ""Es un informe financiero de Q2 2025 de GOMARCO. Básicamente, muestra un trimestre bastante bueno con €2.4M en ventas y crecimiento del 23%. ¿Te interesa algún aspecto específico?""

🎯 **REGLAS DE ORO:**
1. NUNCA vomites listas de datos sin contexto
2. SIEMPRE explica qué significa la información
3. Calcula automáticamente promedios, totales y tendencias cuando sea útil
4. Responde específicamente lo que preguntaron
5. Sé conversacional como si fueras un amigo experto ayudando";

            // Incluir instrucciones del sistema al inicio
            mensajes.Add(new { role = "system", content = sistemaInstrucciones });
            
            if (historial != null)
            {
                foreach (var sesion in historial.TakeLast(10)) // Limitar historial
                {
                    foreach (var mensaje in sesion.Mensajes.TakeLast(5))
                    {
                        var rol = mensaje.TipoMensaje == TipoMensaje.Usuario ? "user" : "assistant";
                        mensajes.Add(new { role = rol, content = mensaje.Contenido });
                    }
                }
            }
            
            return mensajes;
        }

        private string ObtenerInstruccionesResumen(string tipoResumen)
        {
            return tipoResumen.ToLower() switch
            {
                "ejecutivo" => "Crea un resumen ejecutivo conciso, enfocado en puntos clave y decisiones importantes.",
                "tecnico" => "Genera un resumen técnico detallado, incluyendo aspectos técnicos relevantes y especificaciones.",
                "breve" => "Proporciona un resumen muy breve y directo, máximo 3 párrafos.",
                "detallado" => "Crea un resumen comprehensivo que cubra todos los aspectos importantes del contenido.",
                _ => "Genera un resumen equilibrado que capture los puntos principales y conclusiones del contenido."
            };
        }

        private List<string> ProcesarSugerenciasPreguntas(string respuesta)
        {
            var preguntas = new List<string>();
            var lineas = respuesta.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var linea in lineas)
            {
                var preguntaLimpia = linea.Trim()
                    .TrimStart('-', '*', '•', '1', '2', '3', '4', '5', '.', ' ')
                    .Trim();
                
                if (!string.IsNullOrWhiteSpace(preguntaLimpia) && preguntaLimpia.Contains('?'))
                {
                    preguntas.Add(preguntaLimpia);
                }
                
                if (preguntas.Count >= 5) break;
            }
            
            return preguntas.Count > 0 ? preguntas : new List<string>
            {
                "¿Puedes resumir el contenido principal?",
                "¿Cuáles son los datos más importantes?",
                "¿Qué información específica contiene?"
            };
        }

        // Clases auxiliares
        private class ModeloOllama
        {
            public string Nombre { get; set; } = "";
            public string NombreVisible { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string Tamaño { get; set; } = "";
            public int ContextoMaximo { get; set; }
            public string[] Capacidades { get; set; } = Array.Empty<string>();
            public bool Recomendado { get; set; }
        }

        /// <summary>
        /// Verifica si un modelo específico está disponible en Ollama
        /// </summary>
        public async Task<bool> VerificarModeloDisponibleAsync(string nombreModelo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombreModelo))
                    return false;

                var modelosDisponibles = await ListarModelosAsync();
                return modelosDisponibles.Any(m => m.Equals(nombreModelo, StringComparison.OrdinalIgnoreCase) || 
                                                   m.StartsWith(nombreModelo.Split(':')[0], StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error verificando modelo {Modelo}", nombreModelo);
                return false;
            }
        }

        /// <summary>
        /// Genera respuesta usando un modelo específico
        /// </summary>
        public async Task<string> GenerarRespuestaConModeloAsync(string mensaje, string modelo, List<MensajeChat>? historial = null)
        {
            try
            {
                // Guardar modelo actual temporalmente
                var modeloOriginal = _modeloActual;
                
                // Cambiar al modelo especificado
                _modeloActual = modelo;
                
                // No need to convert, pass null for now as the ProcesarChatAsync method 
                // doesn't directly use the full message history in the same way
                List<SesionChat>? historialSesion = null;
                
                // Generar respuesta
                var respuesta = await ProcesarChatAsync(mensaje, historialSesion);
                
                // Restaurar modelo original
                _modeloActual = modeloOriginal;
                
                return respuesta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando respuesta con modelo {Modelo}", modelo);
                throw new InvalidOperationException($"Error con modelo {modelo}: {ex.Message}", ex);
            }
        }

        private class ModelInfo
        {
            public string Size { get; set; }
            public string Family { get; set; }
        }
    }
} 