using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblMasterLookup
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public int? Value { get; set; }

    public string? Description { get; set; }

    public string? Type { get; set; }

    public DateTime? CreatedTime { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public DateTime? DeletedTime { get; set; }

    public long? CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? DeletedById { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }
}
