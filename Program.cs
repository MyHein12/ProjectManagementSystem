using Microsoft.AspNetCore.Authentication.Cookies;
using ProjectManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IEmailSender, EmailSender>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add services to the container.
builder.Services.AddControllersWithViews();

//add session
builder.Services.AddSession(
	options =>
	{
		options.IOTimeout = TimeSpan.FromSeconds(30);
		options.Cookie.HttpOnly = true;
		options.Cookie.IsEssential = true;
	});

builder.Services.AddAuthentication(
	CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(option =>
	{
		option.LoginPath = "/Home/SignIn";
		option.ExpireTimeSpan = TimeSpan.FromSeconds(30);
	});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", context =>
{
	context.Response.Redirect("/SignIn");
	return Task.CompletedTask;
});

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Manager}/{action=SignIn}");
app.UseSession();
app.Run();
