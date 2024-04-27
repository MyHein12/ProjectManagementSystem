using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;

namespace ProjectManagement.Models
{
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeId { get; set; } // Thuộc tính khóa chính mới
        public string Email {  get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Rating { get; set; }
        public bool IsActived { get; set; }

        //FK
        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        //public ICollection<Mission> Missions { get; set; }
        public IList<EmployeeMission> EmployeeMissions { get; set; }
        public ICollection<Document> Documents { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}
