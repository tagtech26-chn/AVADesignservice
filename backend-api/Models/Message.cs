using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Message
{
    public long MessageId { get; set; }

    public long? ProjectId { get; set; }

    public long? UserId { get; set; }

    public long? CustomerId { get; set; }

    public string SenderType { get; set; } = null!;

    public string MessageText { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Project? Project { get; set; }

    public virtual User? User { get; set; }
}
