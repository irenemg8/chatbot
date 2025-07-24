using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace ChatbotGomarco.Servicios
{
    public class ServicioOpenAI : IServicioIA
    {
        private readonly ILogger<ServicioOpenAI> _logger;
        private OpenAIAPI? _clienteOpenAI;
        private bool _iaConfigurada = false;
        private string? _apiKey;

        // Configuración del modelo - Usamos GPT-4 Turbo (el más potente disponible)
        private const int MAX_TOKENS_RESPUESTA = 4096;
        private const double TEMPERATURA = 0.7;

        public ServicioOpenAI(ILogger<ServicioOpenAI> logger)
        {
            _logger = logger;
        }

        public void ConfigurarClave(string apiKey)
        {
            try
            {
                _apiKey = apiKey;
                _clienteOpenAI = new OpenAIAPI(apiKey);
                _iaConfigurada = true;
                _logger.LogInformation("OpenAI configurado correctamente con GPT-4 Turbo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar OpenAI");
                _iaConfigurada = false;
            }
        }

        public bool EstaDisponible()
        {
            return _iaConfigurada && _clienteOpenAI != null;
        }

        public async Task<string> GenerarRespuestaAsync(string mensaje, string contextoArchivos = "", List<MensajeChat>? historialConversacion = null)
        {
            try
            {
                if (!EstaDisponible())
                {
                    return "El servicio de IA no está configurado. Por favor, configura tu API key de OpenAI.";
                }

                var conversacion = _clienteOpenAI!.Chat.CreateConversation();
                conversacion.Model = Model.GPT4_Turbo;
                conversacion.RequestParameters.Temperature = TEMPERATURA;
                conversacion.RequestParameters.MaxTokens = MAX_TOKENS_RESPUESTA;
                conversacion.RequestParameters.TopP = 0.9;

                // Mensaje del sistema con contexto de GOMARCO
                conversacion.AppendSystemMessage(
                    "Eres un asistente de IA avanzado para GOMARCO, una empresa líder en colchones y productos de descanso. " +
                    "Tu objetivo es proporcionar respuestas precisas, detalladas y útiles. " +
                    "Cuando analices documentos o archivos, debes extraer y comprender toda la información relevante para responder preguntas específicas sobre su contenido. " +
                    "Responde de manera natural, fluida y conversacional, como lo haría un experto humano. " +
                    "Si recibes contenido de archivos, analízalo cuidadosamente y proporciona respuestas basadas en ese contenido específico."
                );

                // Agregar contexto de archivos si existe
                if (!string.IsNullOrEmpty(contextoArchivos))
                {
                    conversacion.AppendUserInput(
                        $"CONTEXTO DE ARCHIVOS CARGADOS:\n{contextoArchivos}\n\n" +
                        "Por favor, utiliza esta información para responder las preguntas del usuario."
                    );
                    
                    // Confirmar que el asistente entendió el contexto
                    conversacion.AppendExampleChatbotOutput(
                        "Entendido. He analizado el contenido de los archivos proporcionados y estoy listo para responder preguntas específicas sobre esa información."
                    );
                }

                // Agregar historial de conversación si existe
                if (historialConversacion?.Any() == true)
                {
                    foreach (var mensajeHistorial in historialConversacion.TakeLast(10)) // Limitar a últimos 10 mensajes
                    {
                        if (mensajeHistorial.TipoMensaje == TipoMensaje.Usuario)
                        {
                            conversacion.AppendUserInput(mensajeHistorial.Contenido);
                        }
                        else if (mensajeHistorial.TipoMensaje == TipoMensaje.Asistente)
                        {
                            conversacion.AppendExampleChatbotOutput(mensajeHistorial.Contenido);
                        }
                    }
                }

                // Agregar el mensaje actual del usuario
                conversacion.AppendUserInput(mensaje);

                // Generar respuesta con GPT-4 Turbo
                var respuesta = await conversacion.GetResponseFromChatbotAsync();
                
                _logger.LogInformation("Respuesta generada exitosamente");
                
                return respuesta ?? "No se pudo generar una respuesta.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar respuesta con OpenAI");
                return $"Error al procesar tu solicitud: {ex.Message}. Por favor, verifica tu API key e intenta nuevamente.";
            }
        }

        public async Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta)
        {
            try
            {
                if (!EstaDisponible())
                {
                    return "El servicio de IA no está configurado.";
                }

                var conversacion = _clienteOpenAI!.Chat.CreateConversation();
                conversacion.Model = Model.GPT4_Turbo;
                conversacion.RequestParameters.Temperature = 0.3; // Menor temperatura para respuestas más precisas
                conversacion.RequestParameters.MaxTokens = MAX_TOKENS_RESPUESTA;
                conversacion.RequestParameters.TopP = 0.9;

                // Sistema especializado en análisis de documentos
                conversacion.AppendSystemMessage(
                    "Eres un experto en análisis de documentos. Tu tarea es analizar cuidadosamente el contenido proporcionado y responder preguntas específicas sobre él. " +
                    "Debes ser preciso, detallado y basar tus respuestas únicamente en la información presente en el documento. " +
                    "Si la información solicitada no está en el documento, indícalo claramente."
                );

                // Contenido del documento
                conversacion.AppendUserInput($"DOCUMENTO A ANALIZAR:\n{contenidoArchivos}");

                // Pregunta del usuario
                conversacion.AppendUserInput($"PREGUNTA: {pregunta}");

                var respuesta = await conversacion.GetResponseFromChatbotAsync();
                
                return respuesta ?? "No se pudo analizar el contenido.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar contenido con OpenAI");
                return $"Error al analizar el documento: {ex.Message}";
            }
        }

        public async Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general")
        {
            try
            {
                if (!EstaDisponible())
                {
                    return "El servicio de IA no está configurado.";
                }

                var conversacion = _clienteOpenAI!.Chat.CreateConversation();
                conversacion.Model = Model.GPT4_Turbo;
                conversacion.RequestParameters.Temperature = 0.5;
                conversacion.RequestParameters.MaxTokens = 1000;
                conversacion.RequestParameters.TopP = 0.9;

                var instruccionResumen = tipoResumen switch
                {
                    "ejecutivo" => "Genera un resumen ejecutivo conciso, destacando los puntos clave para la toma de decisiones.",
                    "tecnico" => "Genera un resumen técnico detallado, incluyendo aspectos técnicos y especificaciones importantes.",
                    "detallado" => "Genera un resumen completo y detallado, cubriendo todos los aspectos importantes del documento.",
                    _ => "Genera un resumen claro y completo del siguiente contenido, destacando los puntos más importantes."
                };

                conversacion.AppendSystemMessage(
                    "Eres un experto en análisis y síntesis de información. " + instruccionResumen
                );

                conversacion.AppendUserInput($"Contenido a resumir:\n{contenido}");

                var respuesta = await conversacion.GetResponseFromChatbotAsync();
                
                return respuesta ?? "No se pudo generar el resumen.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen con OpenAI");
                return $"Error al generar resumen: {ex.Message}";
            }
        }

        public async Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos)
        {
            try
            {
                if (!EstaDisponible())
                {
                    return new List<string> { "Configura tu API key para obtener sugerencias inteligentes." };
                }

                var conversacion = _clienteOpenAI!.Chat.CreateConversation();
                conversacion.Model = Model.GPT4_Turbo;
                conversacion.RequestParameters.Temperature = 0.8;
                conversacion.RequestParameters.MaxTokens = 200;
                conversacion.RequestParameters.TopP = 0.9;

                conversacion.AppendSystemMessage(
                    "Basándote en el contenido del documento proporcionado, genera exactamente 4 preguntas relevantes y útiles que un usuario podría hacer. " +
                    "Las preguntas deben ser específicas, prácticas y demostrar comprensión profunda del contenido. " +
                    "Formato: Una pregunta por línea, sin numeración ni viñetas."
                );

                conversacion.AppendUserInput($"Documento:\n{contenidoArchivos.Substring(0, Math.Min(2000, contenidoArchivos.Length))}");

                var respuesta = await conversacion.GetResponseFromChatbotAsync();
                
                var sugerencias = (respuesta ?? "")
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 10)
                    .Take(4)
                    .ToList();

                if (!sugerencias.Any())
                {
                    sugerencias = new List<string>
                    {
                        "¿Cuáles son los puntos principales del documento?",
                        "¿Hay información específica sobre procedimientos?",
                        "¿Qué datos importantes contiene el archivo?",
                        "¿Puedes explicar el contenido en detalle?"
                    };
                }

                return sugerencias;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar sugerencias con OpenAI");
                return new List<string>
                {
                    "¿Qué información específica necesitas del documento?",
                    "¿Te gustaría un resumen del contenido?",
                    "¿Hay algún tema particular que te interese?"
                };
            }
        }
    }
} 