using SRMCore.Mappings.Interfaces;
using SRMShared.DTOs.Customer;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public class CustomerDtoMapper : ICrudDtoMapper<Customer, CustomerCreateDto, CustomerUpdateDto, CustomerReadDto>
{
    public CustomerReadDto ToReadDto(Customer entity) => new()
    {
        Id = entity.Id,
        ExternalReference = entity.ExternalReference,
        Name = entity.Name,
        ContactEmail = entity.ContactEmail,
        ContactPhone = entity.ContactPhone,
        IsActive = entity.IsActive,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    public Customer ToEntity(CustomerCreateDto dto) => new()
    {
        ExternalReference = dto.ExternalReference,
        Name = dto.Name,
        ContactEmail = dto.ContactEmail,
        ContactPhone = dto.ContactPhone,
        IsActive = dto.IsActive
    };

    public Customer ToEntity(CustomerUpdateDto dto) => new()
    {
        ExternalReference = dto.ExternalReference,
        Name = dto.Name,
        ContactEmail = dto.ContactEmail,
        ContactPhone = dto.ContactPhone,
        IsActive = dto.IsActive
    };
}
