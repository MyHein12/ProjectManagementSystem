using Microsoft.EntityFrameworkCore;

namespace ProjectManagement.Models
{
    public class PMContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Mission> Missions { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmployeeMission> EmployeeMissions { get; set; }
        public DbSet<ProjectDepartment> ProjectDepartments { get; set; }
        //public bool ValidateOnSaveEnabled { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=.\SQLEXPRESS; Database=PMSystemDb; Trusted_Connection=True; TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>()
                .Property(ap => ap.Rating)
                .HasColumnType("decimal(4,1)");

            modelBuilder.Entity<EmployeeMission>().HasKey(sc => new { sc.EmployeeId, sc.MissionId});
            modelBuilder.Entity<EmployeeMission>()
                .HasOne<Employee>(sc => sc.Employee)
                .WithMany(s => s.EmployeeMissions)
                .HasForeignKey(sc => sc.EmployeeId);
            modelBuilder.Entity<EmployeeMission>()
                .HasOne<Mission>(sc => sc.Mission)
                .WithMany(s => s.EmployeeMissions)
                .HasForeignKey(sc => sc.MissionId);

            modelBuilder.Entity<ProjectDepartment>().HasKey(sc => new {sc.ProjectId, sc.DepartmentId});
            modelBuilder.Entity<ProjectDepartment>()
                .HasOne<Project>(sc => sc.Project)
                .WithMany(s => s.ProjectDepartments)
                .HasForeignKey(sc => sc.ProjectId);
            modelBuilder.Entity<ProjectDepartment>()
                .HasOne<Department>(sc => sc.Department)
                .WithMany(s => s.ProjectDepartments)
                .HasForeignKey(sc => sc.DepartmentId);
            base.OnModelCreating(modelBuilder);
        }
    }
}
