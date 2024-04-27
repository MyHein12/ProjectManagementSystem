using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjectManagement.Models;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Reflection;

namespace ProjectManagement.Controllers
{
	public class HomeController : Controller
	{
        PMContext objModel = new PMContext();
        private readonly IEmailSender _emailSender;
		private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHost;

        public HomeController(IWebHostEnvironment webHost, IEmailSender emailSender, ILogger<HomeController> logger)
        {
            _webHost = webHost;
            this._emailSender = emailSender;
			_logger = logger;
        }

        public void RemindNotification()
        {
            // Ngày hiện tại
            var currentDate = DateTime.Now;

            // Ngày deadline sẽ được tính là currentDate + 2 ngày
            var deadlineDate = currentDate.AddDays(2);

            var missions = objModel.Missions.ToList();
            if (missions != null)
            {
                foreach (var mission in missions)
                {
                    var warningMission = objModel.Notifications.Where(s => s.MissionId == mission.MissionId && s.IsRemind == true).ToList();
                    var employeeMissions = objModel.EmployeeMissions.Where(s => s.MissionId == mission.MissionId).ToList();

                    //TH chua co thong bao nhac nho va da co nhan vien duoc assign vao du an thi thong bao
                    if(warningMission.Count == 0 && employeeMissions != null && mission.PlanedEndDate.Date <= deadlineDate.Date && mission.Status != "Done")
                    {
                        foreach (var EM in employeeMissions)
                        {
                            Notification newNoti = new Notification
                            {
                                Description = $"Mission '{mission.Title}' is nearing its deadline. Please take action.",
                                IsRead = false,
                                CreatedDate = DateTime.Now,
                                EmployeeId = EM.EmployeeId, // Điền ID của nhân viên cần nhận thông báo
                                MissionId = mission.MissionId,
                                DocumentId = null,
                                ProjectId = null,
                                IsRemind = true
                            };
                            objModel.Notifications.Add(newNoti);
                            objModel.SaveChanges();
                        }
                    }

                    if (mission.PlanedEndDate <= currentDate && mission.ActualEndDate == null && mission.Status != "Done")
                    {
                        // Tìm project mà mission thuộc về
                        var project = objModel.ProjectDepartments.Where(p => p.ProjectId == mission.ProjectId).ToList();
                        if (project != null)
                        {
                            foreach(var PD in project)
                            {
                                var manager = objModel.Employees.FirstOrDefault(e => e.Role.Name == "Manager" && e.DepartmentId == PD.DepartmentId);
                                var existRemind = objModel.Notifications.FirstOrDefault(s=> s.MissionId == mission.MissionId && s.ProjectId ==  PD.ProjectId && s.IsRemind==true && s.EmployeeId == manager.EmployeeId);
                                // chi tao remind khi ko ton tai existRemind
                                if (manager != null && existRemind == null) 
                                {
                                    // Gửi thông báo đến manager
                                    Notification newNotification = new Notification
                                    {
                                        Description = $"The mission '{mission.Title}' is overdue. Please take action.",
                                        IsRead = false,
                                        CreatedDate = DateTime.Now,
                                        EmployeeId = manager.EmployeeId,
                                        MissionId = mission.MissionId,
                                        DocumentId = null,
                                        ProjectId = PD.ProjectId,
                                        IsRemind = true
                                    };
                                    objModel.Notifications.Add(newNotification);
                                    objModel.SaveChanges();
                                }
                            }
                        }
                        //update la khong hoan thanh
                        var EMs = objModel.EmployeeMissions.Where(s => s.MissionId == mission.MissionId).ToList();
                        if (EMs != null)
                        {
                            foreach (var em in EMs)
                            {
                                em.IsCompleted = false;
                                objModel.SaveChanges();
                            }
                            UpdateEmployeeRating();
                        }
                    }
                }
            }


            //update warning for project 
            var projects = objModel.Projects.ToList();
            if (projects != null)
            {
                foreach (var project in projects)
                {
                    var warningProject = objModel.Notifications.Where(s => s.ProjectId == project.ProjectId && s.IsRemind == true).ToList();
                    var projectDepartments = objModel.ProjectDepartments.Where(s => s.ProjectId == project.ProjectId).ToList();
                    if (warningProject.Count == 0 && projectDepartments != null && project.PlanedEndDate.Date <= deadlineDate.Date && project.Status != "Complete")
                    {
                        project.Status = "Warning";
                        objModel.SaveChanges();
                        foreach (var PD in projectDepartments)
                        {
                            int roleManager = objModel.Roles.FirstOrDefault(s => s.Name == "Manager").RoleId;
                            Notification newNoti = new Notification
                            {
                                Description = $"Project '{project.Name}' is nearing its deadline. Please take action.",
                                IsRead = false,
                                CreatedDate = DateTime.Now,
                                EmployeeId = objModel.Employees.FirstOrDefault(s => s.DepartmentId == PD.DepartmentId && s.RoleId == roleManager).EmployeeId,
                                MissionId = null,
                                DocumentId = null,
                                ProjectId = project.ProjectId,
                                IsRemind = true
                            };
                            objModel.Notifications.Add(newNoti);
                            objModel.SaveChanges();
                        }
                    }

                    if (project.PlanedEndDate <= currentDate && project.ActualEndDate == null && project.Status != "Complete")
                    {
                        if (projectDepartments != null)
                        {
                            foreach (var PD in projectDepartments)
                            {
                                var manager = objModel.Employees.FirstOrDefault(e => e.Role.Name == "Manager" && e.DepartmentId == PD.DepartmentId);
                                var existRemind = objModel.Notifications.FirstOrDefault(s => s.MissionId == null && s.ProjectId == PD.ProjectId && s.IsRemind == true && s.EmployeeId == manager.EmployeeId);
                                // chi tao remind khi ko ton tai existRemind
                                if (manager != null && existRemind == null)
                                {
                                    // Gửi thông báo đến manager
                                    Notification newNotification = new Notification
                                    {
                                        Description = $"Project '{project.Name}' is overdue. Please take action.",
                                        IsRead = false,
                                        CreatedDate = DateTime.Now,
                                        EmployeeId = manager.EmployeeId,
                                        MissionId = null,
                                        DocumentId = null,
                                        ProjectId = PD.ProjectId,
                                        IsRemind = true
                                    };
                                    objModel.Notifications.Add(newNotification);
                                    objModel.SaveChanges();
                                }
                            }
                        }
                    }


                }
            }
        }

