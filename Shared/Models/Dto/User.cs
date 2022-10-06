namespace Concerto.Shared.Models.Dto;

public record User
{
    public long UserId { get; init; }
    public string Username { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }

    public string FullName
    {
        get
        {
            return $"{FirstName} {LastName}";
        }
    }
}