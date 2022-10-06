using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concerto.Shared.Models.Dto;
public record Session
{    
    public long SessionId { get; init; }
    public string Name { get; init; }
    public DateTime ScheduledDateTime { get; set; }
    public Conversation Conversation { get; set; }
    public IEnumerable<Dto.UploadedFile>? Files { get; set; }
}

public record CreateSessionRequest
{
    public string Name { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public long RoomId { get; set; }
}