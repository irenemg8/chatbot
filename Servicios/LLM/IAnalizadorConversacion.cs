using System.Collections.Generic;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios.LLM
{
    public interface IAnalizadorConversacion
    {
        TipoConversacion DeterminarTipoConversacion(string mensaje, string contextoArchivos, List<MensajeChat>? historial);
        string ConstruirContextoCompleto(string mensaje, string contextoArchivos, List<MensajeChat>? historial);
        List<string> ExtraerPalabrasClave(string mensaje);
        string AnalizarIntencionUsuario(string mensaje);
    }

    public enum TipoConversacion
    {
        AnalisisDocumento,
        ConversacionProfunda,
        PreguntaTecnica,
        CharlaCasual,
        RespuestaIntegrativa
    }
} 