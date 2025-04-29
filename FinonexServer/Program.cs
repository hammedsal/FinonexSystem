namespace FinonexServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();


            builder.Services.AddCors(opts =>
            {
                opts.AddPolicy("all", builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            
            var app = builder.Build();

            app.UseCors("all");
            
            // Configure the HTTP request pipeline.
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
