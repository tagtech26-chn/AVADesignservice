using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long? InvoiceId { get; set; }

    public long ProjectId { get; set; }

    public long CustomerId { get; set; }

    public string? PaymentReference { get; set; }

    public DateOnly PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? TransactionReference { get; set; }

    public string? Notes { get; set; }

    public long? ReceivedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User? ReceivedByUser { get; set; }
}