        public void UpdateIsCompleted()
        {
            var currentDate = DateTime.Now;
            var missions = objModel.Missions.ToList();

            if (missions != null)
            {
                foreach (var mission in missions)
                {
                    // Kiểm tra xem ngày kết thúc có sau ngày hiện tại hay không
                    if (mission.PlanedEndDate <= currentDate && mission.Status != "Done")
                    {
                        // Lấy danh sách nhân viên được giao nhiệm vụ
                        var employeeMissions = objModel.EmployeeMissions.Where(em => em.MissionId == mission.MissionId).ToList();
                        if (employeeMissions != null)
                        {
                            foreach (var empMission in employeeMissions)
                            {
                                empMission.IsCompleted = false;
                            }
                            objModel.SaveChanges(); // Lưu thay đổi vào cơ sở dữ liệu
                        }
                    }
                }
            }
        }

        public void UpdateEmployeeRating()
        {
            var employees = objModel.Employees.ToList();
            if (employees != null)
            {
                foreach (var employee in employees)
                {
                    var totalMissions = objModel.EmployeeMissions.Where(s => s.EmployeeId == employee.EmployeeId && s.IsCompleted != null).Count();
                    var completedMissions = objModel.EmployeeMissions.Where(s => s.EmployeeId == employee.EmployeeId && s.IsCompleted != true).Count();
                    if (totalMissions != 0 && completedMissions != 0)
                    {
                        int rating = completedMissions * 100 / totalMissions;
                        employee.Rating = rating;
                        objModel.SaveChanges();
                    }
                }
            }
        }


 

