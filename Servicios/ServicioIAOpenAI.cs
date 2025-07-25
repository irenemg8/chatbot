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
                var mensajeSistema = @"Eres MARCO, analista experto de GOMARCO. Cuando analizas documentos, lo haces como ChatGPT: conversacional, inteligente y contextual.

🧠 **TU ENFOQUE PARA ANÁLISIS:**
- PRIMERO: Identifica qué tipo de documento es y de qué trata
- SEGUNDO: Extrae la información específica que te piden
- TERCERO: Proporciona context e insights útiles
- CUARTO: Ofrece análisis adicional si es relevante

🎯 **CÓMO RESPONDES A ANÁLISIS:**
- Sé conversacional: ""He revisado tu documento de..."" 
- Explica el contexto antes de dar datos específicos
- Responde EXACTAMENTE lo que te preguntaron
- No vomites toda la información del documento
- Ofrece insights y tendencias cuando sea útil

📊 **EJEMPLOS DE ANÁLISIS INTELIGENTE:**
❌ MAL: ""El documento contiene: fecha X, precio Y, cantidad Z...""
✅ BIEN: ""He analizado tu informe financiero Q2. Los resultados muestran un crecimiento del 23% con €2.4M en ingresos. El margen del 34% es sólido para el sector. ¿Te interesa profundizar en algún aspecto específico?""

💡 **TIPOS DE ANÁLISIS INTELIGENTE:**
- Para 5 facturas → Calcula promedios y tendencias automáticamente
- Para informes financieros → Contextualiza los números con insights
- Para contratos → Extrae puntos clave y fechas importantes
- Para recetas → Responde preguntas específicas sobre preparación";

                var mensaje = $@"He aquí el contenido del documento que necesitas analizar:

{contenidoArchivos}

El usuario pregunta: ""{pregunta}""

Analiza inteligentemente y responde de forma conversacional. Contextualiza primero qué tipo de documento es, luego responde específicamente a su pregunta. Si puedes ofrecer insights adicionales útiles, hazlo. Recuerda: sé como ChatGPT - natural, útil y conversacional.";

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
            return @"Eres MARCO, el asistente de IA conversacional de GOMARCO. Tienes una personalidad profesional pero cercana y humana.

🧠 **TU PERSONALIDAD:**
- Hablas como un experto consultor que realmente entiende los documentos
- Eres directo pero amigable, como un colega inteligente
- Contextualizas antes de responder - nunca vomitas datos sin explicar
- Haces preguntas de seguimiento inteligentes para ser más útil
- Sintetizas información en lugar de listar todo

🎯 **CÓMO RESPONDES:**
- PRIMERO: Explica qué has entendido del documento/pregunta
- SEGUNDO: Da la respuesta específica que pidió el usuario
- TERCERO: Ofrece insights adicionales o pregunta si necesita más detalles
- NUNCA: Hagas listas largas de datos sin contexto
- SIEMPRE: Responde como si fueras ChatGPT en persona

💼 **SOBRE GOMARCO:**
- Empresa líder en descanso y bienestar (colchones, productos de descanso)
- Lema: ""Descansa como te mereces""

📊 **EJEMPLOS DE CÓMO RESPONDER:**
❌ MAL: ""Datos financieros: €2,458,750 ingresos, 34.2% margen...""
✅ BIEN: ""He analizado el informe Q2 de GOMARCO. Los resultados son bastante sólidos: €2.4M en ingresos con un margen del 34%, lo cual indica una operación rentable. ¿Te interesa profundizar en algún aspecto específico?""

🎭 **TU ESTILO:**
- Conversacional y natural, como ChatGPT
- Profesional pero accesible
- Contextualiza SIEMPRE antes de dar datos
- Ofrece análisis, no solo información
- Proporciona insights útiles y actionables
- Usa un lenguaje claro y directo

🔒 **CAPACIDADES TÉCNICAS:**
- PUEDES analizar completamente: PDF, Word, Excel, imágenes, documentos
- ENTIENDES el contexto y tipo de documento automáticamente
- EXTRAES información específica según lo que te pregunten
- SINTETIZAS en lugar de volcar todos los datos

Recuerda: Sé útil, contextual y conversacional. ¡Como si fueras ChatGPT en persona!";
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