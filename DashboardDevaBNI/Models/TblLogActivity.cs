using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblLogActivity
{
    public long Id { get; set; }

    public string? Username { get; set; }

    public string? Url { get; set; }

    public DateTime? ActionTime { get; set; }

    public string? Browser { get; set; }

    public string? Ip { get; set; }

    public string? Os { get; set; }

    public string? ClientInfo { get; set; }

    public string? Keterangan { get; set; }

    public long? UserId { get; set; }
}
