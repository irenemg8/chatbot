using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatbotGomarco.Modelos;
using Microsoft.Extensions.Logging;

namespace ChatbotGomarco.Servicios.LLM
{
    public class AnalizadorConversacion : IAnalizadorConversacion
    {
        private readonly ILogger<AnalizadorConversacion> _logger;

        public AnalizadorConversacion(ILogger<AnalizadorConversacion> logger)
        {
            _logger = logger;
        }

        public TipoConversacion DeterminarTipoConversacion(string mensaje, string contextoArchivos, List<MensajeChat>? historial)
        {
            var mensajeLower = mensaje.ToLowerInvariant();
            
            // Análisis de documento si hay archivos y términos de análisis
            if (!string.IsNullOrEmpty(contextoArchivos) && 
                (mensajeLower.Contains("analiz") || mensajeLower.Contains("documento") || mensajeLower.Contains("archivo")))
            {
                return TipoConversacion.AnalisisDocumento;
            }
            
            // Pregunta técnica compleja
            if (EsPreguntaTecnicaCompleja(mensaje))
            {
                return TipoConversacion.PreguntaTecnica;
            }
                
            // Conversación profunda basada en indicadores y historial
            if (EsConversacionProfunda(mensaje, historial))
            {
                return TipoConversacion.ConversacionProfunda;
            }
                
            // Charla casual
            if (EsCharlaCasual(mensaje))
            {
                return TipoConversacion.CharlaCasual;
            }
                
            return TipoConversacion.RespuestaIntegrativa;
        }

        public string ConstruirContextoCompleto(string mensaje, string contextoArchivos, List<MensajeChat>? historial)
        {
            var contexto = new StringBuilder();
            
            if (!string.IsNullOrEmpty(contextoArchivos))
            {
                contexto.AppendLine($"Contexto de archivos: {contextoArchivos.Substring(0, Math.Min(500, contextoArchivos.Length))}...");
            }
            
            if (historial?.Any() == true)
            {
                var ultimosMensajes = historial.TakeLast(3);
                contexto.AppendLine("Conversación reciente:");
                foreach (var msg in ultimosMensajes)
                {
                    contexto.AppendLine($"- {msg.Contenido}");
                }
            }
            
            return contexto.ToString();
        }

        public List<string> ExtraerPalabrasClave(string mensaje)
        {
            var palabrasComunes = new HashSet<string> { "el", "la", "de", "que", "y", "a", "en", "un", "es", "se", "no", "te", "lo", "le", "da", "su", "por", "son", "con", "para", "como", "muy", "si", "mi", "ya", "del", "más", "qué", "me", "una" };
            
            return mensaje.ToLowerInvariant()
                .Split(new[] { ' ', ',', '.', '?', '¿', '!', '¡', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(palabra => palabra.Length > 2 && !palabrasComunes.Contains(palabra))
                .Take(5)
                .ToList();
        }

        public string AnalizarIntencionUsuario(string mensaje)
        {
            var mensajeLower = mensaje.ToLowerInvariant();
            
            if (mensajeLower.Contains("precio") || mensajeLower.Contains("costo") || mensajeLower.Contains("$"))
                return "precio";
            if (mensajeLower.Contains("fecha") || mensajeLower.Contains("cuando") || mensajeLower.Contains("tiempo"))
                return "fecha";
            if (mensajeLower.Contains("contacto") || mensajeLower.Contains("teléfono") || mensajeLower.Contains("email"))
                return "contacto";
            if (mensajeLower.Contains("procedimiento") || mensajeLower.Contains("proceso") || mensajeLower.Contains("paso"))
                return "procedimiento";
            
            return "general";
        }

        private bool EsPreguntaTecnicaCompleja(string mensaje)
        {
            var indicadoresTecnicos = new[] { "cómo funciona", "por qué", "especificación", "técnico", "proceso", "mecanismo", "algoritmo", "implementación" };
            return indicadoresTecnicos.Any(ind => mensaje.ToLowerInvariant().Contains(ind));
        }

        private bool EsConversacionProfunda(string mensaje, List<MensajeChat>? historial)
        {
            var indicadoresProfundos = new[] { "qué opinas", "qué piensas", "reflexión", "análisis", "perspectiva", "punto de vista", "consideras" };
            var tieneProfundidad = indicadoresProfundos.Any(ind => mensaje.ToLowerInvariant().Contains(ind));
            var tieneHistorial = historial?.Count > 2;
            
            return tieneProfundidad || tieneHistorial;
        }

        private bool EsCharlaCasual(string mensaje)
        {
            var indicadoresCasuales = new[] { "hola", "qué tal", "cómo estás", "gracias", "saludos", "buenas", "hey" };
            return indicadoresCasuales.Any(ind => mensaje.ToLowerInvariant().Contains(ind));
        }
    }
} 