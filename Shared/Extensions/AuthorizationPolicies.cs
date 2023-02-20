using Microsoft.AspNetCore.Authorization;

namespace Concerto.Shared.Extensions
{
	public static class AuthorizationPolicies
	{
		public static class IsVerified
		{
			public const string Name = "IsVerified";
			public static AuthorizationPolicy Policy()
			{
				return new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.RequireAssertion(c => c.User.IsVerified())
				.Build();
			}
		}

		public static class IsNotVerified
		{
			public const string Name = "IsNotVerified";
			public static AuthorizationPolicy Policy()
			{
				return new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.RequireAssertion(c => !c.User.IsVerified())
				.Build();
			}
		}

		public static class IsAuthenticated
		{
			public const string Name = "IsAuthenticated";
			public static AuthorizationPolicy Policy()
			{
				return new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.Build();
			}
		}

		public static class IsAdmin
		{
			public const string Name = "IsAdmin";
			public static AuthorizationPolicy Policy()
			{
				return new AuthorizationPolicyBuilder()
				.RequireAssertion(c => c.User.IsAdmin())
				.Build();
			}
		}
		public static class IsTeacher
		{
			public const string Name = "IsTeacher";
			public static AuthorizationPolicy Policy()
			{
				return new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.RequireAssertion(c => c.User.IsTeacher() || c.User.IsAdmin())
				.Build();
			}
		}

	}
}
