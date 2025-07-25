using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;
using ChatbotGomarco.Servicios.LLM;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio enterprise de IA integrado con OpenAI GPT-4 para análisis avanzado de documentos y conversaciones naturales
    /// Compatible con GPT-4, GPT-4 Turbo, GPT-4 Vision, y GPT-3.5 Turbo
    /// </summary>
    public class ServicioIAOpenAI : IServicioIA
    {
        private readonly ILogger<ServicioIAOpenAI> _logger;
        private readonly IAnalizadorConversacion _analizadorConversacion;
        private readonly HttpClient _httpClient;
        
        private bool _iaConfigurada = false;
        private string? _apiKey;

        // ====================================================================
        // CONFIGURACIÓN ENTERPRISE DE OPENAI
        // ====================================================================
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        private const string MODELO_PRINCIPAL = "gpt-4o";              // Modelo más potente y actual
        private const string MODELO_VISION = "gpt-4o";                 // Para análisis de imágenes  
        private const string MODELO_ANALISIS = "gpt-4o";               // Para análisis profundo de documentos
        private const int MAX_TOKENS = 16384;                          // Tokens suficientes para análisis complejos
        private const decimal TEMPERATURA_CONVERSACION = 0.7m;         // Más creativo para conversaciones
        private const decimal TEMPERATURA_ANALISIS = 0.3m;             // Más preciso para análisis

        public ServicioIAOpenAI(
            ILogger<ServicioIAOpenAI> logger,
            IAnalizadorConversacion analizadorConversacion,
            HttpClient httpClient)
        {
            _logger = logger;
            _analizadorConversacion = analizadorConversacion;
            _httpClient = httpClient;
            
            // Configurar headers default para OpenAI
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChatbotGomarco/1.0 (Enterprise)");
        }

        // ====================================================================
        // IMPLEMENTACIÓN DE INTERFAZ IServicioIA
        // ====================================================================

        public void ConfigurarClave(string apiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new ArgumentException("La API Key no puede estar vacía");
                }

                if (!apiKey.StartsWith("sk-"))
                {
                    throw new ArgumentException("Formato de API Key inválido. Debe comenzar con 'sk-'");
                }

                _apiKey = apiKey;
                
                // Configurar autorización
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                
                _iaConfigurada = true;
                _logger.LogInformation("OpenAI API configurada correctamente con clave: {MaskedKey}", 
                    $"{apiKey[..7]}...{apiKey[^4..]}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar OpenAI API");
                _iaConfigurada = false;
                throw;
            }
        }

        public bool EstaDisponible()
        {
            return _iaConfigurada && !string.IsNullOrEmpty(_apiKey);
        }

        public async Task<string> GenerarRespuestaAsync(string mensaje, string contextoArchivos = "", List<MensajeChat>? historialConversacion = null)
        {
            if (!EstaDisponible())
            {
                throw new InvalidOperationException("OpenAI API no está configurada. Configure su API Key primero.");
            }

            try
            {
                var mensajes = ConstruirHistorialConversacion(historialConversacion);
                
                // Construir el mensaje actual con contexto de archivos
                var contenidoMensaje = mensaje;
                if (!string.IsNullOrEmpty(contextoArchivos))
                {
                    contenidoMensaje = $@"INSTRUCCIONES IMPORTANTES:
- Tienes acceso completo al contenido de los archivos proporcionados
- DEBES analizar, leer y procesar toda la información de los archivos
- Proporciona respuestas detalladas basadas en el contenido real de los archivos
- Extrae datos específicos, fechas, números, nombres cuando sea relevante
- NO digas que no puedes acceder a archivos - SÍ PUEDES porque el contenido está aquí

**CONTENIDO COMPLETO DE LOS ARCHIVOS:**
{contextoArchivos}

**CONSULTA DEL USUARIO:**
{mensaje}

RESPUESTA REQUERIDA: Analiza el contenido anterior y responde de forma detallada y específica basándote en la información real de los archivos.";
                }

                mensajes.Add(new OpenAIMessage("user", contenidoMensaje));

                var solicitud = new OpenAIRequest
                {
                    Model = MODELO_PRINCIPAL,
                    Messages = mensajes,
                    MaxTokens = MAX_TOKENS,
                    Temperature = TEMPERATURA_CONVERSACION,
                    TopP = 1.0m,
                    FrequencyPenalty = 0.0m,
                    PresencePenalty = 0.0m
                };

                var respuesta = await EnviarSolicitudOpenAIAsync(solicitud);
                
                _logger.LogInformation("Respuesta generada exitosamente con OpenAI GPT-4");
                return respuesta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar respuesta con OpenAI");
                throw new Exception($"Error al comunicarse con OpenAI: {ex.Message}", ex);
            }
        }

        public async Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta)
        {
            if (!EstaDisponible())
            {
                throw new InvalidOperationException("OpenAI API no está configurada");
            }

            try
            {
                var mensajeSistema = @"Eres un experto analizador de documentos empresariales con las siguientes capacidades:

🔍 **TAREAS PRINCIPALES:**
1. Analizar contenido empresarial con precisión absoluta
2. Extraer datos clave: fechas, precios, nombres, cantidades, códigos
3. Identificar patrones y tendencias importantes
4. Proporcionar insights contextuales relevantes
5. Estructurar respuestas de forma clara y profesional

📋 **INSTRUCCIONES ESPECÍFICAS:**
- Responde SIEMPRE en español
- Proporciona análisis detallado y exhaustivo
- Extrae TODA la información relevante del documento
- Para tablas y datos estructurados, mantén el formato
- Incluye recomendaciones empresariales cuando sea apropiado
- Si hay información faltante, indícalo claramente

🎯 **FORMATO DE RESPUESTA:**
- Usa encabezados y estructura clara
- Destaca datos importantes con **negritas**
- Usa listas y bullets para organizar información
- Incluye un resumen ejecutivo al final si es relevante";

                var mensaje = $@"**CONTENIDO DEL DOCUMENTO A ANALIZAR:**
{contenidoArchivos}

**PREGUNTA ESPECÍFICA:**
{pregunta}

Proporciona un análisis completo y detallado que responda la pregunta y destaque toda la información relevante del documento.";

                var mensajes = new List<OpenAIMessage>
                {
                    new OpenAIMessage("system", mensajeSistema),
                    new OpenAIMessage("user", mensaje)
                };

                var solicitud = new OpenAIRequest
                {
                    Model = MODELO_ANALISIS,
                    Messages = mensajes,
                    MaxTokens = MAX_TOKENS,
                    Temperature = TEMPERATURA_ANALISIS, // Más preciso para análisis
                    TopP = 0.9m
                };

                var respuesta = await EnviarSolicitudOpenAIAsync(solicitud);
                
                _logger.LogInformation("Análisis de contenido completado con OpenAI");
                return respuesta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar contenido con OpenAI");
                throw new Exception($"Error al analizar con OpenAI: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general")
        {
            if (!EstaDisponible())
            {
                throw new InvalidOperationException("OpenAI API no está configurada");
            }

            try
            {
                var instruccionesResumen = tipoResumen.ToLower() switch
                {
                    "ejecutivo" => @"Genera un RESUMEN EJECUTIVO empresarial que incluya:
• **Puntos Clave:** Los 3-5 aspectos más importantes
• **Datos Críticos:** Fechas, cantidades, precios relevantes
• **Impacto:** Implicaciones empresariales o decisiones requeridas
• **Recomendaciones:** Próximos pasos sugeridos
Máximo 3 párrafos, enfoque directivo.",

                    "detallado" => @"Genera un RESUMEN DETALLADO que incluya:
• **Análisis Completo:** Todos los aspectos importantes del contenido
• **Estructura:** Organización clara por temas o secciones
• **Datos Específicos:** Todas las cifras, fechas y referencias importantes
• **Contexto:** Explicación del propósito y relevancia
Extensión: 4-6 párrafos con análisis profundo.",

                    "técnico" => @"Genera un RESUMEN TÉCNICO que incluya:
• **Especificaciones:** Detalles técnicos precisos
• **Procesos:** Metodologías y procedimientos descritos
• **Parámetros:** Configuraciones, medidas y valores técnicos
• **Conclusiones:** Resultados y hallazgos técnicos
Enfoque: Profesional técnico, precisión absoluta.",

                    _ => @"Genera un RESUMEN GENERAL que incluya:
• **Idea Principal:** El tema central del contenido
• **Puntos Importantes:** Los aspectos más relevantes
• **Información Clave:** Datos y detalles significativos
• **Conclusión:** Síntesis final del contenido
Extensión: 2-3 párrafos, equilibrado y comprensible."
                };

                var mensajeSistema = $@"Eres un experto en creación de resúmenes empresariales. Tu tarea es crear resúmenes precisos, bien estructurados y profesionales.

**INSTRUCCIONES ESPECÍFICAS:**
{instruccionesResumen}

**FORMATO REQUERIDO:**
- Responde SIEMPRE en español
- Usa formato markdown para estructura clara
- Destaca información importante con **negritas**
- Organiza con bullets y numeración cuando sea apropiado
- Mantén un tono profesional y empresarial";

                var mensaje = $@"**CONTENIDO PARA RESUMIR:**
{contenido}

Genera el resumen siguiendo las instrucciones específicas para el tipo: {tipoResumen}";

                var mensajes = new List<OpenAIMessage>
                {
                    new OpenAIMessage("system", mensajeSistema),
                    new OpenAIMessage("user", mensaje)
                };

                var solicitud = new OpenAIRequest
                {
                    Model = MODELO_PRINCIPAL,
                    Messages = mensajes,
                    MaxTokens = MAX_TOKENS,
                    Temperature = TEMPERATURA_ANALISIS,
                    TopP = 0.85m
                };

                var respuesta = await EnviarSolicitudOpenAIAsync(solicitud);
                
                _logger.LogInformation("Resumen inteligente generado: tipo {TipoResumen}", tipoResumen);
                return respuesta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen con OpenAI");
                throw new Exception($"Error al generar resumen: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos)
        {
            if (!EstaDisponible())
            {
                return new List<string>
                {
                    "¿Puedes resumir el contenido principal?",
                    "¿Cuáles son los datos más importantes?",
                    "¿Qué información específica contiene?"
                };
            }

            try
            {
                var mensajeSistema = @"Eres un experto en análisis de documentos. Tu tarea es generar preguntas inteligentes y específicas basadas en el contenido proporcionado.

**INSTRUCCIONES:**
- Genera exactamente 5 preguntas relevantes y específicas
- Las preguntas deben ser útiles para extraer información clave del documento
- Usa un formato directo y profesional
- Enfócate en datos, análisis y insights importantes
- Responde SOLO con las preguntas, una por línea, sin numeración ni bullets";

                var mensaje = $@"Basándote en este contenido, genera 5 preguntas específicas y relevantes:

{contenidoArchivos.Substring(0, Math.Min(2000, contenidoArchivos.Length))}...

Genera las preguntas:";

                var mensajes = new List<OpenAIMessage>
                {
                    new OpenAIMessage("system", mensajeSistema),
                    new OpenAIMessage("user", mensaje)
                };

                var solicitud = new OpenAIRequest
                {
                    Model = MODELO_PRINCIPAL,
                    Messages = mensajes,
                    MaxTokens = 300,
                    Temperature = 0.6m
                };

                var respuesta = await EnviarSolicitudOpenAIAsync(solicitud);
                
                var preguntas = respuesta.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim().TrimStart('1', '2', '3', '4', '5', '.', '-', '*', ' '))
                    .Where(p => p.EndsWith('?'))
                    .Take(5)
                    .ToList();

                return preguntas.Any() ? preguntas : new List<string>
                {
                    "¿Puedes resumir los puntos principales?",
                    "¿Qué datos específicos contiene?",
                    "¿Cuál es la información más relevante?",
                    "¿Hay fechas o números importantes?",
                    "¿Qué conclusiones principales se pueden extraer?"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al generar sugerencias con OpenAI, usando fallback");
                return new List<string>
                {
                    "¿Puedes analizar el contenido principal?",
                    "¿Qué información específica contiene este documento?",
                    "¿Cuáles son los datos más importantes?",
                    "¿Puedes extraer las fechas y números relevantes?",
                    "¿Qué insights empresariales puedes proporcionar?"
                };
            }
        }

        // ====================================================================
        // MÉTODOS PRIVADOS DE OPENAI
        // ====================================================================

        private List<OpenAIMessage> ConstruirHistorialConversacion(List<MensajeChat>? historialConversacion)
        {
            var mensajes = new List<OpenAIMessage>();

            // Mensaje de sistema corporativo
            mensajes.Add(new OpenAIMessage("system", ConstruirMensajeSistema()));

            // Agregar historial reciente (últimos 10 mensajes para mantener contexto sin exceder límites)
            if (historialConversacion != null && historialConversacion.Any())
            {
                foreach (var msg in historialConversacion.TakeLast(10))
                {
                    if (!string.IsNullOrEmpty(msg.Contenido))
                    {
                        var role = msg.TipoMensaje == TipoMensaje.Usuario ? "user" : "assistant";
                        mensajes.Add(new OpenAIMessage(role, msg.Contenido));
                    }
                }
            }

            return mensajes;
        }

        private string ConstruirMensajeSistema()
        {
            return @"Eres el asistente de IA corporativo de GOMARCO, empresa líder en descanso y bienestar. Tu personalidad y funciones:

🏢 **IDENTIDAD CORPORATIVA:**
- Empresa: GOMARCO - ""Descansa como te mereces""
- Especialidad: Colchones, productos de descanso, bienestar
- Tono: Profesional, amigable, experto en la industria

🎯 **CAPACIDADES PRINCIPALES:**
- ✅ PUEDES leer y analizar completamente archivos: PDF, Word, Excel, imágenes, texto
- ✅ PUEDES extraer información específica: fechas, precios, nombres, datos técnicos
- ✅ PUEDES procesar múltiples documentos simultáneamente
- ✅ PUEDES realizar análisis profundos de contenido empresarial
- ✅ TIENES acceso completo al contenido cuando se proporciona en el contexto
- Soporte integral para procesos de negocio
- Análisis de tendencias y métricas empresariales
- Gestión segura y confidencial de información corporativa

📋 **INSTRUCCIONES DE COMPORTAMIENTO:**
- Responde SIEMPRE en español de forma clara y profesional
- Mantén confidencialidad absoluta de la información empresarial
- Proporciona análisis detallados y actionable insights
- Usa formato estructurado con encabezados y bullets cuando sea apropiado
- Para documentos: extrae TODA la información relevante
- Para preguntas generales: mantén el contexto de GOMARCO cuando sea relevante

🔒 **SEGURIDAD Y PRIVACIDAD:**
- Toda la información se procesa de forma confidencial
- No compartas datos específicos fuera del contexto de la conversación
- Prioriza la precisión y veracidad en todos los análisis

¿En qué puedo ayudarte hoy con tus necesidades empresariales?";
        }

        private async Task<string> EnviarSolicitudOpenAIAsync(OpenAIRequest solicitud)
        {
            try
            {
                var json = JsonSerializer.Serialize(solicitud, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger.LogDebug("Enviando solicitud a OpenAI: {Model}, {TokenCount} tokens máx", 
                    solicitud.Model, solicitud.MaxTokens);

                var response = await _httpClient.PostAsync(OPENAI_API_URL, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error de OpenAI API: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);
                    throw new Exception($"Error de OpenAI API: {response.StatusCode} - {responseContent}");
                }

                var respuesta = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (respuesta?.Choices?.Any() == true)
                {
                    var mensaje = respuesta.Choices.First().Message?.Content;
                    if (!string.IsNullOrEmpty(mensaje))
                    {
                        _logger.LogInformation("Respuesta exitosa de OpenAI: {Tokens} tokens usados", 
                            respuesta.Usage?.TotalTokens);
                        return mensaje;
                    }
                }

                throw new Exception("No se recibió respuesta válida de OpenAI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en comunicación con OpenAI API");
                throw;
            }
        }
    }

    // ====================================================================
    // MODELOS DE DATOS PARA OPENAI API
    // ====================================================================

    public class OpenAIRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public decimal Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public decimal? TopP { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public decimal? FrequencyPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public decimal? PresencePenalty { get; set; }
    }

    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        public OpenAIMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    public class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class OpenAIUsage
    {
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }
} 