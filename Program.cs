
using RouteDemo.Services;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            #region PROGRAM-SPECIFIC CODE
            builder.Services.AddLogging();
            builder.Services.AddHttpClient(); // to use httpClientFactory
            builder.Services.AddSingleton<ISearchProviderOne>(sp => ActivatorUtilities.CreateInstance<SearchProviderOne>(sp, builder.Configuration["ProviderOneUrl"] ?? string.Empty));
            builder.Services.AddSingleton<ISearchProviderTwo>(sp => ActivatorUtilities.CreateInstance<SearchProviderTwo>(sp, builder.Configuration["ProviderTwoUrl"] ?? string.Empty));
            builder.Services.AddSingleton<ISearchService, SearchService>();
            #endregion

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

#if false // Disabled for local debugging
            app.UseHttpsRedirection();
#endif
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
