using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class DocumentType
{
    public long DocumentTypeId { get; set; }

    public string DocumentTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
