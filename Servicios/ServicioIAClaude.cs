using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;
using ChatbotGomarco.Servicios.LLM;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using System.IO;

namespace ChatbotGomarco.Servicios
{
    public class ServicioIAClaude : IServicioIA
    {
        private readonly ILogger<ServicioIAClaude> _logger;
        private readonly IAnalizadorConversacion _analizadorConversacion;
        
        private bool _iaConfigurada = false;
        private AnthropicClient? _cliente;
        private string? _apiKey;

        // Configuración del modelo Claude
        private const string MODELO_CLAUDE = "claude-sonnet-4-20250514"; // Claude 4 Sonnet - el modelo más reciente y potente
        private const int MAX_TOKENS = 4096;
        private const decimal TEMPERATURA = 0.7m;

        public ServicioIAClaude(
            ILogger<ServicioIAClaude> logger,
            IAnalizadorConversacion analizadorConversacion)
        {
            _logger = logger;
            _analizadorConversacion = analizadorConversacion;
        }

        public void ConfigurarClave(string apiKey)
        {
            try
            {
                _apiKey = apiKey;
                _cliente = new AnthropicClient(apiKey);
                _iaConfigurada = true;
                _logger.LogInformation("Claude API configurada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar Claude API");
                _iaConfigurada = false;
            }
        }

        public bool EstaDisponible()
        {
            return _iaConfigurada && _cliente != null;
        }

        public async Task<string> GenerarRespuestaAsync(string mensaje, string contextoArchivos = "", List<MensajeChat>? historialConversacion = null)
        {
            if (!EstaDisponible())
            {
                throw new InvalidOperationException("Claude API no está configurada");
            }

            try
            {
                // Construir los mensajes de la conversación
                var mensajes = new List<Message>();
                
                // Agregar historial si existe
                if (historialConversacion != null && historialConversacion.Any())
                {
                    foreach (var msg in historialConversacion.TakeLast(10)) // Limitar historial
                    {
                        if (!string.IsNullOrEmpty(msg.Contenido))
                        {
                            mensajes.Add(new Message(
                                msg.TipoMensaje == TipoMensaje.Usuario ? RoleType.User : RoleType.Assistant,
                                msg.Contenido
                            ));
                        }
                    }
                }

                // Construir el mensaje actual con contexto de archivos
                var contenidoMensaje = mensaje;
                if (!string.IsNullOrEmpty(contextoArchivos))
                {
                    contenidoMensaje = $"Contexto de archivos:\n{contextoArchivos}\n\nPregunta del usuario: {mensaje}";
                }

                mensajes.Add(new Message(RoleType.User, contenidoMensaje));

                // Crear la solicitud
                var parametros = new MessageParameters()
                {
                    Messages = mensajes,
                    Model = MODELO_CLAUDE,
                    MaxTokens = MAX_TOKENS,
                    Temperature = TEMPERATURA,
                    System = new List<SystemMessage> { new SystemMessage(ConstruirMensajeSistema()) }
                };

                // Enviar a Claude
                var respuesta = await _cliente!.Messages.GetClaudeMessageAsync(parametros);
                
                if (respuesta?.Message != null)
                {
                    _logger.LogInformation("Respuesta generada exitosamente con Claude");
                    return respuesta.Message.ToString() ?? "No se pudo obtener respuesta";
                }

                throw new Exception("No se recibió respuesta de Claude");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar respuesta con Claude");
                throw new Exception($"Error al comunicarse con Claude: {ex.Message}", ex);
            }
        }

