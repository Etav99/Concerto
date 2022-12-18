﻿using Microsoft.AspNetCore.Components;

namespace Concerto.Shared.Client.Extensions;

public static class NavigationManagerExtensions
{
	public static void ToCourse(this NavigationManager navigation, long courseId, string tab)
	{
		navigation.NavigateTo($"courses/{courseId}/{tab}");
	}
}



