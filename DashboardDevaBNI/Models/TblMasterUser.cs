using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblMasterUser
{
    public long Id { get; set; }

    public string? Fullname { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? NoTelp { get; set; }

    public int? TypeUser { get; set; }

    public long? RoleId { get; set; }

    public long? UnitId { get; set; }

    public DateTime? CreatedTime { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public DateTime? DeletedTime { get; set; }

    public long? CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? DeletedById { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? LastLogin { get; set; }

    public string? IsVerifEmail { get; set; }

    public string? IsVerifNoTelp { get; set; }
}
