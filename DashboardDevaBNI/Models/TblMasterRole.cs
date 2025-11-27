using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblMasterRole
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? Kode { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedTime { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public DateTime? DeletedTime { get; set; }

    public long? CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? DeletedById { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }
}
