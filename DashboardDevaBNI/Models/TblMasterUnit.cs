using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblMasterUnit
{
    public long Id { get; set; }

    public long? ParentId { get; set; }

    public int Type { get; set; }

    public string? KodeWilayah { get; set; }

    public string? Code { get; set; }

    public string? FullCode { get; set; }

    public string? ShortName { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Telepon { get; set; }

    public string? StatusOutlet { get; set; }

    public string? Latitude { get; set; }

    public string? Longitude { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedTime { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public int? CreatedById { get; set; }

    public int? UpdatedById { get; set; }

    public DateTime? DeletedTime { get; set; }

    public int? DeletedById { get; set; }

    public bool? IsDelete { get; set; }

    public int? ParentLevel { get; set; }
}
