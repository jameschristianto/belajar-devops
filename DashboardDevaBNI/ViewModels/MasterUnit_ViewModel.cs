namespace DashboardDevaBNI.ViewModels
{
    public class MasterUnit_ViewModel
    {
        public Int64? Number { get; set; }
        public long? Id { get; set; }
        public string? Name { get; set; }
        public string? StatusOutlet { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateMasterUnit_ViewModel
    {
        public string? Name { get; set; }
        public string? ShortName { get; set; }
        public string? Alamat { get; set; }
        public int? ParentId { get; set; }
        public string? StatusOutlet { get; set; }
        public string? NoTelp { get; set; }
        public string? Kode { get; set; }
        public string? KodeWilayah { get; set; }
        public string? KodeCabang { get; set; }
        public string? FullKode { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateMasterUnit_ViewModel : CreateMasterUnit_ViewModel
    {
        public int? Id { get; set;  }
    }
}
