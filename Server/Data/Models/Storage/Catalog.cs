using System.ComponentModel.DataAnnotations;

namespace Concerto.Server.Data.Models
{
    public class Catalog
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public long OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public virtual ICollection<UploadedFile> Files { get; set; } = null!;
        public virtual ICollection<User> UsersSharedTo { get; set; } = null!;
        public virtual ICollection<Session> SharedInSessions { get; set; } = null!;
    }
}
