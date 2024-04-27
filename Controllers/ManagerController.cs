using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using ProjectManagement.Models;
using System.Reflection;

namespace ProjectManagement.Controllers
{
    public class ManagerController : Controller
    {
        PMContext objModel = new PMContext();
        private readonly IWebHostEnvironment _webHost;

        public ManagerController(IWebHostEnvironment webHost)
        {
            _webHost = webHost;
        }
        private List<Notification> GetNotifications(int EmpId)
        {
            return objModel.Notifications.Where(s => s.EmployeeId == EmpId)
                .OrderByDescending(s=> s.CreatedDate)
                .ToList();
        }
        public IActionResult Home()
        {
            if(HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            var projectInfor = objModel.ProjectDepartments
                .Include(x => x.Project)
                .Include(x => x.Department)
                .Where(x => x.Department.Name == HttpContext.Session.GetString("Department"))
                .OrderByDescending(s => s.Project.Status)
                .ThenBy(s => s.Project.PlanedEndDate)
                .ToList();
            ViewBag.Missions = objModel.Missions.ToList();
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(projectInfor);
        }
        public IActionResult Project()
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            var projectInfor = objModel.ProjectDepartments
                .Include(x => x.Project)
                .Include(x => x.Department)
                .Where(x => x.Department.Name == HttpContext.Session.GetString("Department"))
                .OrderBy(s => s.Project.PlanedEndDate)
                .ToList();
            ViewBag.Missions = objModel.Missions.ToList();
            ViewBag.Departments = objModel.Departments
                .Where(s => s.Name != HttpContext.Session.GetString("Department") && s.Name != "CEO" && s.Name != "HR")
                .ToList();
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(projectInfor);
        }

        public IActionResult Confirm(int projectId)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            var project = objModel.Projects.FirstOrDefault(s => s.ProjectId == projectId);
            if (project != null)
            {
                // Cập nhật thông tin dự án
                project.Status = "Complete";
                project.ActualEndDate = DateTime.Now;
                objModel.SaveChanges();
            }
            return RedirectToAction("ProjectDetail", "Manager", new { id = projectId });
        }

