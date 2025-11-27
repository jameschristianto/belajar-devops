using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblLogErrorPrint
{
    public int Id { get; set; }

    public int? IdFile { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? CreatedTime { get; set; }
}
