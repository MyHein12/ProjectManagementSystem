namespace ProjectManagement.Models
{
    public class EmployeeMission
    {
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public int MissionId { get; set; }
        public Mission Mission { get; set; }
        public Boolean? IsCompleted { get; set; }
    }
}
