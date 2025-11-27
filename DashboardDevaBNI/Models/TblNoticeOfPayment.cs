using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblNoticeOfPayment
{
    public long Id { get; set; }

    public string? NopNo { get; set; }

    public DateTime? DueDate { get; set; }

    public decimal? InterestRate { get; set; }

    public decimal? InterestDays { get; set; }

    public string? Cur { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int? CreatedById { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public int? UpdatedById { get; set; }

    public int? IsActive { get; set; }

    public int? IsDeleted { get; set; }

    public string? Status { get; set; }

    public string? AccountNo { get; set; }

    public string? AccountName { get; set; }

    public string? IdNopFromApi { get; set; }

    public DateTime? LastSentDate { get; set; }

    public int? RekId { get; set; }

    public string? RekNameAcc { get; set; }
}
