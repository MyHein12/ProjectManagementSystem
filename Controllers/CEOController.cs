using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Models;
using System.Collections.Generic;
using System.Reflection;
using OfficeOpenXml;
using System.IO;

namespace ProjectManagement.Controllers
{
    public class CEOController : Controller
    {
        PMContext objModel = new PMContext();
        private readonly IWebHostEnvironment _webHost;

        public CEOController(IWebHostEnvironment webHost)
        {
            _webHost = webHost;
        }
        public IActionResult Home(string? option)
        {
            if (HttpContext.Session.GetString("Email") != "CEOexample@gmail.com")
            {
                return RedirectToAction("SignIn", "Home");
            }
            if (option != null)
            {
                // Lấy ngày bắt đầu của tuần, tháng, và năm hiện tại
                var currentDate = DateTime.Today;
                var firstDayOfCurrentWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
                var firstDayOfCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                var firstDayOfCurrentYear = new DateTime(currentDate.Year, 1, 1);
                switch (option)
                {
                    case "week":
                        // Xử lý cho tuần
                        var missionsThisWeek = objModel.Missions.Where(m => m.PlanedStartDate >= firstDayOfCurrentWeek && m.PlanedStartDate < firstDayOfCurrentWeek.AddDays(7)).ToList();
                        var projectsThisWeek = objModel.Projects.Where(p => p.PlanedStartDate >= firstDayOfCurrentWeek && p.PlanedStartDate < firstDayOfCurrentWeek.AddDays(7)).ToList();
                        ViewBag.Missions = missionsThisWeek;
                        ViewBag.Projects = projectsThisWeek;
                        ViewBag.export = "week";
                        break;

                    case "month":
                        // Xử lý cho tháng
                        var missionsThisMonth = objModel.Missions.Where(m => m.PlanedStartDate >= firstDayOfCurrentMonth && m.PlanedStartDate < firstDayOfCurrentMonth.AddMonths(1)).ToList();
                        var projectsThisMonth = objModel.Projects.Where(p => p.PlanedStartDate >= firstDayOfCurrentMonth && p.PlanedStartDate < firstDayOfCurrentMonth.AddMonths(1)).ToList();
                        ViewBag.Missions = missionsThisMonth;
                        ViewBag.Projects = projectsThisMonth;
                        ViewBag.export = "month";
                        break;

                    case "year":
                        // Xử lý cho năm
                        var missionsThisYear = objModel.Missions.Where(m => m.PlanedStartDate >= firstDayOfCurrentYear && m.PlanedStartDate < firstDayOfCurrentYear.AddYears(1)).ToList();
                        var projectsThisYear = objModel.Projects.Where(p => p.PlanedStartDate >= firstDayOfCurrentYear && p.PlanedStartDate < firstDayOfCurrentYear.AddYears(1)).ToList();
                        ViewBag.Missions = missionsThisYear;
                        ViewBag.Projects = projectsThisYear;
                        ViewBag.export = "year";
                        break;
                }
            }
            ViewBag.Departments = objModel.Departments
                .Where(s => s.Name != "CEO" && s.Name != "HR")
                .ToList();
            return View();
        }

