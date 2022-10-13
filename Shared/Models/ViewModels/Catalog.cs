namespace Concerto.Shared.Models.Dto;

public record CatalogListItem : EntityModel
{
    public string Name { get; init; } = string.Empty;
    public long OwnerId { get; init; }
}

public record CatalogContent : EntityModel
{
    public string Name { get; init; } = string.Empty;
    public long OwnerId { get; init; }
    public virtual IEnumerable<Dto.UploadedFile> Files { get; init; } = Enumerable.Empty<Dto.UploadedFile>();
}

public record CatalogSettings : EntityModel
{
    public string Name { get; init; } = string.Empty;
    public long OwnerId { get; init; }
    public virtual IEnumerable<Dto.User> UserShares { get; init; } = Enumerable.Empty<Dto.User>();
    public virtual IEnumerable<Dto.Session> SessionShares { get; init; } = Enumerable.Empty<Dto.Session>();
}

public record CreateCatalogRequest
{
    public string Name { get; set; }
    public IEnumerable<long> SharedToSessionIds { get; set; } = Enumerable.Empty<long>();
    public IEnumerable<long> SharedToUserIds { get; set; } = Enumerable.Empty<long>();
}

public record UpdateCatalogRequest : EntityModel
{
    public string Name { get; set; }
    public IEnumerable<long> SharedToSessionIds { get; set; } = Enumerable.Empty<long>();
    public IEnumerable<long> SharedToUserIds { get; set; } = Enumerable.Empty<long>();
}