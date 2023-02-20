using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;

namespace Concerto.Server.Extensions;
class BadRequestEventListener : IObserver<KeyValuePair<string, object>>, IDisposable
{
	private readonly IDisposable _subscription;
	private readonly Action<IBadRequestExceptionFeature> _callback;

	public BadRequestEventListener(DiagnosticListener diagnosticListener, Action<IBadRequestExceptionFeature> callback)
	{
		_subscription = diagnosticListener.Subscribe(this!, IsEnabled);
		_callback = callback;
	}
	private static readonly Predicate<string> IsEnabled = (provider) => provider switch
	{
		"Microsoft.AspNetCore.Server.Kestrel.BadRequest" => true,
		_ => false
	};
	public void OnNext(KeyValuePair<string, object> pair)
	{
		if (pair.Value is IFeatureCollection featureCollection)
		{
			var badRequestFeature = featureCollection.Get<IBadRequestExceptionFeature>();

			if (badRequestFeature is not null)
			{
				_callback(badRequestFeature);
			}
		}
	}
	public void OnError(Exception error) { }
	public void OnCompleted() { }
	public virtual void Dispose() => _subscription.Dispose();
};