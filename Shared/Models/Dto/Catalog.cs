namespace Concerto.Shared.Models.Dto;
public record Catalog : EntityDto
{
    public string Name { get; set; } = string.Empty;
    public long OwnerId { get; set; }
    public bool WriteAccess { get; set; } = false;
    public virtual IEnumerable<Dto.UploadedFile>? Files { get; set; }
    public virtual IEnumerable<Dto.User>? UserShares { get; set; }
    public virtual IEnumerable<Dto.Session>? SessionShares { get; set; }
}

public record CreateCatalogRequest
{
    public string Name { get; set; }

    public IEnumerable<long> SharedToSessionIds { get; set; } = Enumerable.Empty<long>();
}