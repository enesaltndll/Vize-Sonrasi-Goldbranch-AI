using GoldBranchAI.Data;
using GoldBranchAI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using GoldBranchAI.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddHttpClient();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Gemini AI Service
builder.Services.AddHttpClient<GeminiService>();

// Email Service
builder.Services.AddTransient<EmailService>();
builder.Services.AddSingleton<BillingService>();

// Localization Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<LocalizationService>();
builder.Services.AddTransient<TelegramService>();

// SignalR Service
builder.Services.AddSignalR();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "GitHub";
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
    })
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? "GITHUB_CLIENT_ID";
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? "GITHUB_CLIENT_SECRET";
        options.CallbackPath = "/signin-github";
        options.Scope.Add("user:email");
    });

var app = builder.Build();

// Database Init / Migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // context.Database.EnsureDeleted(); // Kritik: Bir kez başarıyla çalıştıktan sonra güvenlik için kapalı tutuyoruz
        context.Database.EnsureCreated(); // Yeni şemayla yeniden oluşturur veya mevcut olanı kontrol eder

        // AI Provider alanlarını mevcut tabloya ekle (EnsureCreated bunu yapmaz)
        try
        {
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'PreferredAiProvider')
                    ALTER TABLE [Users] ADD [PreferredAiProvider] nvarchar(max) NOT NULL DEFAULT 'default';
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'CustomAiApiKey')
                    ALTER TABLE [Users] ADD [CustomAiApiKey] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'CustomAiModel')
                    ALTER TABLE [Users] ADD [CustomAiModel] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'Bio')
                    ALTER TABLE [Users] ADD [Bio] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'TelegramChatId')
                    ALTER TABLE [Users] ADD [TelegramChatId] nvarchar(max) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Tasks') AND name = 'CompletedAt')
                    ALTER TABLE [Tasks] ADD [CompletedAt] datetime2 NULL;
            ");
        }
        catch { /* Kolonlar zaten varsa hata görmezden gel */ }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the DB.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");
    
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