        [HttpGet]
        [Route("ChangePassword")]
        public ActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            return View();
        }
        [HttpPost]
        [Route("ChangePassword")]
        public ActionResult ChangePassword(string OldPassword, string NewPassword)
        {
            var email = HttpContext.Session.GetString("Email");
            if (email == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            else
            {
                var obj = objModel.Employees.FirstOrDefault(s => s.Email == email);
                if (obj != null)
                {
                    if (obj.Password != OldPassword)
                    {
                        ViewBag.error = "Old password is wrong.";
                    }
                    else
                    {
                        obj.Password = NewPassword;
                        objModel.SaveChanges();
                        TempData["success"] = "Your password is changed. Please sign in again.";
                        return RedirectToAction("Logout");
                    }
                }
            }
            return View();
        }

        [HttpGet]
        [Route("ForgotPassword")]
        public ActionResult ForgotPassword()
        {
            if (HttpContext.Session.GetString("Email") != null)
            {
                return RedirectToAction("Home", HttpContext.Session.GetString("Role"));
            }
            return View();
        }

        [HttpPost]
        [Route("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(string InputEmail)
        {
            string newPassword = GenerateRandomPassword(8);
            string subject = "Reset Password";
            string message = $@"
                        <html>
                        <body>
                            <p>Your new password is:</p>
                            <div style='width: 75px; font-weight: bold; color:red; border: 1px solid #000; border-radius: 5px; padding: 10px; background-color: #ece7e;'>
                                {newPassword}
                            </div>
                            <p>Please use this password to log in and consider changing it after logging in.</p>
                        </body>
                        </html>
                    ";

            try
            {
                await _emailSender.SendEmailAsync(InputEmail, subject, message);
                TempData["success"] = "Please check your email to log in again";
                return RedirectToAction("SignIn");
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.ToString();
                return View();
            }
        }


        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpGet]
        [Route("SignIn")]
        public ActionResult SignIn()
        {
            RemindNotification();
            UpdateEmployeeRating();
            if (HttpContext.Session.GetString("Email") != null)
            {
                return RedirectToAction("Home", HttpContext.Session.GetString("Role"));
            }

            if (!objModel.Departments.Any() && !objModel.Roles.Any())
            {
                var departments = new List<Department>
                    {
                        new Department { Name = "CEO" },
                        new Department { Name = "HR" }
                    };
                objModel.Departments.AddRange(departments);
                objModel.SaveChanges();

                var roles = new List<Role>
                    {
                        new Role { Name = "CEO" },
                        new Role { Name = "HR" },
                        new Role { Name = "Manager" },
                        new Role { Name = "Employee" }
                    };
                objModel.Roles.AddRange(roles);
                objModel.SaveChanges();

                var defaultCEO = new Employee
                {
                    Email = "CEOexample@gmail.com",
                    Password = "CEOexample123",
                    FullName = "CEO",
                    PhoneNumber = "0123456789",
                    Rating = 100,
                    IsActived = true,
                    DepartmentId = departments.FirstOrDefault(d => d.Name == "CEO").DepartmentId,
                    RoleId = roles.FirstOrDefault(r => r.Name == "CEO").RoleId
                };
                objModel.Employees.Add(defaultCEO);
                objModel.SaveChanges();

                // Tạo tài khoản HR mặc định
                var defaultHR = new Employee
                {
                    Email = "HRexample@gmail.com",
                    Password = "HRexample123",
                    FullName = "HR",
                    PhoneNumber = "0123456789",
                    Rating = 100,
                    IsActived = true,
                    DepartmentId = departments.FirstOrDefault(d => d.Name == "HR").DepartmentId,
                    RoleId = roles.FirstOrDefault(r => r.Name == "HR").RoleId
                };
                objModel.Employees.Add(defaultHR);
                objModel.SaveChanges();
            }
            return View();
        }

        [HttpPost]
        [Route("SignIn")]
        //[ValidateAntiForgeryToken]
        public ActionResult SignIn(string InputEmail, string InputPassword)
        {
            if (ModelState.IsValid)
            {
                var data = objModel.Employees
                    .Include(s => s.Role)
                    .Include(s => s.Department)
                    .FirstOrDefault(s => s.Email.Equals(InputEmail) && s.Password.Equals(InputPassword));

                if (data != null)
                {
                    if(!data.IsActived)
                    {
                        ViewBag.error = "Account is not yet activated, please wait for HR to approve.";
                        return View();
                    }
                    HttpContext.Session.SetString("Email", InputEmail);
                    HttpContext.Session.SetInt32("EmployeeId", data.EmployeeId);
                    HttpContext.Session.SetInt32("DepartmentId", data.DepartmentId);
                    HttpContext.Session.SetString("FullName", data.FullName);
                    HttpContext.Session.SetString("Role", data.Role.Name);
                    HttpContext.Session.SetString("Department", data.Department.Name);
                    if (data.Role.Name == "Employee")
                    {
                        return RedirectToAction("Mission", "Employee");
                    }
                    return RedirectToAction("Home", data.Role.Name);
                }
                else
                {
                    ViewBag.error = "Email or password is wrong.";
                    return View();
                }
            }
            ViewBag.error = "Login failed";
            return View();
        }

        [HttpGet]
        [Route("SignUp")]
        public ActionResult SignUp()
        {
            if (HttpContext.Session.GetString("Email") != null)
            {
                return RedirectToAction("Home", HttpContext.Session.GetString("Role"));
            }
            var listDepartment = objModel.Departments
                                .Where(e => e.Name != "CEO" && e.Name != "HR")
                            .ToList();
            return View(listDepartment);
        }
        [HttpPost]
        [Route("SignUp")]
        public ActionResult SignUp(string InputName, string InputEmail, int InputDepartment, string InputPhone, string InputPassword)
        {
            if (ModelState.IsValid)
            {
                var existingEmployee = objModel.Employees.FirstOrDefault(s => s.Email.Equals(InputEmail) || s.PhoneNumber.Equals(InputPhone));
                if (existingEmployee != null)
                {
                    ViewBag.error = "Email or number phone already exists.";
                    return View(objModel.Departments.ToList());
                }

                var newEmployee = new Employee
                {
                    Email = InputEmail,
                    Password = InputPassword,
                    FullName = InputName,
                    PhoneNumber = InputPhone,
                    Rating = 100,
                    IsActived = false,
                    DepartmentId = InputDepartment,
                    RoleId = objModel.Roles.First(s => s.Name == "Employee").RoleId,
                };

                objModel.Employees.Add(newEmployee);
                objModel.SaveChanges();

                Notification newNoti = new Notification
                {
                    Description = "Email " + InputEmail + " just registered an account.",
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    EmployeeId = objModel.Employees.FirstOrDefault(s => s.RoleId == objModel.Roles.FirstOrDefault(s => s.Name == "HR").RoleId).EmployeeId,
                    MissionId = null,
                    DocumentId = null,
                    ProjectId = null,
                    IsRemind = false
                };
                objModel.Notifications.Add(newNoti);
                objModel.SaveChanges();

                TempData["success"] = "Your account has been created successfully. Please wait for HR approval before signing in.";
                return RedirectToAction("SignIn");
            }

            ViewBag.error = "Invalid data. Please check your input.";
            return View(objModel.Departments.ToList());
        }
        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("SignIn");
        }

        [HttpPost]
        public async Task<IActionResult> Uploads(IFormFile docName, int? mission, int? project)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
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

            try
            {
                int empID = HttpContext.Session.GetInt32("EmployeeId") ?? 0;

                Document newDocument = new Document
                {
                    Name = Name,
                    Path = savePath,
                    CreatedDate = DateTime.Now,
                    ProjectId = project,
                    MissionId = mission,
                    EmployeeId = empID
                };
                objModel.Documents.Add(newDocument);
                await objModel.SaveChangesAsync();
                if (mission != null)
                {
                    var missionStatus = objModel.Missions.FirstOrDefault(s => s.MissionId == mission);
                    if (missionStatus != null)
                    {
                        missionStatus.Status = "Done";
                        missionStatus.ActualEndDate = newDocument.CreatedDate;
                        objModel.SaveChanges();
                        var allMissionsOfProject = objModel.Missions.Where(s=>s.ProjectId == missionStatus.ProjectId && s.Status != "Done").Count();
                        // Neu tat ca mission da Done thi project tro thanh trang thai Pending va doi duyet.
                        if (allMissionsOfProject == 0)
                        {
                            var updateProject = objModel.Projects.FirstOrDefault(s=>s.ProjectId ==missionStatus.ProjectId);
                            if (updateProject != null)
                            {
                                updateProject.Status = "Pending";
                                objModel.SaveChanges();

                                //thong bao den cac manager de duyet
                                var DepartmentIds = objModel.ProjectDepartments.Where(s => s.ProjectId == updateProject.ProjectId).ToList();
                                if (DepartmentIds != null)
                                {
                                    foreach (var id in DepartmentIds)
                                    {
                                        int roleManager = objModel.Roles.FirstOrDefault(s => s.Name == "Manager").RoleId;
                                        Notification newNoti = new Notification
                                        {
                                            Description = $"All missions of project '{updateProject.Name}'. Please confirm to complete this project.",
                                            IsRead = false,
                                            CreatedDate = DateTime.Now,
                                            EmployeeId = objModel.Employees.FirstOrDefault(s => s.DepartmentId == id.DepartmentId && s.RoleId == roleManager).EmployeeId,
                                            MissionId = null,
                                            DocumentId = null,
                                            ProjectId = updateProject.ProjectId,
                                            IsRemind = false
                                        };
                                        objModel.Notifications.Add(newNoti);
                                        objModel.SaveChanges();
                                    }
                                }
                            }
                        }
                    }

                    var employees = objModel.EmployeeMissions.Where(s => s.MissionId == mission && s.EmployeeId != empID).ToList();
                    //gui thong bao den cac thanh vien trong nhom khi misson da hoan thanh
                    if (employees != null)
                    {
                        foreach (var emp in employees)
                        {
                            Notification newNoti = new Notification
                            {
                                Description = objModel.Employees.FirstOrDefault(s => s.EmployeeId == empID).FullName + " just uploaded a document for an assigned mission.",
                                IsRead = false,
                                CreatedDate = DateTime.Now,
                                EmployeeId = emp.EmployeeId,
                                MissionId = mission,
                                DocumentId = newDocument.DocumentId,
                                ProjectId = null,
                                IsRemind = false
                            };
                            objModel.Notifications.Add(newNoti);
                            // update la da complete mission neu chua bi danh not Completed
                            if (emp.IsCompleted == null)
                            {
                                emp.IsCompleted = true;
                            }
                            objModel.SaveChanges();
                            UpdateEmployeeRating();
                        }
                    }
                }
                TempData["success"] = "Documentation is uploaded successfully.";
                if (mission != null)
                {
                    return RedirectToAction("MissionDetail", "Employee", new { id = mission });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while uploading the documentation.";
            }
            
            return View();
        }

        public IActionResult DeleteDocument(int docId)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            var document = objModel.Documents.FirstOrDefault(s => s.DocumentId == docId);
            if (document != null)
            {
                var mission = document.MissionId;
                document.MissionId = null;
                objModel.SaveChanges();

                var missionStatus = objModel.Missions.FirstOrDefault(s => s.MissionId == mission);
                if (missionStatus != null)
                {
                    missionStatus.Status = "In Progress";
                    missionStatus.ActualEndDate = null;
                    var updateProject = objModel.Projects.FirstOrDefault(s => s.ProjectId == missionStatus.ProjectId);
                    if (updateProject != null)
                    {
                        updateProject.Status = "In Progress";
                    }
                    objModel.SaveChanges();
                }
                if (System.IO.File.Exists(document.Path))
                {
                    System.IO.File.Delete(document.Path);
                }
                int empID = HttpContext.Session.GetInt32("EmployeeId") ?? 0;
                var employees = objModel.EmployeeMissions.Where(s => s.MissionId == mission && s.EmployeeId != empID).ToList();
                if (employees != null)
                {
                    foreach (var emp in employees)
                    {
                        Notification newNoti = new Notification
                        {
                            Description = objModel.Employees.FirstOrDefault(s => s.EmployeeId == empID).FullName + " just deleted a document for an assigned mission.",
                            IsRead = false,
                            CreatedDate = DateTime.Now,
                            EmployeeId = emp.EmployeeId,
                            MissionId = mission,
                            DocumentId = document.DocumentId,
                            ProjectId = null,
                            IsRemind = false
                        };
                        objModel.Notifications.Add(newNoti);
                        emp.IsCompleted = null;
                        // update lai la chua complete mission
                        objModel.SaveChanges();
                        UpdateEmployeeRating();
                    }
                }
                return RedirectToAction("MissionDetail", "Employee", new { id = mission });
            }
            return View(document);
        }
        public IActionResult Downloads(int docId, int? noti)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }

            if (noti != null)
            {
                var notification = objModel.Notifications.FirstOrDefault(s => s.NotificationId == noti);
                if (notification != null)
                {
                    notification.IsRead = true;
                    objModel.SaveChanges();
                }
            }
            var document = objModel.Documents.FirstOrDefault(s => s.DocumentId == docId);
            if (document != null)
            {
                var memory = DownloadFile(document.Path);
                string category = document.Name.Split('.').Last();
                string categoryName = "";
                switch (category)
                {
                    case "txt":
                        categoryName = "text/plain";
                        break;
                    case "pdf":
                        categoryName = "application/pdf";
                        break;
                    case "doc" or "docx":
                        categoryName = "application/vnd.ms-word";
                        break;
                    case "xls" or "xlsx":
                        categoryName = "application/vnd.ms-excel";
                        break;
                    case "png":
                        categoryName = "image/png";
                        break;
                    case "jpg":
                        categoryName = "image/jpg";
                        break;
                    case "jpeg":
                        categoryName = "image/jpeg";
                        break;
                    case "gif":
                        categoryName = "image/gif";
                        break;
                    case "csv":
                        categoryName = "text/csv";
                        break;
                    case "zip":
                        categoryName = "application/zip";
                        break;
                }
                return File(memory.ToArray(), categoryName, document.Name);
            }
            return View();
        }

        private MemoryStream DownloadFile (string uploadPath)
        {
            var memory = new MemoryStream ();
            if(System.IO.File.Exists(uploadPath))
            {
                var net = new System.Net.WebClient();
                var data = net.DownloadData(uploadPath);
                var content = new System.IO.MemoryStream(data);
                memory = content;
            }
            memory.Position = 0;
            return memory;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
    }
}