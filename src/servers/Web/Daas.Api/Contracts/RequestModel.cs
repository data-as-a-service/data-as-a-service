using Daas.Api.Generation;

namespace Daas.Api.Contracts;

public class RequestModel
{
    public required string  fieldName { get; set; }
    public required FieldType fieldType { get; set; }
}
