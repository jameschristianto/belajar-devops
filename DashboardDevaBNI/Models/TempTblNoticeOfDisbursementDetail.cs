using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TempTblNoticeOfDisbursementDetail
{
    public int Id { get; set; }

    public int? NodId { get; set; }

    public int? CreditorRefId { get; set; }

    public string? CreditorRef { get; set; }

    public decimal? Amount { get; set; }

    public decimal? AmountIdr { get; set; }

    public DateTime? DisbursementDate { get; set; }

    public string? Apdplno { get; set; }

    public DateTime? Apdpldate { get; set; }

    public string? RealisasiNo { get; set; }

    public DateTime? RealisasiDate { get; set; }

    public string? ContractNo { get; set; }

    public DateTime? ContractDate { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int? CreatedById { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public int? UpdatedById { get; set; }

    public int? IsActive { get; set; }

    public int? IsDeleted { get; set; }
}
