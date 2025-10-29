
namespace ProductService.Models
{
    public class Auditable
    {
        public int Id { get; set; }
        public string? Idsolicitud { get; set; }
        public string? Estado { get; set; }
        public decimal? Monto { get; set; }
        public string? Solicitante { get; set; }
        public string? Aprobador { get; set; }
        public string? Comentario { get; set; }
        public string? Origen { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Accion { get; set; }
        public string? Callbackurl { get; set; }
    }
}