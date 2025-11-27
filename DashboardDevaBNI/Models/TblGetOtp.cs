using System;
using System.Collections.Generic;

namespace DashboardDevaBNI.Models;

public partial class TblGetOtp
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? KodeOtp { get; set; }

    public DateTime? ExpiredTime { get; set; }
}
