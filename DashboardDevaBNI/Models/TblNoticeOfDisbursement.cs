using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblNoticeOfDisbursement
{
    public int Id { get; set; }

    public int? FileUploadNodid { get; set; }

    public string? NodNo { get; set; }

    public DateTime? NodDate { get; set; }

    public DateTime? ValueDate { get; set; }

    public string? Cur { get; set; }

    public string? Beneficiary { get; set; }

    public DateTime? LastSentDate { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int? CreatedById { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public int? UpdatedById { get; set; }

    public int? IsActive { get; set; }

    public int? IsDeleted { get; set; }

    public string? Status { get; set; }

    public string? IdNodFromApi { get; set; }
}
