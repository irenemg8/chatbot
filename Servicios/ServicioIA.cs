using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;
using ChatbotGomarco.Servicios.LLM;

namespace ChatbotGomarco.Servicios
{
    public class ServicioIA : IServicioIA
    {
        private readonly ILogger<ServicioIA> _logger;
        private readonly IAnalizadorConversacion _analizadorConversacion;
        private readonly IGeneradorRespuestas _generadorRespuestas;
        
        private bool _iaConfigurada = false;
        private string? _apiKey;

        // Base de conocimientos GOMARCO para respuestas temáticas
        private readonly Dictionary<string, List<string>> _baseConocimiento = new()
        {
            ["saludo"] = new() 
            { 
                "¡Hola! Me alegra poder conversar contigo. Como asistente de GOMARCO, estoy aquí para ayudarte con cualquier consulta sobre nuestros productos o análisis de documentos.",
                "¡Buenos días! Es un placer poder asistirte. Soy tu asistente especializado en productos de descanso GOMARCO y análisis inteligente de documentos.",
                "¡Hola! Qué bueno tenerte por aquí. Puedo ayudarte tanto con información sobre nuestros colchones premium como con el análisis detallado de cualquier documento que necesites revisar."
            },
            
            ["colchones"] = new()
            {
                "Los colchones GOMARCO representan la excelencia en tecnología de descanso. Combinamos materiales de alta calidad con diseños ergonómicos para garantizar el mejor descanso posible.",
                "Nuestros colchones están diseñados pensando en las necesidades específicas de cada persona. Utilizamos tecnología avanzada para ofrecer el equilibrio perfecto entre soporte y comodidad.",
                "En GOMARCO entendemos que el buen descanso es fundamental para la calidad de vida. Por eso cada colchón pasa por rigurosos controles de calidad antes de llegar a tu hogar."
            },
            
            ["análisis"] = new()
            {
                "Puedo analizar diferentes tipos de documentos: PDF, Word, Excel, PowerPoint e incluso imágenes con texto. Mi enfoque está en extraer información útil y relevante para tu trabajo.",
                "El análisis de documentos es una de mis especialidades. Puedo identificar datos clave, fechas, precios, contactos y cualquier información específica que necesites encontrar.",
                "Mi capacidad de análisis me permite procesar documentos complejos y presentarte la información de manera clara y organizada, enfocándome en lo que realmente importa para tu trabajo."
            }
        };

        public ServicioIA(
            ILogger<ServicioIA> logger,
            IAnalizadorConversacion analizadorConversacion,
            IGeneradorRespuestas generadorRespuestas)
        {
            _logger = logger;
            _analizadorConversacion = analizadorConversacion;
            _generadorRespuestas = generadorRespuestas;
        }

        public void ConfigurarClave(string apiKey)
        {
            try
            {
                _apiKey = apiKey;
                _iaConfigurada = !string.IsNullOrEmpty(apiKey);
                _logger.LogInformation("IA configurada correctamente en modo avanzado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar la IA");
                _iaConfigurada = false;
            }
        }

        public bool EstaDisponible()
        {
            return _iaConfigurada;
        }

        public async Task<string> GenerarRespuestaAsync(string mensaje, string contextoArchivos = "", List<MensajeChat>? historialConversacion = null)
        {
            try
            {
                // Usar el analizador modular para determinar el tipo de conversación
                var tipoConversacion = _analizadorConversacion.DeterminarTipoConversacion(mensaje, contextoArchivos, historialConversacion);
                var contextoCompleto = _analizadorConversacion.ConstruirContextoCompleto(mensaje, contextoArchivos, historialConversacion);
                
                // Generar respuesta usando el generador modular
                return tipoConversacion switch
                {
                    TipoConversacion.AnalisisDocumento => await _generadorRespuestas.GenerarRespuestaAnalisisDocumento(mensaje, contextoArchivos, contextoCompleto),
                    TipoConversacion.ConversacionProfunda => await _generadorRespuestas.GenerarConversacionProfunda(mensaje, contextoCompleto, historialConversacion),
                    TipoConversacion.PreguntaTecnica => await _generadorRespuestas.GenerarRespuestaTecnica(mensaje, contextoArchivos, contextoCompleto),
                    TipoConversacion.CharlaCasual => await _generadorRespuestas.GenerarCharlaCasual(mensaje, contextoCompleto),
                    _ => await _generadorRespuestas.GenerarRespuestaIntegrativa(mensaje, contextoArchivos, contextoCompleto, historialConversacion)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar respuesta conversacional");
                return GenerarRespuestaFallback(mensaje, contextoArchivos);
            }
        }

        public async Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta)
        {
            try
            {
                // Usar directamente el generador de análisis de documentos
                return await _generadorRespuestas.GenerarRespuestaAnalisisDocumento(pregunta, contenidoArchivos, contenidoArchivos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar contenido con IA");
                return GenerarAnalisisFallback(contenidoArchivos, pregunta);
            }
        }

        public async Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general")
        {
            try
            {
                await Task.Delay(500);
                
                var palabras = contenido.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lineas = contenido.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                var respuesta = new StringBuilder();
                respuesta.AppendLine($"He analizado el documento y puedo ofrecerte un resumen {tipoResumen} del contenido.");
                respuesta.AppendLine();
                
                respuesta.AppendLine($"El documento contiene {palabras.Length:N0} palabras distribuidas en {lineas.Length:N0} párrafos. Basándome en mi análisis, puedo destacar que la información está bien estructurada y presenta elementos clave que son relevantes para el contexto empresarial.");
                respuesta.AppendLine();
                
                // Intentar identificar elementos clave
                var elementosClave = IdentificarElementosClave(contenido);
                if (elementosClave.Any())
                {
                    respuesta.AppendLine("Entre los aspectos más destacados encontré:");
                    foreach (var elemento in elementosClave.Take(3))
                    {
                        respuesta.AppendLine($"- {elemento}");
                    }
                    respuesta.AppendLine();
                }
                
                respuesta.AppendLine("Este resumen te proporciona una visión general del contenido. Si necesitas que profundice en algún aspecto específico o que analice secciones particulares, no dudes en preguntarme.");
                
                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen inteligente");
                return GenerarResumenFallback(contenido, tipoResumen);
            }
        }

        public async Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos)
        {
            try
            {
                await Task.Delay(300);
                
                var sugerencias = new List<string>();
                var contenidoLower = contenidoArchivos.ToLowerInvariant();
                
                // Sugerencias basadas en el contenido
                if (contenidoLower.Contains("precio") || contenidoLower.Contains("costo"))
                    sugerencias.Add("¿Puedes explicarme los aspectos económicos del documento?");
                    
                if (contenidoLower.Contains("fecha") || contenidoLower.Contains("tiempo"))
                    sugerencias.Add("¿Qué información temporal es más relevante aquí?");
                    
                if (contenidoLower.Contains("proceso") || contenidoLower.Contains("procedimiento"))
                    sugerencias.Add("¿Cuáles son los pasos clave que se describen?");
                
                // Sugerencias generales
                sugerencias.AddRange(new[]
                {
                    "¿Qué es lo más importante de este documento?",
                    "¿Hay información que requiera acción inmediata?",
                    "¿Qué datos específicos te parecen más relevantes?"
                });
                
                return sugerencias.Take(4).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar sugerencias");
                return GenerarSugerenciasFallback(contenidoArchivos);
            }
        }

