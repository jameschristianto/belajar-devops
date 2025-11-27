namespace DashboardDevaBNI.ViewModels
{
    public class TempNoticeOfDisbursementDetails_ViewModel
    {
        public int? Id { get; set; }

        public int? NodId { get; set; }

        public int? CreditorRefId { get; set; }

        public string? CreditorRef { get; set; }

        public decimal? Amount { get; set; }

        public decimal? AmountIDR { get; set; }

        public DateTime? DisbursementDate { get; set; }

        public string? APDPLNo { get; set; }

        public DateTime? APDPLDate { get; set; }

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
}
