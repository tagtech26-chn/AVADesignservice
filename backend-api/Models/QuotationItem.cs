using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class QuotationItem
{
    public long QuotationItemId { get; set; }

    public long QuotationId { get; set; }

    public long? ProjectServiceId { get; set; }

    public string ItemType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxPercent { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    public int DisplayOrder { get; set; }

    public string? Notes { get; set; }

    public virtual ProjectService? ProjectService { get; set; }

    public virtual Quotation Quotation { get; set; } = null!;
}
