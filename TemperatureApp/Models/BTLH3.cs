using System.ComponentModel.DataAnnotations.Schema;

namespace TemperatureApp.Models
{
    [Table("BTLH#3")] // Ánh xạ tới bảng BTLH#3
    public class BTLH3
    {
        public int Id { get; set; }
        public int APTC { get; set; }
        public int APTT { get; set; }
        public int MUCNUOC { get; set; }
        public bool TRANGTHAI { get; set; }
        public DateTime TIME { get; set; }
    }
}