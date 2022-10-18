namespace Concerto.Client.Services;

public interface IContactService
{
	public List<Dto.User> Contacts { get; }
	public Task LoadContactsAsync();
	public Task<String> GetContactNameAsync(long contactId);
	public void InvalidateCache();
}

public class ContactService : IContactService
{
	private readonly IUserClient _userClient;

	private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

	public ContactService(IUserClient userClient)
	{
		_userClient = userClient;
	}


	bool cacheInvalidated = true;
	private List<Dto.User> contactsCache = new List<Dto.User>();
	public List<Dto.User> Contacts
	{
		get
		{
			return contactsCache;
		}
	}

	public async Task LoadContactsAsync()
	{
		await semaphore.WaitAsync();
		if (cacheInvalidated)
		{
			var contactsResponse = await _userClient.GetCurrentUserContactsAsync();
			contactsCache = contactsResponse?.ToList() ?? new List<Dto.User>();
			cacheInvalidated = false;
		}
		semaphore.Release();
	}

	public void InvalidateCache()
	{
		cacheInvalidated = true;
	}

	public async Task<string> GetContactNameAsync(long contactId)
	{
		await LoadContactsAsync();
		var contact = contactsCache?.FirstOrDefault(c => c.Id == contactId);
		if (contact != null)
		{
			return $"{contact.FirstName} {contact.LastName}";
		}
		return "Unknown user";
	}
}
