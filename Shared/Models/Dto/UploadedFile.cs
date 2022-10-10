namespace Concerto.Shared.Models.Dto;

public record UploadedFile : EntityDto
{
    public string Name { get; init; }
}
