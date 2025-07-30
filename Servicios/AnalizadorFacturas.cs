using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio especializado para análisis inteligente de facturas
    /// </summary>
    public class AnalizadorFacturas : IAnalizadorFacturas
    {
        private readonly ILogger<AnalizadorFacturas> _logger;

        // Patrones para detectar facturas
        private readonly string[] _patronesFactura = {
            @"factura",
            @"invoice",
            @"n[úu]mero.*?(?:factura|invoice)",
            @"fecha.*?(?:factura|invoice|emisi[óo]n)",
            @"total.*?(?:€|$|USD|EUR|\d)",
            @"subtotal",
            @"iva|tax|impuesto",
            @"cantidad|qty|units",
            @"precio.*?unitario|unit.*?price",
            @"descripci[óo]n|description"
        };

        // Patrones para extraer datos específicos
        private readonly Dictionary<string, string> _patronesExtraccion = new()
        {
            ["numero_factura"] = @"(?:factura|invoice|bill|ref)[^\w]*?(?:n[°ºª]?|num|number|#)?\s*[:.]?\s*([A-Z0-9\-/]{3,20})",
            ["fecha"] = @"(?:fecha|date|issued|emitted)[:.]?\s*(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4})",
            ["total"] = @"(?:total|amount|suma)[:.]?\s*([€$]?\s*[\d,]+\.?\d*\s*[€$]?)",
            ["subtotal"] = @"(?:subtotal|sub-total|base)[:.]?\s*([€$]?\s*[\d,]+\.?\d*\s*[€$]?)",
            ["iva"] = @"(?:iva|tax|vat|impuesto)[:.]?\s*([€$]?\s*[\d,]+\.?\d*\s*[€$]?)",
            ["proveedor"] = @"(?:empresa|company|proveedor|supplier|de)[:.]?\s*([A-Za-zÀ-ÿ\s]{2,50})",
            ["vencimiento"] = @"(?:vencimiento|due|expiry)[:.]?\s*(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4})"
        };

        public AnalizadorFacturas(ILogger<AnalizadorFacturas> logger)
        {
            _logger = logger;
        }

        public bool EsFactura(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return false;

            var contenidoLower = contenido.ToLowerInvariant();
            var coincidencias = 0;

            foreach (var patron in _patronesFactura)
            {
                if (Regex.IsMatch(contenidoLower, patron, RegexOptions.IgnoreCase))
                {
                    coincidencias++;
                }
            }

            // Si tiene al menos 3 patrones típicos de factura, es probable que sea una factura
            return coincidencias >= 3;
        }

        public async Task<AnalisisFactura> AnalizarFacturaAsync(string contenidoFactura, string? preguntaEspecifica = null)
        {
            var analisis = new AnalisisFactura();

            try
            {
                // Verificar si es una factura
                analisis.EsFacturaValida = EsFactura(contenidoFactura);

                if (!analisis.EsFacturaValida)
                {
                    analisis.AnalisisCompleto = "❌ El documento no parece ser una factura válida.";
                    return analisis;
                }

                _logger.LogInformation("📄 Analizando factura - Extrayendo datos estructurados");

                // Extraer datos básicos usando patrones
                await ExtraerDatosBasicosAsync(contenidoFactura, analisis);

                // Extraer items si es posible
                await ExtraerItemsFacturaAsync(contenidoFactura, analisis);

                // Generar análisis completo estructurado
                analisis.AnalisisCompleto = GenerarAnalisisCompleto(analisis, preguntaEspecifica);

                _logger.LogInformation("✅ Análisis de factura completado - {NumItems} items encontrados", analisis.Items.Count);

                return analisis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error analizando factura");
                analisis.AnalisisCompleto = $"❌ Error al analizar la factura: {ex.Message}";
                return analisis;
            }
        }

        public string GenerarPromptAnalisisFactura(string contenidoFactura, string pregunta, TipoProveedorIA tipoProveedor)
        {
            return tipoProveedor switch
            {
                TipoProveedorIA.DeepSeek => GenerarPromptDeepSeek(contenidoFactura, pregunta),
                TipoProveedorIA.Claude => GenerarPromptClaude(contenidoFactura, pregunta),
                TipoProveedorIA.OpenAI => GenerarPromptOpenAI(contenidoFactura, pregunta),
                TipoProveedorIA.Ollama => GenerarPromptOllama(contenidoFactura, pregunta),
                _ => GenerarPromptGenerico(contenidoFactura, pregunta)
            };
        }

        private async Task ExtraerDatosBasicosAsync(string contenido, AnalisisFactura analisis)
        {
            await Task.Run(() =>
            {
                // Extraer número de factura
                var matchNumero = Regex.Match(contenido, _patronesExtraccion["numero_factura"], RegexOptions.IgnoreCase);
                if (matchNumero.Success)
                    analisis.NumeroFactura = matchNumero.Groups[1].Value.Trim();

                // Extraer fecha
                var matchFecha = Regex.Match(contenido, _patronesExtraccion["fecha"], RegexOptions.IgnoreCase);
                if (matchFecha.Success)
                    analisis.Fecha = matchFecha.Groups[1].Value.Trim();

                // Extraer total
                var matchTotal = Regex.Match(contenido, _patronesExtraccion["total"], RegexOptions.IgnoreCase);
                if (matchTotal.Success)
                {
                    var totalStr = matchTotal.Groups[1].Value.Trim();
                    analisis.Total = ExtraerNumero(totalStr);
                }

                // Extraer subtotal
                var matchSubtotal = Regex.Match(contenido, _patronesExtraccion["subtotal"], RegexOptions.IgnoreCase);
                if (matchSubtotal.Success)
                {
                    var subtotalStr = matchSubtotal.Groups[1].Value.Trim();
                    analisis.Subtotal = ExtraerNumero(subtotalStr);
                }

                // Extraer IVA
                var matchIva = Regex.Match(contenido, _patronesExtraccion["iva"], RegexOptions.IgnoreCase);
                if (matchIva.Success)
                {
                    var ivaStr = matchIva.Groups[1].Value.Trim();
                    analisis.Impuestos = ExtraerNumero(ivaStr);
                }

                // Extraer proveedor
                var matchProveedor = Regex.Match(contenido, _patronesExtraccion["proveedor"], RegexOptions.IgnoreCase);
                if (matchProveedor.Success)
                    analisis.Proveedor = matchProveedor.Groups[1].Value.Trim();

                // Extraer fecha de vencimiento
                var matchVencimiento = Regex.Match(contenido, _patronesExtraccion["vencimiento"], RegexOptions.IgnoreCase);
                if (matchVencimiento.Success)
                    analisis.FechaVencimiento = matchVencimiento.Groups[1].Value.Trim();

                // Detectar moneda
                if (contenido.Contains("€") || contenido.ToUpper().Contains("EUR"))
                    analisis.Moneda = "EUR";
                else if (contenido.Contains("$") || contenido.ToUpper().Contains("USD"))
                    analisis.Moneda = "USD";
            });
        }

        private async Task ExtraerItemsFacturaAsync(string contenido, AnalisisFactura analisis)
        {
            await Task.Run(() =>
            {
                // Buscar tablas de items (patrón básico)
                var patronItems = @"(?:descripci[óo]n|description|concepto|item)[^\n]*\n((?:[^\n]*\d+[^\n]*\n?){1,20})";
                var matchItems = Regex.Match(contenido, patronItems, RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (matchItems.Success)
                {
                    var lineasItems = matchItems.Groups[1].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var linea in lineasItems.Take(10)) // Máximo 10 items para evitar ruido
                    {
                        var item = ParsearLineaItem(linea);
                        if (item != null)
                        {
                            analisis.Items.Add(item);
                        }
                    }
                }
            });
        }

        private ItemFactura? ParsearLineaItem(string linea)
        {
            try
            {
                // Buscar patrones de cantidad, precio y total en la línea
                var numeros = Regex.Matches(linea, @"[\d,]+\.?\d*").Cast<Match>().Select(m => m.Value).ToList();
                
                if (numeros.Count >= 2)
                {
                    var item = new ItemFactura();
                    
                    // Extraer descripción (texto sin números)
                    var descripcion = Regex.Replace(linea, @"[\d,]+\.?\d*", "").Trim();
                    item.Descripcion = LimpiarTexto(descripcion);

                    // Intentar parsear números
                    if (numeros.Count >= 3)
                    {
                        // Formato típico: cantidad, precio unitario, total
                        item.Cantidad = int.TryParse(numeros[0].Replace(",", ""), out var cant) ? cant : 1;
                        item.PrecioUnitario = ExtraerNumero(numeros[1]);
                        item.Total = ExtraerNumero(numeros[2]);
                    }
                    else if (numeros.Count == 2)
                    {
                        // Solo cantidad y total, o precio unitario y total
                        item.Cantidad = 1;
                        item.Total = ExtraerNumero(numeros[1]);
                    }

                    return string.IsNullOrWhiteSpace(item.Descripcion) ? null : item;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private decimal? ExtraerNumero(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            // Limpiar el texto de símbolos de moneda y espacios
            var textoLimpio = Regex.Replace(texto, @"[€$\s]", "");
            
            // Reemplazar comas por puntos para formato decimal
            textoLimpio = textoLimpio.Replace(",", ".");

            if (decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out var numero))
            {
                return numero;
            }

            return null;
        }

        private string LimpiarTexto(string texto)
        {
            return Regex.Replace(texto.Trim(), @"\s+", " ");
        }

        private string GenerarAnalisisCompleto(AnalisisFactura analisis, string? preguntaEspecifica)
        {
            var resultado = new System.Text.StringBuilder();

            resultado.AppendLine("📄 **ANÁLISIS COMPLETO DE FACTURA**");
            resultado.AppendLine();

            // Información básica
            resultado.AppendLine("🔍 **DATOS PRINCIPALES:**");
            if (!string.IsNullOrEmpty(analisis.NumeroFactura))
                resultado.AppendLine($"• **Número de Factura:** {analisis.NumeroFactura}");
            if (!string.IsNullOrEmpty(analisis.Fecha))
                resultado.AppendLine($"• **Fecha:** {analisis.Fecha}");
            if (!string.IsNullOrEmpty(analisis.Proveedor))
                resultado.AppendLine($"• **Proveedor:** {analisis.Proveedor}");
            if (!string.IsNullOrEmpty(analisis.FechaVencimiento))
                resultado.AppendLine($"• **Vencimiento:** {analisis.FechaVencimiento}");
            resultado.AppendLine();

            // Información financiera
            resultado.AppendLine("💰 **RESUMEN FINANCIERO:**");
            if (analisis.Subtotal.HasValue)
                resultado.AppendLine($"• **Subtotal:** {analisis.Subtotal:C} {analisis.Moneda}");
            if (analisis.Impuestos.HasValue)
                resultado.AppendLine($"• **Impuestos:** {analisis.Impuestos:C} {analisis.Moneda}");
            if (analisis.Total.HasValue)
                resultado.AppendLine($"• **TOTAL:** {analisis.Total:C} {analisis.Moneda}");
            resultado.AppendLine();

            // Items encontrados
            if (analisis.Items.Any())
            {
                resultado.AppendLine($"📋 **ITEMS IDENTIFICADOS ({analisis.Items.Count}):**");
                foreach (var item in analisis.Items.Take(5))
                {
                    resultado.AppendLine($"• **{item.Descripcion}**");
                    if (item.Cantidad > 0)
                        resultado.AppendLine($"  - Cantidad: {item.Cantidad}");
                    if (item.PrecioUnitario.HasValue)
                        resultado.AppendLine($"  - Precio unitario: {item.PrecioUnitario:C}");
                    if (item.Total.HasValue)
                        resultado.AppendLine($"  - Total: {item.Total:C}");
                }
                
                if (analisis.Items.Count > 5)
                    resultado.AppendLine($"... y {analisis.Items.Count - 5} items más");
                    
                resultado.AppendLine();
            }

            // Respuesta a pregunta específica si existe
            if (!string.IsNullOrWhiteSpace(preguntaEspecifica))
            {
                resultado.AppendLine("❓ **RESPUESTA A TU PREGUNTA:**");
                resultado.AppendLine(ResponderPreguntaEspecifica(analisis, preguntaEspecifica));
                resultado.AppendLine();
            }

            // Insights adicionales
            resultado.AppendLine("💡 **INSIGHTS:**");
            var insights = GenerarInsights(analisis);
            foreach (var insight in insights)
            {
                resultado.AppendLine($"• {insight}");
            }

            return resultado.ToString();
        }

        private string ResponderPreguntaEspecifica(AnalisisFactura analisis, string pregunta)
        {
            var preguntaLower = pregunta.ToLowerInvariant();

            if (preguntaLower.Contains("total") || preguntaLower.Contains("precio") || preguntaLower.Contains("costo"))
            {
                return analisis.Total.HasValue 
                    ? $"El total de la factura es {analisis.Total:C} {analisis.Moneda}"
                    : "No pude identificar el total de la factura en el documento.";
            }

            if (preguntaLower.Contains("fecha") || preguntaLower.Contains("cuándo"))
            {
                return !string.IsNullOrEmpty(analisis.Fecha)
                    ? $"La factura está fechada el {analisis.Fecha}"
                    : "No pude identificar la fecha de la factura.";
            }

            if (preguntaLower.Contains("proveedor") || preguntaLower.Contains("empresa") || preguntaLower.Contains("quién"))
            {
                return !string.IsNullOrEmpty(analisis.Proveedor)
                    ? $"La factura es de {analisis.Proveedor}"
                    : "No pude identificar claramente el proveedor.";
            }

            if (preguntaLower.Contains("items") || preguntaLower.Contains("productos") || preguntaLower.Contains("qué"))
            {
                if (analisis.Items.Any())
                {
                    var items = string.Join(", ", analisis.Items.Take(3).Select(i => i.Descripcion));
                    return $"Los principales items son: {items}" + 
                           (analisis.Items.Count > 3 ? $" y {analisis.Items.Count - 3} más." : ".");
                }
                else
                {
                    return "No pude identificar items específicos en la factura.";
                }
            }

            return "He analizado la factura completa. Revisa la información estructurada arriba para más detalles.";
        }

        private List<string> GenerarInsights(AnalisisFactura analisis)
        {
            var insights = new List<string>();

            // Insight sobre completitud de datos
            int datosEncontrados = 0;
            if (!string.IsNullOrEmpty(analisis.NumeroFactura)) datosEncontrados++;
            if (!string.IsNullOrEmpty(analisis.Fecha)) datosEncontrados++;
            if (analisis.Total.HasValue) datosEncontrados++;
            if (!string.IsNullOrEmpty(analisis.Proveedor)) datosEncontrados++;

            if (datosEncontrados >= 3)
                insights.Add("✅ Factura bien estructurada con datos principales identificables");
            else
                insights.Add("⚠️ Algunos datos importantes no están claramente visibles");

            // Insight sobre items
            if (analisis.Items.Any())
                insights.Add($"📋 Se identificaron {analisis.Items.Count} items en el detalle");
            else
                insights.Add("📋 No se pudo extraer el detalle de items automáticamente");

            // Insight financiero
            if (analisis.Total.HasValue && analisis.Subtotal.HasValue && analisis.Impuestos.HasValue)
            {
                var calculoTotal = analisis.Subtotal.Value + analisis.Impuestos.Value;
                if (Math.Abs(calculoTotal - analisis.Total.Value) < 0.01m)
                    insights.Add("✅ Los cálculos de subtotal + impuestos coinciden con el total");
            }

            // Insight sobre fecha de vencimiento
            if (!string.IsNullOrEmpty(analisis.FechaVencimiento))
            {
                insights.Add($"📅 Fecha de vencimiento identificada: {analisis.FechaVencimiento}");
            }

            return insights;
        }

        #region Prompts Especializados por Proveedor

        private string GenerarPromptDeepSeek(string contenidoFactura, string pregunta)
        {
            return $@"[ANÁLISIS DE FACTURA CON DEEPSEEK-R1 - RAZONAMIENTO PASO A PASO]

Analiza sistemáticamente la siguiente factura usando razonamiento lógico:

CONTENIDO DE LA FACTURA:
{contenidoFactura}

PREGUNTA ESPECÍFICA:
{pregunta}

PROCESO DE ANÁLISIS PASO A PASO:

PASO 1: IDENTIFICACIÓN DEL DOCUMENTO
- Confirma que es una factura válida
- Identifica el formato y estructura

PASO 2: EXTRACCIÓN DE DATOS CLAVE
- Número de factura
- Fecha de emisión y vencimiento
- Proveedor/Empresa emisora
- Cliente/Destinatario
- Moneda utilizada

PASO 3: ANÁLISIS FINANCIERO DETALLADO
- Subtotal/Base imponible
- Impuestos (IVA, taxes)
- Total final
- Verificar que los cálculos sean coherentes

PASO 4: DETALLE DE PRODUCTOS/SERVICIOS
- Lista de items/conceptos
- Cantidades y precios unitarios
- Totales por línea

PASO 5: RESPUESTA ESPECÍFICA
- Responder directamente la pregunta planteada
- Proporcionar contexto y razonamiento
- Identificar cualquier anomalía o punto de atención

ANÁLISIS COMPLETO:";
        }

        private string GenerarPromptClaude(string contenidoFactura, string pregunta)
        {
            return $@"[ANÁLISIS DE FACTURA CLAUDE-STYLE - CONVERSACIONAL Y ÚTIL]

Hola! Voy a analizar esta factura de manera clara y útil para ti.

FACTURA A ANALIZAR:
{contenidoFactura}

TU PREGUNTA:
{pregunta}

ENFOQUE DE ANÁLISIS CLAUDE:

🔍 **PRIMERA IMPRESIÓN:**
- ¿Qué tipo de factura es?
- ¿Está bien estructurada?
- ¿Qué llama la atención?

📋 **DATOS PRINCIPALES:**
- Número y fecha de factura
- Empresa que factura
- Cliente/destinatario
- Moneda y montos principales

💰 **ANÁLISIS FINANCIERO:**
- Subtotal, impuestos y total
- ¿Los números tienen sentido?
- Cualquier cosa inusual

📦 **PRODUCTOS/SERVICIOS:**
- ¿Qué se está facturando?
- Cantidades y precios
- ¿Algo que destaque?

✅ **RESPUESTA A TU PREGUNTA:**
- Respuesta directa y útil a lo que preguntaste
- Contexto adicional relevante
- Cualquier consejo o insight útil

Analicemos esta factura juntos:";
        }

        private string GenerarPromptOpenAI(string contenidoFactura, string pregunta)
        {
            return $@"Eres un experto analista de documentos financieros. Analiza la siguiente factura de manera exhaustiva y profesional.

FACTURA:
{contenidoFactura}

PREGUNTA DEL USUARIO:
{pregunta}

INSTRUCCIONES:
1. Extrae TODOS los datos estructurados posibles (números, fechas, nombres, cantidades)
2. Identifica el tipo exacto de documento y su propósito
3. Verifica la consistencia matemática de los cálculos
4. Responde específicamente a la pregunta del usuario
5. Proporciona insights empresariales valiosos
6. Identifica cualquier dato faltante o anomalía
7. Estructura la respuesta de forma profesional y clara

Si es una imagen de factura, analiza también:
- Calidad de la imagen y legibilidad
- Posibles campos ocultos o borrosos
- Estructura visual y formato del documento

ANÁLISIS PROFESIONAL:";
        }

        private string GenerarPromptOllama(string contenidoFactura, string pregunta)
        {
            return $@"[ANÁLISIS RÁPIDO DE FACTURA]

Factura:
{contenidoFactura}

Pregunta: {pregunta}

Analiza y extrae:
1. Número de factura
2. Fecha
3. Proveedor
4. Total
5. Items principales

Respuesta directa y útil:";
        }

        private string GenerarPromptGenerico(string contenidoFactura, string pregunta)
        {
            return $@"Analiza la siguiente factura y responde a la pregunta específica:

FACTURA:
{contenidoFactura}

PREGUNTA:
{pregunta}

Proporciona un análisis estructurado con los datos principales de la factura y responde específicamente a la pregunta.

ANÁLISIS:";
        }

        #endregion
    }
}