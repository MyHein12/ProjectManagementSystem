using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Models
{
    public class Document
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DocumentId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ProjectId { get; set; }
        public Project Project { get; set; }

        public int? MissionId { get; set; }
        public Mission Mission { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
    }
}
