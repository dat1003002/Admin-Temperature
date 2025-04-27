using System.ComponentModel.DataAnnotations.Schema;

namespace TemperatureApp.Models
{
    [Table("BTLH#4")]
    public class BTLH4
    {
        public int Id { get; set; }
        public int APTC { get; set; }
        public int APTT { get; set; }
        public int MUCNUOC { get; set; }
        public bool TRANGTHAI { get; set; } // Sửa thành bool
        public DateTime TIME { get; set; }
    }
}
