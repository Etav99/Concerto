using Concerto.Server.Extensions;

namespace Concerto.Server.Data.Models;

public class Catalog : Entity
{
	public string Name { get; set; } = null!;
	public long OwnerId { get; set; }
	public User Owner { get; set; } = null!;
	public virtual ICollection<UploadedFile> Files { get; set; } = null!;
	public virtual ICollection<User> UsersSharedTo { get; set; } = null!;
	public virtual ICollection<Session> SharedInSessions { get; set; } = null!;
}

public static partial class ViewModelConversions
{
	public static Dto.CatalogListItem ToCatalogListItem(this Catalog catalog)
	{
		return new Dto.CatalogListItem
		{
			Id = catalog.Id,
			Name = catalog.Name,
			OwnerId = catalog.OwnerId,
		};
	}

	public static Dto.CatalogContent ToCatalogContent(this Catalog catalog)
	{
		return new Dto.CatalogContent
		{
			Id = catalog.Id,
			Name = catalog.Name,
			OwnerId = catalog.OwnerId,
			Files = catalog.Files.ToDto(),
		};
	}

	public static Dto.CatalogSettings ToCatalogSettings(this Catalog catalog)
	{
		return new Dto.CatalogSettings
		{
			Id = catalog.Id,
			Name = catalog.Name,
			OwnerId = catalog.OwnerId,
			UserShares = catalog.UsersSharedTo.Select(x => x.ToDto()).ToList(),
			SessionShares = catalog.SharedInSessions.Select(x => x.ToDto()),
		};
	}

}