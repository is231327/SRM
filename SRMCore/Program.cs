using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Mappings;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.Agent;
using SRMShared.DTOs.Customer;
using SRMShared.DTOs.MaintenanceWindow;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.DTOs.SensorReading;
using SRMShared.DTOs.ServerRoom;
using SRMShared.DTOs.ShellyDevice;
using SRMShared.Entities;
using Scalar.AspNetCore;

namespace SRMCore;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDbContext<SrmCoreDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("SrmCoreDatabase")));
        builder.Services.AddScoped<ICustomerService, CustomerService>();
        builder.Services.AddScoped<IServerRoomService, ServerRoomService>();
        builder.Services.AddScoped<IAgentService, AgentService>();
        builder.Services.AddScoped<IShellyDeviceService, ShellyDeviceService>();
        builder.Services.AddScoped<IMonitoredDeviceService, MonitoredDeviceService>();
        builder.Services.AddScoped<IMaintenanceWindowService, MaintenanceWindowService>();
        builder.Services.AddScoped<ISensorReadingService, SensorReadingService>();
        builder.Services.AddScoped<ICrudDtoMapper<Customer, CustomerCreateDto, CustomerUpdateDto, CustomerReadDto>, CustomerDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<ServerRoom, ServerRoomCreateDto, ServerRoomUpdateDto, ServerRoomReadDto>, ServerRoomDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<Agent, AgentCreateDto, AgentUpdateDto, AgentReadDto>, AgentDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<ShellyDevice, ShellyDeviceCreateDto, ShellyDeviceUpdateDto, ShellyDeviceReadDto>, ShellyDeviceDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<MonitoredDevice, MonitoredDeviceCreateDto, MonitoredDeviceUpdateDto, MonitoredDeviceReadDto>, MonitoredDeviceDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<MaintenanceWindow, MaintenanceWindowCreateDto, MaintenanceWindowUpdateDto, MaintenanceWindowReadDto>, MaintenanceWindowDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<SensorReading, SensorReadingCreateDto, SensorReadingUpdateDto, SensorReadingReadDto>, SensorReadingDtoMapper>();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        using (IServiceScope scope = app.Services.CreateScope())
        {
            SrmCoreDbContext dbContext = scope.ServiceProvider.GetRequiredService<SrmCoreDbContext>();
            dbContext.Database.EnsureCreated();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(); // Add Scalar (like swagger ;-) )
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
