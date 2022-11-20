using Concerto.Client.Pages;
using Concerto.Shared.Models.Dto;
using MudBlazor;
using Nito.AsyncEx;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Concerto.Client.Services;

public interface IBreadcrumbsService
{
	public List<BreadcrumbItem> Breadcrumbs { get; set; }
	public EventHandler<List<BreadcrumbItem>>? BreadcrumbsChangeEventHandler { get; set; }
    public void Set(params BreadcrumbItem[] breadcrumbs);
}
public class BreadcrumbsService : IBreadcrumbsService
{
	public List<BreadcrumbItem> Breadcrumbs { get; set; } = new List<BreadcrumbItem>();

	public EventHandler<List<BreadcrumbItem>>? BreadcrumbsChangeEventHandler { get; set; }

    public void Set(params BreadcrumbItem[] breadcrumbs)
	{
		Breadcrumbs = new List<BreadcrumbItem>(breadcrumbs);
		BreadcrumbsChangeEventHandler?.Invoke(this, Breadcrumbs);
    }
}