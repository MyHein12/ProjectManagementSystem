using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Models;

namespace ProjectManagement.Controllers
{
    public class HRController : Controller
    {
        PMContext objModel = new PMContext();
        private readonly IEmailSender _emailSender;

        public HRController(IEmailSender emailSender)
        {
            this._emailSender = emailSender;
        }
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private List<Notification> GetNotifications(int EmpId)
        {
            return objModel.Notifications.Where(s => s.EmployeeId == EmpId).ToList();
        }
        public IActionResult Home()
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("Home", "SignIn");
            }
            var employeeInformation = objModel.Employees
                .Where(e => e.Role.Name != "CEO" && e.Role.Name != "HR")
                .Include(s => s.Department)
                .Include(s => s.Role)
                .ToList();

            ViewBag.Department = objModel.Departments
                .Where(e => e.Name != "CEO" && e.Name != "HR")
                .ToList();
            ViewBag.Role = objModel.Roles
                .Where(e => e.Name != "CEO" && e.Name != "HR")
                .ToList();
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(employeeInformation);
        }

        [HttpPost]
        public IActionResult UpdateActive(int employeeId, bool isActive)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("Home", "SignIn");
            }
            // Tìm nhân viên trong cơ sở dữ liệu và cập nhật trạng thái
            var employee = objModel.Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
            if (employee != null)
            {
                employee.IsActived = isActive;
                objModel.SaveChanges();
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, error = "Employee not found" });
            }
        }

        [HttpPost]
        public IActionResult AddDepartment(string DepartmentName)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("Home", "SignIn");
            }
            // Tìm nhân viên trong cơ sở dữ liệu và cập nhật trạng thái
            var employee = objModel.Departments.FirstOrDefault(e => e.Name == DepartmentName);
            if (employee != null)
            {
                TempData["error"] = "Department " + DepartmentName + " already exists.";
            }
            else
            {
                objModel.Departments.Add(new Department { Name = DepartmentName });
                objModel.SaveChanges();
                TempData["success"] = "Department " + DepartmentName + " is added";
            }
            return RedirectToAction("Home", "HR");
        }

        
        [HttpPost]
        public async Task<IActionResult> Home(string InputName, string InputEmail, int InputDepartment, string InputPhone, int InputRole)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("Home", "SignIn");
            }
            ViewBag.Department = objModel.Departments
                .Where(e => e.Name != "CEO" && e.Name != "HR")
                .ToList();
            ViewBag.Role = objModel.Roles
                .Where(e => e.Name != "CEO" && e.Name != "HR")
                .ToList();
            if (ModelState.IsValid)
            {
                var employeeInformation1 = objModel.Employees
                .Where(e => e.Role.Name != "CEO" && e.Role.Name != "HR")
                .Include(s => s.Department)
                .Include(s => s.Role)
                .ToList();
                var existingEmployee = objModel.Employees.FirstOrDefault(s => s.Email.Equals(InputEmail) || s.PhoneNumber.Equals(InputPhone));
                if (existingEmployee != null)
                {
                    ViewBag.error = "Email or number phone already exists.";
                    return View(employeeInformation1);
                }
                int manager = objModel.Roles.FirstOrDefault(s => s.Name == "Manager").RoleId;
                if (InputRole == manager)
                {
                    var existingManager = objModel.Employees.FirstOrDefault(s => s.RoleId == manager && s.DepartmentId == InputDepartment && s.IsActived);
                    if (existingManager != null)
                    {
                        ViewBag.error = "Each department can only have 1 manager. If you want to add a new manager, you must deactive the old manager account.";
                        return View(employeeInformation1);
                    }
                }
                string newPassword = GenerateRandomPassword(8);
                string Name = InputName.Split(' ').LastOrDefault();
                var newEmployee = new Employee
                {
                    Email = InputEmail,
                    Password = newPassword,
                    FullName = InputName,
                    PhoneNumber = InputPhone,
                    Rating = 100,
                    IsActived = true,
                    DepartmentId = InputDepartment,
                    RoleId = InputRole,
                };

                objModel.Employees.Add(newEmployee);
                objModel.SaveChanges();

                var employeeInformation = objModel.Employees
                .Where(e => e.Role.Name != "CEO" && e.Role.Name != "HR")
                .Include(s => s.Department)
                .Include(s => s.Role)
                .ToList();

                string subject = "Account Confirmation";
                string message = $@"
                        <html>
                        <body>
                            <p>Dear {Name},</p>
                            <br>
                            <p>Congratulations! Your account has been successfully created. Below is your account information:</p>
                            <br>
                            <div style='font-weight: bold; color:red; border: 1px solid #000; border-radius: 5px; padding: 10px; background-color: #ece7e;'>
                                Email: {InputEmail}
                            </div>
                            <br>
                            <div style='font-weight: bold; color:red; border: 1px solid #000; border-radius: 5px; padding: 10px; background-color: #ece7e;'>
                                Password: {newPassword}
                            </div>
                            <br>
                            <p>Please use the above information to log in to our system.</p>
                            <br>
                            <p>If you have any questions or need assistance, feel free to contact us.</p>
                            <br>
                            <p>Best regards.</p>
                        </body>
                        </html>
                    ";

                try
                {
                    await _emailSender.SendEmailAsync(InputEmail, subject, message);
                    ViewBag.success = "Create new employee successfully.";
                }
                catch (Exception ex)
                {
                    ViewBag.error = ex.ToString();
                }
                return View(employeeInformation);
            }

            ViewBag.error = "Invalid data. Please check your input.";
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View();
        }
    }
}