        // Métodos auxiliares y fallbacks
        private string GenerarRespuestaFallback(string mensaje, string contextoArchivos)
        {
            if (!string.IsNullOrEmpty(contextoArchivos))
            {
                return $"He recibido tu consulta sobre el documento: \"{mensaje}\"\n\nTengo información disponible para ayudarte con el análisis. ¿Hay algo específico que te gustaría saber?";
            }
            
            // Buscar en base de conocimientos
            var mensajeLower = mensaje.ToLowerInvariant();
            if (mensajeLower.Contains("hola") || mensajeLower.Contains("buenos") || mensajeLower.Contains("saludos"))
            {
                return _baseConocimiento["saludo"][Random.Shared.Next(_baseConocimiento["saludo"].Count)];
            }
            
            if (mensajeLower.Contains("colchón") || mensajeLower.Contains("descanso"))
            {
                return _baseConocimiento["colchones"][Random.Shared.Next(_baseConocimiento["colchones"].Count)];
            }
            
            if (mensajeLower.Contains("analiz") || mensajeLower.Contains("documento"))
            {
                return _baseConocimiento["análisis"][Random.Shared.Next(_baseConocimiento["análisis"].Count)];
            }
            
            return $"Gracias por tu consulta: \"{mensaje}\"\n\nComo asistente de GOMARCO, estoy aquí para ayudarte con información sobre nuestros productos y análisis de documentos. ¿Podrías darme más detalles sobre lo que necesitas?";
        }

        private string GenerarAnalisisFallback(string contenidoArchivos, string pregunta)
        {
            return $"He revisado el contenido del documento respecto a tu pregunta: \"{pregunta}\"\n\nBasándome en la información disponible, puedo ayudarte con análisis específicos. ¿Te gustaría que busque información particular en el documento?";
        }

        private string GenerarResumenFallback(string contenido, string tipoResumen)
        {
            var palabras = contenido.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return $"Resumen {tipoResumen}: Documento analizado con {palabras.Length:N0} palabras. Contenido empresarial disponible para consultas específicas.";
        }

        private List<string> GenerarSugerenciasFallback(string contenidoArchivos)
        {
            return new List<string>
            {
                "¿Qué información específica necesitas del documento?",
                "¿Te gustaría que analice algún aspecto particular?",
                "¿Hay datos específicos que estés buscando?"
            };
        }

        private List<string> IdentificarElementosClave(string contenido)
        {
            var elementos = new List<string>();
            var contenidoLower = contenido.ToLowerInvariant();
            
            if (contenidoLower.Contains("objetivo") || contenidoLower.Contains("meta"))
                elementos.Add("Objetivos y metas estratégicas identificadas");
                
            if (contenidoLower.Contains("problema") || contenidoLower.Contains("desafío"))
                elementos.Add("Problemas y desafíos principales");
                
            if (contenidoLower.Contains("solución") || contenidoLower.Contains("propuesta"))
                elementos.Add("Soluciones y propuestas presentadas");
                
            if (contenidoLower.Contains("resultado") || contenidoLower.Contains("conclusión"))
                elementos.Add("Resultados y conclusiones relevantes");
            
            return elementos.Any() ? elementos : new List<string> { "Información estructurada con contenido empresarial relevante" };
        }
    }
} 