using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblDownloadBigFile
{
    public int Id { get; set; }

    public string? Path { get; set; }

    public string? FileName { get; set; }

    public string? FileExt { get; set; }

    public int? StatusDownload { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int? CreatedById { get; set; }
}
