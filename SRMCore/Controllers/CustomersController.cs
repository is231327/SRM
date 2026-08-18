using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.Customer;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(
    ICustomerService service,
    ICrudDtoMapper<Customer, CustomerCreateDto, CustomerUpdateDto, CustomerReadDto> mapper)
    : CrudControllerBase<Customer, CustomerCreateDto, CustomerUpdateDto, CustomerReadDto>(service, mapper)
{
}
