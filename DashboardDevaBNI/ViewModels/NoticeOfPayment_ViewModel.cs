using DashboardDevaBNI.Models;

namespace DashboardDevaBNI.ViewModels
{
    public class NoticeOfPayment_ViewModel
    {
        public Int64 Number { get; set; }
        public long Id { get; set; }
        public string? IdNopDetailFromApi { get; set; }
        public long? IdDetailFromGet { get; set; }
        public long IdDetailNop { get; set; }
        public long? NopId { get; set; }
        public string? NopNo { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? InterestDays { get; set; }
        public int? RekId { get; set; }
        public string? Cur { get; set; }
        public string? CreditorRef { get; set; }
        public decimal? Outstanding { get; set; }
        public decimal? Principal { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Fee { get; set; }
        public string? Status { get; set; }
        public DateTime? LastSentDate { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountName { get; set; }
        public string? RekNameAcc { get; set; }
        public string? Acc { get; set; }
        public DateTime? CreatedTime { get; set; }
        public long? CreatedById { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public long? UpdatedById { get; set; }
        public int? IsActive { get; set; }
        public int? IsDeleted { get; set; }
        public IFormFile File { get; set; }
        public string? RandomString { get; set; }

        public virtual TblFileUploadNop? FileUploadNop { get; set; }
        public virtual ICollection<TblNoticeOfPaymentDetail> TblNoticeOfPaymentDetails { get; set; } = new List<TblNoticeOfPaymentDetail>();
    }

    public partial class TblNoticeOfPaymentVM
    {
        public long Id { get; set; }
        public long? NopId { get; set; }

        public string? NopNo { get; set; }

        public DateTime? DueDate { get; set; }

        public string? InterestRate { get; set; }

        public string? InterestDays { get; set; }

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

    public partial class TblNoticeOfPaymentDetailVM
    {
        public long? NopId { get; set; }

        public string? CreditorRef { get; set; }

        public string? Outstanding { get; set; }

        public string? Principal { get; set; }

        public string? Interest { get; set; }

        public string? Fee { get; set; }

        public DateTime? CreatedTime { get; set; }

        public int? CreatedById { get; set; }

        public DateTime? UpdatedTime { get; set; }

        public int? UpdatedById { get; set; }

        public int? IsActive { get; set; }

        public int? IsDeleted { get; set; }

        public long Id { get; set; }
    }
    public class NoticeOfPaymentFile_ViewModel
    {
        public Int64? Number { get; set; }
        public int? Id { get; set; }
        public string? FileName { get; set; }
        public int? FileSize { get; set; }
        public string? FilePath { get; set; }
        public string? FileExt { get; set; }
        public int? IsActive { get; set; }
        public int? IsDeleted { get; set; }
        public string? RandomString { get; set; }
    }

    public class NoticeOfPaymentFileUpload_ViewModel
    {
        public IFormFile File { get; set; }
        public string? uniq { get; set; }
    }

    public class NoticeOfPaymentToAPI_ViewModels
    {
        public string NopNo { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? InterestDays { get; set; }
        public string Cur { get; set; }
        public int? RekId { get; set; }
        public List<TblNoticeOfPaymentDetail> NopDetail { get; set; }
    }


    public class CreditorRefDetail_ViewModels
    {
        public DateTime? DueDate { get; set; }
        public string? CreditorRef { get; set; }
        public decimal? Outstanding { get; set; }
        public decimal? Principal { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Fee { get; set; }
    }

    public class NoticeOfPaymentJOB_ViewModel
    {
        public string Id { get; set; }
        public string Status { get; set; }
    }
}
