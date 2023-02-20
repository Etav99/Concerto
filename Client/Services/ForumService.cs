namespace Concerto.Client.Services;

public interface IForumService : IForumClient { }

public class ForumService : ForumClient, IForumService
{
    public ForumService(HttpClient httpClient) : base(httpClient) { }
}



