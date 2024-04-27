using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using ProjectManagement.Models;

namespace ProjectManagement.Controllers
{
    public class EmployeeController : Controller
    {
        PMContext objModel = new PMContext();
        private List<Notification> GetNotifications(int EmpId)
        {
            return objModel.Notifications.Where(s => s.EmployeeId == EmpId)
                .OrderByDescending(s=> s.CreatedDate)
                .ToList();
        }
        public IActionResult Home()
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            var projectInfor = objModel.ProjectDepartments
                .Include(x => x.Project)
                .Include(x => x.Department)
                .Where(x => x.Department.Name == HttpContext.Session.GetString("Department"))
                .ToList();
            ViewBag.Missions = objModel.Missions.ToList();
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
                .ToList();
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(EmployeeMission);
        }

        public IActionResult MissionDetail(int id, int? noti)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            if (noti != null)
            {
                var mi = objModel.Missions.FirstOrDefault(s => s.MissionId == id);
                if (mi != null && mi.Status == "To Do")
                {
                    mi.Status = "In Progress";
                    mi.ActualStartDate = DateTime.Now;
                    objModel.SaveChanges();
                }
                var tb = objModel.Notifications.FirstOrDefault(s => s.NotificationId == noti);
                if (tb != null)
                {
                    tb.IsRead = true;
                    objModel.SaveChanges();
                }
            }

            var checkmission = objModel.Missions.FirstOrDefault(s => s.MissionId == id);
            if (checkmission != null && checkmission.Status == "To Do") 
            {
                checkmission.Status = "In Progress";
                checkmission.ActualStartDate = DateTime.Now;
                objModel.SaveChanges();
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
                if (m.Mission.ActualEndDate == null)
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
            var projectOfMission = objModel.Projects.FirstOrDefault(p => p.ProjectId == m.Mission.ProjectId);
            ViewBag.Attached = objModel.Documents.FirstOrDefault(s => s.ProjectId == projectOfMission.ProjectId);
            ViewBag.Document = objModel.Documents.FirstOrDefault(s => s.MissionId == id);
            ViewBag.Notifications = GetNotifications(HttpContext.Session.GetInt32("EmployeeId") ?? 0);
            return View(misson);
        }
    }
}
