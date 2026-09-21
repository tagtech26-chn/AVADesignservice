using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Document
{
    public long DocumentId { get; set; }

    public long? ProjectId { get; set; }

    public long? DesignId { get; set; }

    public long DocumentTypeId { get; set; }

    public string FileName { get; set; } = null!;

    public string? OriginalFileName { get; set; }

    public string FilePath { get; set; } = null!;

    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public int VersionNumber { get; set; }

    public bool IsCustomerVisible { get; set; }

    public long? UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Design? Design { get; set; }

    public virtual DocumentType DocumentType { get; set; } = null!;

    public virtual Project? Project { get; set; }

    public virtual User? UploadedByUser { get; set; }
}
