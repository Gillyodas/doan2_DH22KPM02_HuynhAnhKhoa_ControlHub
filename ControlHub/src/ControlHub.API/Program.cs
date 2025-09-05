using ControlHub.API.Configurations;
using ControlHub.Application.Accounts.Commands.CreateAccount;
using ControlHub.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ControlHub.Application.Common.Behaviors;
using FluentValidation;

namespace ControlHub.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(ControlHub.Application.AssemblyReference.Assembly));
            builder.Services.AddValidatorsFromAssemblyContaining<CreateAccountCommandValidator>();
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Load extra config files BEFORE services use them
            builder.Configuration
                .AddJsonFile("Configurations/DBSettings.json", optional: true, reloadOnChange: true);

            // Add services to the container.

            // 1. API level ***************************************************************************************

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            //*****************************************************************************************************



            // 2. Infrastructure **********************************************************************************

            // Register Infrastructure service identifiers
            builder.Services.AddInfrastructure();

            // Register DbContext
            builder.Services.AddDatabase(builder.Configuration);

            //*****************************************************************************************************

            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(ControlHub.Application.AssemblyReference.Assembly));

            var app = builder.Build();

            app.UseMiddleware<ValidationExceptionMiddleware>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
