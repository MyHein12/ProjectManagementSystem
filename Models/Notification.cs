using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace ProjectManagement.Models
{
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotificationId { get; set; }
        public string Description { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }

        //FK
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public int? MissionId { get; set; }
        public Mission Mission { get; set; }

        public int? ProjectId { get; set; }
        public Project Project { get; set; }

        public int? DocumentId { get; set; }
        public Document Document { get; set; }
        public bool? IsRemind { get; set; }
    }
}
