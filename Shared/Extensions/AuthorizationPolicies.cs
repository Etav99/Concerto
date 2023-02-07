using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concerto.Shared.Extensions
{
	public static class AuthorizationPolicies
	{
		public static AuthorizationPolicy IsConfirmedPolicy()
		{
			return new AuthorizationPolicyBuilder()
			.RequireAuthenticatedUser()
			.RequireAssertion(c => c.User.IsConfirmed())
			.Build();
		}

		public static AuthorizationPolicy IsNotConfirmedPolicy()
		{
			return new AuthorizationPolicyBuilder()
			.RequireAuthenticatedUser()
			.RequireAssertion(c => !c.User.IsConfirmed())
			.Build();
		}

		public static AuthorizationPolicy IsAuthenticated()
		{
			return new AuthorizationPolicyBuilder()
			.RequireAuthenticatedUser()
			.Build();
		}
	}
}
