using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatbotGomarco.Modelos
{
    public class SesionChat
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Titulo { get; set; } = "Nueva Conversación";

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime FechaUltimaActividad { get; set; } = DateTime.Now;

        public virtual ICollection<MensajeChat> Mensajes { get; set; } = new List<MensajeChat>();

        public virtual ICollection<ArchivoSubido> ArchivosSubidos { get; set; } = new List<ArchivoSubido>();

        public int CantidadMensajes => Mensajes.Count;

        public bool TieneArchivos => ArchivosSubidos.Count > 0;
    }
} 