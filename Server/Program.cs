
namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.MapControllers();

            app.Run();
        }
    }
}
