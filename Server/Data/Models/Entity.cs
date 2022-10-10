using System.ComponentModel.DataAnnotations;

namespace Concerto.Server.Data.Models;

public interface IEntity
{
    [Key]
    public long Id { get; set; }
}

public abstract class Entity : IEntity
{
    [Key]
    public long Id { get; set; }
}
