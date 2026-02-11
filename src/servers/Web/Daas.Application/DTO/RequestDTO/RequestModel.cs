using Daas.Application.Users.Queries;

namespace Daas.Application.DTO.RequestDTO;

public class RequestModel
{
    public required string  fieldName { get; set; } 
    public required FieldType fieldType { get; set; }
}