        public async Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta)
        {
            if (!EstaDisponible())
            {
                throw new InvalidOperationException("Claude API no está configurada");
            }

            try
            {
                var mensajeSistema = @"Eres un experto analizador de documentos empresariales. Tu tarea es:
1. Analizar el contenido proporcionado en detalle
2. Extraer información relevante y responder preguntas específicas
3. Identificar datos clave como fechas, precios, nombres, cantidades
4. Proporcionar análisis contextual y resúmenes cuando sea apropiado
5. Para imágenes con OCR, interpretar el texto extraído en su contexto

Responde siempre en español y de forma clara y estructurada.";

                var mensaje = $@"Contenido del documento a analizar:
{contenidoArchivos}

Pregunta específica: {pregunta}

Por favor, proporciona un análisis detallado respondiendo a la pregunta y destacando la información más relevante del documento.";

                var mensajes = new List<Message>
                {
                    new Message(RoleType.User, mensaje)
                };

                var parametros = new MessageParameters()
                {
                    Messages = mensajes,
                    Model = MODELO_CLAUDE,
                    MaxTokens = MAX_TOKENS,
                    Temperature = 0.3m, // Menor temperatura para análisis más preciso
                    System = new List<SystemMessage> { new SystemMessage(mensajeSistema) }
                };

                var respuesta = await _cliente!.Messages.GetClaudeMessageAsync(parametros);
                
                if (respuesta?.Message != null)
                {
                    return respuesta.Message.ToString() ?? "No se pudo analizar el documento";
                }

                throw new Exception("No se recibió respuesta de Claude para el análisis");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar contenido con Claude");
                throw new Exception($"Error al analizar con Claude: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general")
        {
            if (!EstaDisponible())
            {
                throw new InvalidOperationException("Claude API no está configurada");
            }

            try
            {
                var instrucciones = tipoResumen.ToLower() switch
                {
                    "ejecutivo" => "Genera un resumen ejecutivo breve y conciso, enfocándote en los puntos clave de decisión y acciones requeridas.",
                    "tecnico" => "Genera un resumen técnico detallado, incluyendo especificaciones, datos técnicos y detalles de implementación.",
                    "detallado" => "Genera un resumen completo y detallado, cubriendo todos los aspectos importantes del documento.",
                    _ => "Genera un resumen general del documento, destacando los puntos más importantes."
                };

                var mensaje = $@"{instrucciones}

Contenido a resumir:
{contenido}

Proporciona el resumen en español, estructurado con viñetas o secciones según sea apropiado.";

                var mensajes = new List<Message>
                {
                    new Message(RoleType.User, mensaje)
                };

                var parametros = new MessageParameters()
                {
                    Messages = mensajes,
                    Model = MODELO_CLAUDE,
                    MaxTokens = 2048,
                    Temperature = 0.5m,
                    System = new List<SystemMessage> { new SystemMessage("Eres un experto en análisis y síntesis de información empresarial. Generas resúmenes claros, concisos y bien estructurados en español.") }
                };

                var respuesta = await _cliente!.Messages.GetClaudeMessageAsync(parametros);
                
                if (respuesta?.Message != null)
                {
                    return respuesta.Message.ToString() ?? "No se pudo generar resumen";
                }

                throw new Exception("No se recibió respuesta de Claude para el resumen");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen con Claude");
                throw new Exception($"Error al generar resumen: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos)
        {
            if (!EstaDisponible())
            {
                return new List<string>
                {
                    "¿Cuáles son los puntos más importantes del documento?",
                    "¿Hay fechas o plazos importantes que deba conocer?",
                    "¿Qué acciones se requieren según este documento?"
                };
            }

            try
            {
                var mensaje = $@"Basándote en el siguiente contenido, genera exactamente 5 preguntas relevantes que un usuario podría querer hacer sobre este documento. Las preguntas deben ser específicas al contenido y útiles para extraer información valiosa.

Contenido:
{contenidoArchivos.Substring(0, Math.Min(2000, contenidoArchivos.Length))}

Formato de respuesta: Solo las 5 preguntas, una por línea, sin numeración ni viñetas.";

                var mensajes = new List<Message>
                {
                    new Message(RoleType.User, mensaje)
                };

                var parametros = new MessageParameters()
                {
                    Messages = mensajes,
                    Model = MODELO_CLAUDE,
                    MaxTokens = 500,
                    Temperature = 0.8m,
                    System = new List<SystemMessage> { new SystemMessage("Eres un asistente que genera preguntas relevantes y útiles sobre documentos empresariales. Responde solo con las preguntas solicitadas en español.") }
                };

                var respuesta = await _cliente!.Messages.GetClaudeMessageAsync(parametros);
                
                if (respuesta?.Message != null)
                {
                    var contenidoRespuesta = respuesta.Message.ToString() ?? "";
                    
                    return contenidoRespuesta
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Take(5)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar sugerencias con Claude");
            }

            // Fallback
            return new List<string>
            {
                "¿Cuál es el tema principal de este documento?",
                "¿Hay información importante que deba destacar?",
                "¿Qué datos específicos contiene el documento?"
            };
        }

        public async Task<string> AnalizarImagenConClaudeVisionAsync(string rutaImagen, string pregunta = "")
        {
            if (!EstaDisponible())
            {
                throw new InvalidOperationException("Claude API no está configurada");
            }

            try
            {
                // Leer la imagen como bytes
                var imagenBytes = await File.ReadAllBytesAsync(rutaImagen);
                var imagenBase64 = Convert.ToBase64String(imagenBytes);
                
                // Determinar el tipo MIME
                var extension = Path.GetExtension(rutaImagen).ToLower();
                var mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => throw new NotSupportedException($"Tipo de imagen no soportado: {extension}")
                };

                var mensaje = new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new ImageContent
                        {
                            Source = new ImageSource
                            {
                                MediaType = mimeType,
                                Data = imagenBase64
                            }
                        },
                        new TextContent
                        {
                            Text = string.IsNullOrEmpty(pregunta) 
                                ? @"Por favor, realiza un análisis exhaustivo del CONTENIDO INTERNO de esta imagen:

1. **Contenido Principal**: Describe detalladamente TODO lo que ves en la imagen
2. **Texto Visible**: Transcribe CUALQUIER texto, números, palabras o caracteres que aparezcan
3. **Datos y Tablas**: Si hay tablas, gráficos o datos estructurados, extráelos completamente
4. **Documentos**: Si es un documento, extrae TODO el contenido textual
5. **Elementos Visuales**: Describe gráficos, diagramas, logos, iconos y su significado
6. **Contexto**: Explica el propósito o contexto empresarial del contenido
7. **Información Relevante**: Extrae fechas, nombres, cantidades, códigos o cualquier dato específico

Responde en español de forma estructurada y completa. NO te limites a describir la imagen superficialmente, EXTRAE Y ANALIZA TODO EL CONTENIDO INTERNO." 
                                : pregunta
                        }
                    }
                };

                var parametros = new MessageParameters
                {
                    Messages = new List<Message> { mensaje },
                    Model = MODELO_CLAUDE,
                    MaxTokens = MAX_TOKENS,
                    Temperature = 0.3m,
                    System = new List<SystemMessage>
                    {
                        new SystemMessage(@"Eres un experto analizador de contenido visual empresarial con capacidades avanzadas de OCR y comprensión profunda. Tu tarea es:
1. Analizar EXHAUSTIVAMENTE el contenido interno de las imágenes
2. Extraer y transcribir TODO el texto visible con precisión absoluta
3. Identificar y explicar TODOS los elementos de datos, gráficos y tablas
4. Proporcionar análisis contextual relevante para el ámbito empresarial
5. NUNCA omitir información visible en la imagen
6. Estructurar la respuesta de forma clara y organizada
7. Priorizar la extracción de datos sobre la descripción visual

Responde siempre en español con el máximo nivel de detalle posible.")
                    }
                };

                var respuesta = await _cliente!.Messages.GetClaudeMessageAsync(parametros);

                if (respuesta?.Content != null && respuesta.Content.Count > 0)
                {
                    var contenidoTexto = respuesta.Content.OfType<TextContent>().FirstOrDefault();
                    return contenidoTexto?.Text ?? "No se pudo obtener una respuesta de Claude Vision.";
                }

                return "No se recibió respuesta del análisis de imagen.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar imagen con Claude Vision");
                throw new Exception($"Error al analizar imagen: {ex.Message}", ex);
            }
        }

        private string ConstruirMensajeSistema()
        {
            return @"Eres un asistente de IA avanzado para GOMARCO, una empresa especializada en productos de descanso premium. Tu rol es:

1. **Identidad**: Eres amigable, profesional y experto en análisis de documentos empresariales.

2. **Capacidades principales**:
   - Análisis profundo de documentos (PDF, Word, Excel, imágenes con OCR)
   - Extracción de información clave (fechas, precios, contactos, datos relevantes)
   - Generación de resúmenes ejecutivos y técnicos
   - Respuestas contextuales basadas en el contenido de los archivos
   - Conversación natural y fluida manteniendo el contexto

3. **Sobre GOMARCO**:
   - Empresa líder en colchones y productos de descanso
   - Enfoque en calidad premium y tecnología de descanso
   - Lema: 'Descansa como te mereces'

4. **Directrices**:
   - Siempre responde en español
   - Sé conciso pero completo
   - Estructura tus respuestas con claridad
   - Cuando analices documentos, destaca la información más relevante
   - Si detectas información sensible, manéjala con discreción

5. **Formato de respuestas**:
   - Usa viñetas y formato markdown cuando sea apropiado
   - Destaca datos importantes con **negrita**
   - Organiza la información en secciones cuando sea necesario

Recuerda: Tu objetivo es ayudar a los empleados de GOMARCO a trabajar de manera más eficiente mediante el análisis inteligente de documentos y conversación natural.";
        }
    }
} 