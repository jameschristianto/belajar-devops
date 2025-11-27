using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblUserSession
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string? SessionId { get; set; }

    public DateTime? LastActive { get; set; }

    public string? Info { get; set; }

    public long? RoleId { get; set; }

    public bool? IsLogout { get; set; }

    public bool? SendEmailCounter { get; set; }

    public int? Counter { get; set; }
}