        public IActionResult ExportToExcel(string option, string type)
        {
            if (HttpContext.Session.GetString("Email") != "CEOexample@gmail.com")
            {
                return RedirectToAction("SignIn", "Home");
            }
            // Lấy ngày bắt đầu của tuần, tháng, và năm hiện tại
            var currentDate = DateTime.Today;
            var firstDayOfCurrentWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
            var firstDayOfCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var firstDayOfCurrentYear = new DateTime(currentDate.Year, 1, 1);

            List<Mission>? missions = null;
            List<Project>? projects = null;
            switch (option)
            {
                case "week":
                    missions = objModel.Missions.Where(m => m.PlanedStartDate >= firstDayOfCurrentWeek && m.PlanedStartDate < firstDayOfCurrentWeek.AddDays(7)).ToList();
                    projects = objModel.Projects.Where(p => p.PlanedStartDate >= firstDayOfCurrentWeek && p.PlanedStartDate < firstDayOfCurrentWeek.AddDays(7)).ToList();
                    
                    break;
                case "month":
                    missions = objModel.Missions.Where(m => m.PlanedStartDate >= firstDayOfCurrentMonth && m.PlanedStartDate < firstDayOfCurrentMonth.AddMonths(1)).ToList();
                    projects = objModel.Projects.Where(p => p.PlanedStartDate >= firstDayOfCurrentMonth && p.PlanedStartDate < firstDayOfCurrentMonth.AddMonths(1)).ToList();
                    
                    break;
                case "year":
                    missions = objModel.Missions.Where(m => m.PlanedStartDate >= firstDayOfCurrentYear && m.PlanedStartDate < firstDayOfCurrentYear.AddYears(1)).ToList();
                    projects = objModel.Projects.Where(p => p.PlanedStartDate >= firstDayOfCurrentYear && p.PlanedStartDate < firstDayOfCurrentYear.AddYears(1)).ToList();
                    break;
            }
            if (type=="project" && projects != null && projects.Any())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Projects");

                    // Add header row
                    worksheet.Cells[1, 1].Value = "Project ID";
                    worksheet.Cells[1, 2].Value = "Name";
                    worksheet.Cells[1, 3].Value = "Created Date";
                    worksheet.Cells[1, 4].Value = "Status";
                    // Thêm các cột khác tùy theo dữ liệu cần xuất

                    // Add data rows
                    int row = 2;
                    foreach (var project in projects)
                    {
                        worksheet.Cells[row, 1].Value = project.ProjectId;
                        worksheet.Cells[row, 2].Value = project.Name;
                        worksheet.Cells[row, 3].Value = project.PlanedStartDate.ToString("dd/mm/yyyy hh:mm tt");
                        worksheet.Cells[row, 4].Value = project.Status;
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    // Save the Excel package to a MemoryStream
                    MemoryStream stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    // Return the Excel file as a byte array
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Projects.xlsx");
                }
            }

            if (type == "mission" && missions != null && missions.Any())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Missions");

                    // Add header row
                    worksheet.Cells[1, 1].Value = "Mission ID";
                    worksheet.Cells[1, 2].Value = "Title";
                    worksheet.Cells[1, 3].Value = "Created Date";
                    worksheet.Cells[1, 4].Value = "Status";
                    // Thêm các cột khác tùy theo dữ liệu cần xuất

                    // Add data rows
                    int row = 2;
                    foreach (var mission in missions)
                    {
                        worksheet.Cells[row, 1].Value = mission.MissionId;
                        worksheet.Cells[row, 2].Value = mission.Title;
                        worksheet.Cells[row, 3].Value = mission.PlanedStartDate;
                        worksheet.Cells[row, 4].Value = mission.Status;
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    // Save the Excel package to a MemoryStream
                    MemoryStream stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    // Return the Excel file as a byte array
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Missions.xlsx");
                }
            }

            // Trường hợp không có dữ liệu để xuất
            return Content("No data available to export.");
        }
        [HttpPost]
        public async Task<IActionResult> SetGoal (List<int> DepartmentIds, IFormFile docName)
        {
            if (HttpContext.Session.GetString("Email") != "CEOexample@gmail.com")
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
                    ProjectId = null,
                    MissionId = null,
                    EmployeeId = empID
                };
                objModel.Documents.Add(newDocument);
                await objModel.SaveChangesAsync();

                // Thong Bao den cac manager
                if (DepartmentIds != null)
                {
                    foreach (var id in DepartmentIds)
                    {
                        int roleManager = objModel.Roles.FirstOrDefault(s => s.Name == "Manager").RoleId;
                        Notification newNoti = new Notification
                        {
                            Description = "CEO just sent a document about your department's goal. Click to download now.",
                            IsRead = false,
                            CreatedDate = DateTime.Now,
                            EmployeeId = objModel.Employees.FirstOrDefault(s => s.DepartmentId == id && s.RoleId == roleManager).EmployeeId,
                            MissionId = null,
                            DocumentId = newDocument.DocumentId,
                            ProjectId = null,
                            IsRemind = false
                        };
                        objModel.Notifications.Add(newNoti);
                        objModel.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while uploading the documentation.";
            }
            return RedirectToAction("Home", "CEO");
        }
    }
}
