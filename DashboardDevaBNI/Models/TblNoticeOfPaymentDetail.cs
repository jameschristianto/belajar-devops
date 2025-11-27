using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblNoticeOfPaymentDetail
{
    public long? NopId { get; set; }

    public string? CreditorRef { get; set; }

    public decimal? Outstanding { get; set; }

    public decimal? Principal { get; set; }

    public decimal? Interest { get; set; }

    public decimal? Fee { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int? CreatedById { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public int? UpdatedById { get; set; }

    public int? IsActive { get; set; }

    public int? IsDeleted { get; set; }

    public long Id { get; set; }

    public string? IdNopDetailFromApi { get; set; }
}
