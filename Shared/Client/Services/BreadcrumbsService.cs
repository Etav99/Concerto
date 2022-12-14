using MudBlazor;

namespace Concerto.Shared.Client.Services;

public interface IBreadcrumbsService
{
	public List<BreadcrumbItem> Breadcrumbs { get; set; }
	public EventHandler<BreadcrumbsPackage>? BreadcrumbsChangeEventHandler { get; set; }
	public void Set(string icon, string title, params BreadcrumbItem[] breadcrumbs);
}

public class BreadcrumbsService : IBreadcrumbsService
{
	public List<BreadcrumbItem> Breadcrumbs { get; set; } = new List<BreadcrumbItem>();

	public EventHandler<BreadcrumbsPackage>? BreadcrumbsChangeEventHandler { get; set; }

    public void Set(string icon, string title, params BreadcrumbItem[] breadcrumbs)
	{
		Breadcrumbs = new List<BreadcrumbItem>(breadcrumbs);
		BreadcrumbsChangeEventHandler?.Invoke(this, new BreadcrumbsPackage(Breadcrumbs, icon, title));
    }
}

public record struct BreadcrumbsPackage(List<BreadcrumbItem> BreadcrumbItems, string Icon, string Title);