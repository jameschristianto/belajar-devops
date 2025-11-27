using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblNavigation
{
    public long Id { get; set; }

    public long? ParentNavigationId { get; set; }

    public int? Type { get; set; }

    public string? Name { get; set; }

    public string? Route { get; set; }

    public int? Visible { get; set; }

    public string? Icon { get; set; }

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
