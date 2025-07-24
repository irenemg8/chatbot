using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;
using Microsoft.Extensions.Logging;

namespace ChatbotGomarco.Servicios.LLM
{
    public class GeneradorRespuestas : IGeneradorRespuestas
    {
        private readonly ILogger<GeneradorRespuestas> _logger;

        public GeneradorRespuestas(ILogger<GeneradorRespuestas> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerarRespuestaAnalisisDocumento(string pregunta, string contextoArchivos, string contextoCompleto)
        {
            await Task.Delay(Random.Shared.Next(800, 1500));
            
            var respuesta = new StringBuilder();
            var insights = ExtraerInsightsDocumento(contextoArchivos, pregunta);
            
            respuesta.AppendLine($"He analizado a fondo {(contextoArchivos.Contains("página") ? "el documento" : "la información")} que me compartiste, y hay varios aspectos interesantes que quiero comentarte.");
            respuesta.AppendLine();
            
            if (insights.Any())
            {
                respuesta.AppendLine($"Lo que más me llama la atención es que {insights.First().ToLower()}. Esto me hace pensar que podría ser muy relevante para lo que buscas, especialmente considerando el contexto de tu consulta.");
                respuesta.AppendLine();
            }
            
            var datosEspecificos = ExtraerDatosEspecificos(contextoArchivos);
            if (datosEspecificos.Any())
            {
                respuesta.AppendLine($"En cuanto a datos concretos, he identificado {datosEspecificos.Count} elementos cuantitativos que pintan un cuadro bastante claro de la situación. Estos números cuentan una historia coherente.");
                respuesta.AppendLine();
            }
            
            respuesta.AppendLine("En síntesis, considero que este documento aporta perspectivas valiosas que vale la pena integrar en tu análisis. Los elementos identificados se conectan de manera coherente y sugieren oportunidades claras.");
            
            var preguntasSeguimiento = GenerarPreguntasSeguimiento(contextoArchivos, pregunta);
            if (preguntasSeguimiento.Any())
            {
                respuesta.AppendLine();
                respuesta.AppendLine($"Me pregunto {preguntasSeguimiento.First().ToLower()} ¿Has considerado también {preguntasSeguimiento.Skip(1).FirstOrDefault()?.ToLower() ?? "si hay otros aspectos que deberíamos explorar"}?");
            }
            
            return respuesta.ToString();
        }

        public async Task<string> GenerarConversacionProfunda(string mensaje, string contexto, List<MensajeChat>? historial)
        {
            await Task.Delay(Random.Shared.Next(600, 1200));
            
            var respuesta = new StringBuilder();
            var tematicas = IdentificarTematicas(mensaje, historial);
            
            respuesta.AppendLine("Es una reflexión muy interesante la que compartes. Me gusta la profundidad con la que abordas el tema, y me da ganas de explorar estas ideas contigo.");
            respuesta.AppendLine();
            
            if (tematicas.Contains("productos_descanso"))
            {
                respuesta.AppendLine("El tema del descanso es fascinante porque combina ciencia, tecnología y comprensión humana. En GOMARCO hemos aprendido que no se trata solo de comodidad superficial, sino de crear un sistema integral que respalde tanto la recuperación física como el bienestar mental durante las horas de sueño.");
                respuesta.AppendLine();
            }
            else if (tematicas.Contains("estrategia_empresarial"))
            {
                respuesta.AppendLine("La estrategia empresarial efectiva requiere una visión integral que combine la comprensión profunda del mercado con la capacidad de adaptación constante. Es un equilibrio delicado entre innovación, excelencia operativa y comprensión genuina de las necesidades del cliente.");
                respuesta.AppendLine();
            }
            
            respuesta.AppendLine("Al final del día, creo que estas conversaciones profundas son las que realmente enriquecen nuestra perspectiva sobre las cosas. Me parece que cada intercambio como este nos ayuda a ver ángulos que antes no considerábamos. ¿Tú qué opinas sobre esto?");
            
            return respuesta.ToString();
        }

        public async Task<string> GenerarRespuestaTecnica(string mensaje, string contextoArchivos, string contextoCompleto)
        {
            await Task.Delay(Random.Shared.Next(700, 1400));
            
            var respuesta = new StringBuilder();
            respuesta.AppendLine("Perfecto, vamos a profundizar en los aspectos técnicos de tu consulta. Me gusta poder abordar estos temas con el nivel de detalle que merecen.");
            respuesta.AppendLine();
            
            if (!string.IsNullOrEmpty(contextoArchivos))
            {
                respuesta.AppendLine("Basándome en la documentación técnica que he analizado, puedo darte una perspectiva bastante completa sobre los procesos y mecanismos involucrados.");
                respuesta.AppendLine();
            }
            
            respuesta.AppendLine("Desde un punto de vista técnico, hay varios factores que entran en juego aquí. Los procesos siguen patrones establecidos, pero también hay elementos específicos del contexto que debemos considerar cuidadosamente. La implementación efectiva requiere atención tanto a los aspectos estructurales como a los detalles operativos que a menudo marcan la diferencia entre un resultado satisfactorio y uno excepcional.");
            respuesta.AppendLine();
            respuesta.AppendLine("Espero que esta explicación técnica te sea útil para tu comprensión del tema. Si necesitas que profundice en algún aspecto específico o si tienes más preguntas técnicas, estaré encantado de continuar explorando estos detalles contigo.");
            
            return respuesta.ToString();
        }

        public async Task<string> GenerarCharlaCasual(string mensaje, string contexto)
        {
            await Task.Delay(Random.Shared.Next(300, 600));
            
            var respuestas = new List<string>
            {
                "Me parece muy interesante lo que planteas. En mi experiencia conversando sobre estos temas, he notado que cada perspectiva aporta algo valioso. ¿Te ha pasado algo similar antes?",
                "Es una pregunta muy buena la que haces. Me gusta poder conversar sobre estas cosas de forma relajada contigo.",
                "Qué bueno que me preguntes sobre esto. Siempre disfruto estas conversaciones más naturales donde podemos explorar ideas sin prisa."
            };
            
            return respuestas[Random.Shared.Next(respuestas.Count)];
        }

        public async Task<string> GenerarRespuestaIntegrativa(string mensaje, string contextoArchivos, string contextoCompleto, List<MensajeChat>? historial)
        {
            await Task.Delay(Random.Shared.Next(600, 1100));
            
            var respuesta = new StringBuilder();
            var elementosContexto = AnalizarElementosContexto(mensaje, contextoArchivos, historial);
            
            if (elementosContexto.Contains("archivos") && elementosContexto.Contains("conversacion_previa"))
            {
                respuesta.AppendLine("Perfecto, continuamos con el análisis de los documentos. Me parece genial poder seguir profundizando en esta información contigo.");
            }
            else if (elementosContexto.Contains("archivos"))
            {
                respuesta.AppendLine("Excelente, vamos a revisar esta información juntos. Me gusta poder ayudarte con el análisis de documentos porque siempre hay detalles interesantes que descubrir.");
            }
            else
            {
                respuesta.AppendLine("Claro, vamos a abordar tu consulta de manera integral.");
            }
            respuesta.AppendLine();
            
            if (!string.IsNullOrEmpty(contextoArchivos))
            {
                respuesta.AppendLine("He revisado cuidadosamente el material que me has proporcionado y puedo ofrecerte una perspectiva integral sobre su contenido. La información está bien estructurada y hay varios puntos que vale la pena destacar por su relevancia y potencial impacto.");
                respuesta.AppendLine();
            }
            
            var respuestasDirectas = new List<string>
            {
                "Para responder directamente a tu pregunta, basándome en mi análisis y experiencia en estos temas:",
                "Si me permites ser directo en mi respuesta, después de considerar todos los elementos, creo que:",
                "Yendo al grano de lo que me preguntas, mi perspectiva es la siguiente:"
            };
            respuesta.AppendLine(respuestasDirectas[Random.Shared.Next(respuestasDirectas.Count)]);
            respuesta.AppendLine();
            
            if (elementosContexto.Contains("archivos"))
            {
                respuesta.AppendLine("Además de lo que hemos discutido directamente, creo que vale la pena considerar las implicaciones más amplias de esta información. A menudo, los documentos nos dan pistas sobre tendencias y oportunidades que no son inmediatamente evidentes, pero que pueden ser muy valiosas para la toma de decisiones estratégicas.");
            }
            else
            {
                respuesta.AppendLine("Pensando más allá de la pregunta inicial, me parece que hay oportunidades interesantes para explorar conceptos relacionados que podrían enriquecer tu comprensión del tema y abrir nuevas perspectivas sobre el panorama general.");
            }
            respuesta.AppendLine();
            
            var cierres = new List<string>
            {
                "Espero que mi análisis te sea útil y te dé una buena base para continuar explorando el tema. Si quieres que profundice en algún aspecto específico o si surgen nuevas preguntas, estaré encantado de continuar la conversación.",
                "¿Te parece que esto responde a lo que buscabas? Me encanta poder ayudarte de esta manera, y siempre puedes pedirme que desarrolle más cualquier punto que te resulte particularmente interesante o relevante."
            };
            respuesta.AppendLine(cierres[Random.Shared.Next(cierres.Count)]);
            
            return respuesta.ToString();
        }

        // Métodos auxiliares privados
        private List<string> ExtraerInsightsDocumento(string contenido, string pregunta)
        {
            var insights = new List<string>();
            var contenidoLower = contenido.ToLowerInvariant();
            
            if (contenidoLower.Contains("aumentó") || contenidoLower.Contains("incremento"))
                insights.Add("hay una tendencia de crecimiento que podría indicar una oportunidad positiva");
                
            if (contenidoLower.Contains("disminuy") || contenidoLower.Contains("redujo"))
                insights.Add("se observa una tendencia decreciente que merece atención especial");
                
            if (Regex.IsMatch(contenidoLower, @"\$[\d,]+") || contenidoLower.Contains("precio"))
                insights.Add("los aspectos económicos son un factor clave en esta información");
                
            if (contenidoLower.Contains("cliente") || contenidoLower.Contains("usuario"))
                insights.Add("el enfoque en la experiencia del cliente es un elemento central");
                
            return insights.Any() ? insights : new List<string> { "el documento presenta información estructurada que requiere análisis detallado" };
        }

        private List<string> ExtraerDatosEspecificos(string contenido)
        {
            var datos = new List<string>();
            
            // Buscar números, fechas, porcentajes
            var patronNumeros = @"\d+(?:[.,]\d+)?";
            var patronFechas = @"\d{1,2}[/\-\.]\d{1,2}[/\-\.]\d{2,4}";
            var patronPorcentajes = @"\d+(?:[.,]\d+)?%";
            
            if (Regex.IsMatch(contenido, patronNumeros)) datos.Add("valores numéricos");
            if (Regex.IsMatch(contenido, patronFechas)) datos.Add("fechas específicas");
            if (Regex.IsMatch(contenido, patronPorcentajes)) datos.Add("porcentajes");
            
            return datos;
        }

        private List<string> GenerarPreguntasSeguimiento(string contexto, string preguntaOriginal)
        {
            var preguntas = new List<string>
            {
                "qué factores externos podrían estar influyendo en esta situación",
                "si hay patrones similares en otros contextos que conozcas",
                "qué métricas adicionales te gustaría evaluar"
            };
            
            return preguntas.Take(2).ToList();
        }

        private List<string> IdentificarTematicas(string mensaje, List<MensajeChat>? historial)
        {
            var tematicas = new List<string>();
            var mensajeLower = mensaje.ToLowerInvariant();
            
            if (mensajeLower.Contains("colchón") || mensajeLower.Contains("descanso")) 
                tematicas.Add("productos_descanso");
            if (mensajeLower.Contains("negocio") || mensajeLower.Contains("empresa")) 
                tematicas.Add("estrategia_empresarial");
            if (mensajeLower.Contains("cliente") || mensajeLower.Contains("usuario")) 
                tematicas.Add("experiencia_cliente");
            
            return tematicas.Any() ? tematicas : new List<string> { "conversacion_general" };
        }

        private List<string> AnalizarElementosContexto(string mensaje, string contextoArchivos, List<MensajeChat>? historial)
        {
            var elementos = new List<string>();
            if (!string.IsNullOrEmpty(contextoArchivos)) elementos.Add("archivos");
            if (historial?.Any() == true) elementos.Add("conversacion_previa");
            if (mensaje.Contains("?")) elementos.Add("pregunta_directa");
            return elementos;
        }
    }
} 