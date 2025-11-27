namespace DashboardDevaBNI.ViewModels
{
    public class MasterSystemParameter_ViewModel
    {
        public Int64 Number { get; set; }

        public long Id { get; set; }

        public string? Key { get; set; }

        public string? Value { get; set; }

        public string? Description { get; set; }

        public int? OrderBy { get; set; }

        public DateTime? CreatedTime { get; set; }

        public DateTime? UpdatedTime { get; set; }

        public DateTime? DeletedTime { get; set; }

        public long? CreatedById { get; set; }

        public long? UpdatedById { get; set; }

        public long? DeletedById { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDeleted { get; set; }
    }
}