        public IActionResult UpdateDeadline(int projectId, DateTime newDeadline)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            var project = objModel.Projects.FirstOrDefault(s => s.ProjectId == projectId);
            if (project != null)
            {
                project.PlanedEndDate = newDeadline;
                if(project.Status != "In Progress")
                {
                    project.Status = "In Progress";
                }
                objModel.SaveChanges();

                var DepartmentIds = objModel.ProjectDepartments.Where(s=>s.ProjectId == projectId).ToList();
                if (DepartmentIds != null)
                {
                    if (DepartmentIds != null)
                    {
                        foreach (var id in DepartmentIds)
                        {
                            int roleManager = objModel.Roles.FirstOrDefault(s => s.Name == "Manager").RoleId;
                            Notification newNoti = new Notification
                            {
                                Description = $"Project '{project.Name}' just updated deadline.",
                                IsRead = false,
                                CreatedDate = DateTime.Now,
                                EmployeeId = objModel.Employees.FirstOrDefault(s => s.DepartmentId == id.DepartmentId && s.RoleId == roleManager).EmployeeId,
                                MissionId = null,
                                DocumentId = null,
                                ProjectId = projectId,
                                IsRemind = false
                            };
                            objModel.Notifications.Add(newNoti);
                            objModel.SaveChanges();
                        }
                    }
                }
            }
            return RedirectToAction("ProjectDetail", "Manager", new { id = projectId });
        }

        [HttpPost]
        public async Task<IActionResult> Project(IFormFile? docName, string ProjectName, string Description, DateTime StartDate, DateTime EndDate, List<int>? DepartmentIds)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            if (ModelState.IsValid)
            {
                Project newProject = new Project
                {
                    Name = ProjectName,
                    Description = Description,
                    Status = "In Progress",
                    PlanedStartDate  = StartDate,
                    PlanedEndDate = EndDate,
                    ActualStartDate = null,
                    ActualEndDate = null,
                };
                objModel.Projects.Add(newProject);
                objModel.SaveChanges();

                //tao project roi thi luu document
                if (docName != null)
                {
                    string uploadsFolder = Path.Combine(_webHost.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string Name = Path.GetFileName(docName.FileName);
                    string savePath = Path.Combine(uploadsFolder, Name);

                    using (FileStream stream = new FileStream(savePath, FileMode.Create))
                    {
                        await docName.CopyToAsync(stream);
                    }

                    int empID = HttpContext.Session.GetInt32("EmployeeId") ?? 0;

                    Document newDocument = new Document
                    {
                        Name = Name,
                        Path = savePath,
                        CreatedDate = DateTime.Now,
                        ProjectId = newProject.ProjectId,
                        MissionId = null,
                        EmployeeId = empID
                    };
                    objModel.Documents.Add(newDocument);
                    await objModel.SaveChangesAsync();
                }

                if (HttpContext.Session.GetInt32("DepartmentId") != null)
                {
                    ProjectDepartment newPD = new ProjectDepartment
                    {
                        ProjectId = newProject.ProjectId,
                        DepartmentId = HttpContext.Session.GetInt32("DepartmentId") ?? 0,
                        IsCollaborated = false
                    };
                    objModel.ProjectDepartments.Add(newPD);
                    objModel.SaveChanges();
                }

                if (DepartmentIds != null)
                {
                    foreach (var id in  DepartmentIds) 
                    {
                        ProjectDepartment newPD = new ProjectDepartment
                        {
                            ProjectId = newProject.ProjectId,
                            DepartmentId = id,
                            IsCollaborated = true
                        };
                        objModel.ProjectDepartments.Add(newPD);
                        objModel.SaveChanges();

                        int roleManager = objModel.Roles.FirstOrDefault(s => s.Name == "Manager").RoleId;
                        Notification newNoti = new Notification
                        {
                            Description = "Your department is added to a new project",
                            IsRead = false,
                            CreatedDate = DateTime.Now,
                            EmployeeId = objModel.Employees.FirstOrDefault(s => s.DepartmentId == id && s.RoleId == roleManager).EmployeeId,
                            MissionId = null,
                            DocumentId = null,
                            ProjectId = newProject.ProjectId,
                            IsRemind = false
                        };
                        objModel.Notifications.Add(newNoti);
                        objModel.SaveChanges();
                    }
                }
                ViewBag.success = "Create project successfully";
            }
            else
            {
                ViewBag.error = "Created project failed.";
            }
            var projectInfor = objModel.ProjectDepartments
                .Include(x => x.Project)
                .Include(x => x.Department)
                .Where(x => x.Department.Name == HttpContext.Session.GetString("Department"))
                .ToList();
            ViewBag.Missions = objModel.Missions.ToList();
            ViewBag.Departments = objModel.Departments
                .Where(s => s.Name != HttpContext.Session.GetString("Department") && s.Name != "CEO" && s.Name != "HR")
                .ToList();
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(projectInfor);
        }

        public IActionResult Mission()
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            var projectIds = objModel.ProjectDepartments
            .Where(s => s.Department.Name == HttpContext.Session.GetString("Department"))
            .Select(s => s.ProjectId)
            .ToList();

            var EmployeeMission = objModel.EmployeeMissions
                .Include(x => x.Employee)
                .Include(x => x.Mission)
                .Where(x => projectIds.Contains(x.Mission.ProjectId))
                .OrderBy(s => s.Mission.PlanedEndDate)
                .ToList();
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(EmployeeMission);
        }

        public IActionResult ProjectDetail(int id, int? noti)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            if (noti != null)
            {
                var tb = objModel.Notifications.FirstOrDefault(s => s.NotificationId == noti);
                if (tb != null)
                {
                    tb.IsRead = true;
                    objModel.SaveChanges();
                }
            }
            var project = objModel.ProjectDepartments
                .Include(x => x.Project)
                .Where(x => x.ProjectId == id)
                .Include(x => x.Department)
                .ToList();
            var missions = objModel.Missions
                .Where(m => m.ProjectId == id)
                .ToList();

            ViewBag.Missions = missions;
            if (missions != null)
            {
                var done = missions.Count(s => s.Status == "Done");
                int progress = done * 100 / missions.Count();
                ViewBag.progress = progress;
            }
            var projectDepartments = objModel.ProjectDepartments
                .Include(pd => pd.Department)
                .Where(pd => pd.ProjectId == id)
                .ToList();

            // Retrieve all employees
            var allEmployees = objModel.Employees
                .Include(e => e.Department)
                .Where(s => s.EmployeeId != HttpContext.Session.GetInt32("EmployeeId") && s.RoleId != objModel.Roles.FirstOrDefault(s=>s.Name=="Manager").RoleId)
                .ToList();

            // Filter employees belonging to departments associated with the project
            var employees = allEmployees
                .Where(e => projectDepartments.Any(pd => pd.DepartmentId == e.DepartmentId))
                .OrderByDescending(s => s.Rating)
                .ToList();
            ViewBag.Employees = employees;
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(project);
        }

        [HttpPost]
        public IActionResult ProjectDetail(int id, string MissionName, string Description, DateTime StartDate, DateTime EndDate, List<int>? SelectedEmployees)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            if (ModelState.IsValid)
            {
                var statusProjects = objModel.Projects.FirstOrDefault(s => s.ProjectId == id);
                if (statusProjects != null && statusProjects.Status != "In Progress")
                {
                    statusProjects.Status = "In Progress";
                    objModel.SaveChanges();
                }
                Mission newMission = new Mission
                {
                    Title = MissionName,
                    Description = Description,
                    Status = "To Do",
                    PlanedStartDate = StartDate,
                    PlanedEndDate = EndDate,
                    ActualStartDate = null,
                    ActualEndDate = null,
                    ProjectId = id,
                };
                objModel.Missions.Add(newMission);
                objModel.SaveChanges();

                if (SelectedEmployees != null)
                {
                    foreach(var employee in SelectedEmployees)
                    {
                        EmployeeMission EM = new EmployeeMission
                        {
                            EmployeeId = employee,
                            MissionId = newMission.MissionId,
                            IsCompleted = null
                        };
                        objModel.EmployeeMissions.Add(EM);
                        objModel.SaveChanges();

                        Notification newNoti = new Notification
                        {
                            Description = "You are assigned to a new mission",
                            IsRead = false,
                            CreatedDate = DateTime.Now,
                            EmployeeId = employee,
                            MissionId = newMission.MissionId,
                            DocumentId = null,
                            ProjectId = null,
                            IsRemind = false
                        };
                        objModel.Notifications.Add(newNoti);
                        objModel.SaveChanges();
                    }
                }
                ViewBag.success = "Create mission successfully";
            }
            else
            {
                ViewBag.error = "Created mission failed.";
            }

            var project = objModel.ProjectDepartments
                .Include(x => x.Project)
                .Where(x => x.ProjectId == id)
                .Include(x => x.Department)
                .ToList();
            var missions = objModel.Missions
                .Where(m => m.ProjectId == id)
                .ToList();

            ViewBag.Missions = missions;
            var done = missions.Count(s => s.Status == "Done");
            int progress = done * 100 / missions.Count();
            ViewBag.progress = progress;
            var projectDepartments = objModel.ProjectDepartments
                .Include(pd => pd.Department)
                .Where(pd => pd.ProjectId == id)
                .ToList();

            // Retrieve all employees
            var allEmployees = objModel.Employees
                .Include(e => e.Department)
                .Where(s => s.EmployeeId != HttpContext.Session.GetInt32("EmployeeId") && s.RoleId != objModel.Roles.FirstOrDefault(s => s.Name == "Manager").RoleId)
                .ToList();

            // Filter employees belonging to departments associated with the project
            var employees = allEmployees
                .Where(e => projectDepartments.Any(pd => pd.DepartmentId == e.DepartmentId))
                .ToList();
            ViewBag.Employees = employees;
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(project);
        }
        public IActionResult MissionDetail(int id)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            var misson = objModel.EmployeeMissions
                .Include(x => x.Mission)
                .Where(x => x.MissionId == id)
                .Include(x => x.Employee)
                .ToList();

            var m = misson.GroupBy(g => g.Mission.MissionId)
                                        .Select(g => g.First())
                                        .FirstOrDefault();
            if (m.Mission != null)
            {
                //TH chua hoan thanh mission
                if(m.Mission.ActualEndDate == null)
                {
                    DateTime dueDate = DateTime.Parse(m.Mission.PlanedEndDate.ToString());
                    DateTime currentTime = DateTime.Now;
                    if (currentTime > dueDate)
                    {
                        TimeSpan timePassed = currentTime - dueDate;
                        ViewBag.TimeRemain = $"The deadline has passed. It was {timePassed.Days} days and {timePassed.Hours} hours ago.";
                        ViewBag.Status = "Late";
                    }
                    else
                    {
                        TimeSpan timeRemaining = dueDate - currentTime;
                        ViewBag.TimeRemain = $"{timeRemaining.Days} days and {timeRemaining.Hours} hours.";
                        ViewBag.Status = "Normal";
                    }
                }
                else
                {
                    DateTime dueDate = DateTime.Parse(m.Mission.PlanedEndDate.ToString());
                    DateTime currentTime = DateTime.Parse(m.Mission.ActualEndDate.ToString());
                    if (currentTime > dueDate)
                    {
                        TimeSpan timePassed = currentTime - dueDate;
                        ViewBag.TimeRemain = $"Mission was submitted was {timePassed.Days} days late.";
                        ViewBag.Status = "Late";
                    }
                    else
                    {
                        TimeSpan timeRemaining = dueDate - currentTime;
                        ViewBag.TimeRemain = $"Mission was submitted was {timeRemaining.Days} days early.";
                        ViewBag.Status = "Early";
                    }
                }
            }
            ViewBag.Document = objModel.Documents.FirstOrDefault(s => s.MissionId == id);
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(misson);
        }
    }
}
