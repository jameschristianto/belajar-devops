using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblFileUploadNod
{
    public int Id { get; set; }

    public string? IdFileFromApi { get; set; }

    public int? IdNod { get; set; }

    public string? FileName { get; set; }

    public int? FileSize { get; set; }

    public string? FilePath { get; set; }

    public string? FileExt { get; set; }

    public DateTime? UploadTime { get; set; }

    public int? UploadById { get; set; }

    public int? IsActive { get; set; }

    public int? IsDeleted { get; set; }
}
