using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ProjectManagement
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var mail = "thanhdonguyen0711@gmail.com"; // Thay thế bằng địa chỉ email của bạn
            var pwd = "xfniwiqzmiqdzadr"; // Thay thế bằng mật khẩu của bạn

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(mail, pwd),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(mail),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            try
            {
                await client.SendMailAsync(mailMessage);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }
    }
}
