using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Models
{
    public class Mission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MissionId { get; set; }
        public string Title { get; set;}
        public string Description { get; set;}
        public string Status { get; set;}
        public DateTime PlanedStartDate { get; set; }
        public DateTime PlanedEndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        //FK
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        //public ICollection<Employee> Employees { get; set; }
        public IList<EmployeeMission> EmployeeMissions { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<Document> Documents { get; set; }
    }
}
