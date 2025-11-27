namespace DashboardDevaBNI.ViewModels
{
    public class BulkMasterUser_ViewModels
    {
        public IFormFile DataFileUser { get; set; }
        public string? RoleId{ get; set; }
        public string? RandomString{ get; set; }
        public string? TypeUser { get; set; }
        public string? TypeDinkes { get; set; }
        public string? ProvinceKode { get; set; }
        public string? DistrictKode { get; set; }
        public string? SubDistrictKode { get; set; }
        public string? PuskesmasKode { get; set; }


    }
    public class MasterUser_ViewModel
    {
        public Int64 Number { get; set; }
        public long Id { get; set; }

        public string? Fullname { get; set; }

        public string? Username { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }

        public string? NoTelp { get; set; }
        public string? Role { get; set; }
        public string? Provinsi { get; set; }
        public string? Kota { get; set; }
        public string? Puskesmas { get; set; }

        public int? TypeUser { get; set; }

        public string? TypeUserName { get; set; }

        public int? RoleId { get; set; }

        public string? RoleName { get; set; }

        public string? UnitId { get; set; }

        public DateTime? CreatedTime { get; set; }

        public DateTime? UpdatedTime { get; set; }

        public DateTime? DeletedTime { get; set; }

        public long? CreatedById { get; set; }

        public long? UpdatedById { get; set; }

        public long? DeletedById { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDeleted { get; set; }

        public DateTime? LastLogin { get; set; }

        public int? IsActivated { get; set; }
    }

}
