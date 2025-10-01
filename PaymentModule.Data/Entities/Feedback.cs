using System;
using System.Collections.Generic;

namespace PaymentModule.Data.Entities;

public partial class Feedback
{
    public int Id { get; set; }

    public int? SellerId { get; set; }

    public decimal? AverageRating { get; set; }

    public int? TotalReviews { get; set; }

    public decimal? PositiveRate { get; set; }

    public virtual User? Seller { get; set; }
}
