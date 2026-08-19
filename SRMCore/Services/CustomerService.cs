using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class CustomerService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<Customer>(dbContext, currentUserContext), ICustomerService
{
}
