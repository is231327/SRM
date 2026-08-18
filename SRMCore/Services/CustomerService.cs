using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class CustomerService(SrmCoreDbContext dbContext) : CrudService<Customer>(dbContext), ICustomerService
{
}
