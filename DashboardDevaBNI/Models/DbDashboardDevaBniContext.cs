using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;
using DashboardDevaBNI.Component;
using Microsoft.EntityFrameworkCore;

namespace DashboardDevaBNI.Models;

public partial class DbDashboardDevaBniContext : DbContext
{
    public DbDashboardDevaBniContext()
    {
    }

    public DbDashboardDevaBniContext(DbContextOptions<DbDashboardDevaBniContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblDownloadBigFile> TblDownloadBigFiles { get; set; }

    public virtual DbSet<TblFileUploadNod> TblFileUploadNods { get; set; }

    public virtual DbSet<TblFileUploadNodTemp> TblFileUploadNodTemps { get; set; }

    public virtual DbSet<TblFileUploadNop> TblFileUploadNops { get; set; }

    public virtual DbSet<TblFileUploadNopTemp> TblFileUploadNopTemps { get; set; }

    public virtual DbSet<TblGetOtp> TblGetOtps { get; set; }

    public virtual DbSet<TblLogActivity> TblLogActivities { get; set; }

    public virtual DbSet<TblLogErrorPrint> TblLogErrorPrints { get; set; }

    public virtual DbSet<TblMasterLookup> TblMasterLookups { get; set; }

    public virtual DbSet<TblMasterRole> TblMasterRoles { get; set; }

    public virtual DbSet<TblMasterSystemParameter> TblMasterSystemParameters { get; set; }

    public virtual DbSet<TblMasterUnit> TblMasterUnits { get; set; }

    public virtual DbSet<TblMasterUser> TblMasterUsers { get; set; }

    public virtual DbSet<TblMasterUserVerif> TblMasterUserVerifs { get; set; }

    public virtual DbSet<TblNavigation> TblNavigations { get; set; }

    public virtual DbSet<TblNavigationAssignment> TblNavigationAssignments { get; set; }

    public virtual DbSet<TblNoticeOfDisbursement> TblNoticeOfDisbursements { get; set; }

    public virtual DbSet<TblNoticeOfDisbursementDetail> TblNoticeOfDisbursementDetails { get; set; }

    public virtual DbSet<TblNoticeOfDisbursementDetailTemp> TblNoticeOfDisbursementDetailTemps { get; set; }

    public virtual DbSet<TblNoticeOfPayment> TblNoticeOfPayments { get; set; }

    public virtual DbSet<TblNoticeOfPaymentDetail> TblNoticeOfPaymentDetails { get; set; }

    public virtual DbSet<TblNoticeOfPaymentDetailTemp> TblNoticeOfPaymentDetailTemps { get; set; }

    public virtual DbSet<TblUserSession> TblUserSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        string? decodedString = GetConfig.AppSetting["ConnectionString:dbDeva"];
        if (string.IsNullOrWhiteSpace(decodedString))
        {
            throw new Exception("empty connection string");
        }

        try
        {
            byte[] connString = Convert.FromBase64String(decodedString!);
            optionsBuilder.UseSqlServer(Encoding.UTF8.GetString(connString));
        } catch
        {
            throw new Exception("invalid connection string");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblDownloadBigFile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_down__3214EC078FE38745");

            entity.ToTable("Tbl_download_big_file");

            entity.Property(e => e.FileExt)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FileName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Path)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblFileUploadNod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_File__3214EC072BB140F8");

            entity.ToTable("Tbl_File_Upload_Nod");

            entity.Property(e => e.FileExt)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FilePath)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.IdFileFromApi)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("IdFileFromAPI");
        });

        modelBuilder.Entity<TblFileUploadNodTemp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_File__3214EC070D7122AA");

            entity.ToTable("Tbl_File_Upload_Nod_Temp");

            entity.Property(e => e.FileExt)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FilePath)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.IdFileFromApi)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("IdFileFromAPI");
        });

        modelBuilder.Entity<TblFileUploadNop>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_File__3214EC07C7824456");

            entity.ToTable("Tbl_File_Upload_Nop");

            entity.Property(e => e.FileExt)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FilePath)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.IdFileFromApi)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("IdFileFromAPI");
        });

        modelBuilder.Entity<TblFileUploadNopTemp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_File__3214EC076CE99E4A");

            entity.ToTable("Tbl_File_Upload_Nop_Temp");

            entity.Property(e => e.FileExt)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FilePath)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.IdFileFromApi)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("IdFileFromAPI");
        });

        modelBuilder.Entity<TblGetOtp>(entity =>
        {
            entity.ToTable("Tbl_GetOtp");

            entity.Property(e => e.ExpiredTime).HasColumnType("datetime");
            entity.Property(e => e.KodeOtp).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<TblLogActivity>(entity =>
        {
            entity.ToTable("Tbl_LogActivity");

            entity.Property(e => e.ActionTime).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<TblLogErrorPrint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_LogE__3214EC07493145C8");

            entity.ToTable("Tbl_LogErrorPrint");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.ErrorMessage).HasColumnType("text");
        });

        modelBuilder.Entity<TblMasterLookup>(entity =>
        {
            entity.ToTable("Tbl_MasterLookup");

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DeletedTime).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblMasterRole>(entity =>
        {
            entity.ToTable("Tbl_MasterRole");

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DeletedTime).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(50);
            entity.Property(e => e.Kode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblMasterSystemParameter>(entity =>
        {
            entity.ToTable("Tbl_MasterSystemParameter");

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DeletedTime).HasColumnType("datetime");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Key)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
            entity.Property(e => e.Value).HasColumnType("text");
        });

        modelBuilder.Entity<TblMasterUnit>(entity =>
        {
            entity.ToTable("Tbl_MasterUnit");

            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedById).HasColumnName("CreatedBy_Id");
            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DeletedById).HasColumnName("DeletedBy_Id");
            entity.Property(e => e.DeletedTime).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullCode).HasMaxLength(50);
            entity.Property(e => e.KodeWilayah).HasMaxLength(50);
            entity.Property(e => e.Latitude).HasMaxLength(500);
            entity.Property(e => e.Longitude).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.ParentId).HasColumnName("Parent_Id");
            entity.Property(e => e.ParentLevel).HasColumnName("parent_level");
            entity.Property(e => e.ShortName).HasMaxLength(50);
            entity.Property(e => e.StatusOutlet).HasMaxLength(50);
            entity.Property(e => e.Telepon).HasColumnType("text");
            entity.Property(e => e.UpdatedById).HasColumnName("UpdatedBy_Id");
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblMasterUser>(entity =>
        {
            entity.ToTable("Tbl_MasterUser");

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DeletedTime).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Fullname)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsVerifEmail).HasMaxLength(50);
            entity.Property(e => e.IsVerifNoTelp).HasMaxLength(50);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.NoTelp)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblMasterUserVerif>(entity =>
        {
            entity.ToTable("Tbl_MasterUser_Verif");

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DeletedTime).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NoTelp)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblNavigation>(entity =>
        {
            entity.ToTable("Tbl_Navigation");

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DeletedTime).HasColumnType("datetime");
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Route).HasMaxLength(50);
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblNavigationAssignment>(entity =>
        {
            entity.ToTable("Tbl_NavigationAssignment");

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DeletedTime).HasColumnType("datetime");
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblNoticeOfDisbursement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_Noti__3214EC0752DD1BD7");

            entity.ToTable("Tbl_Notice_Of_Disbursement");

            entity.Property(e => e.Beneficiary)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Cur)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.FileUploadNodid).HasColumnName("FileUploadNODId");
            entity.Property(e => e.IdNodFromApi).HasMaxLength(100);
            entity.Property(e => e.NodDate).HasColumnName("NOdDate");
            entity.Property(e => e.NodNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblNoticeOfDisbursementDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_Noti__3214EC07B69BDB30");

            entity.ToTable("Tbl_Notice_Of_Disbursement_Detail");

            entity.Property(e => e.Amount).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.AmountIdr)
                .HasColumnType("decimal(22, 4)")
                .HasColumnName("AmountIDR");
            entity.Property(e => e.Apdpldate).HasColumnName("APDPLDate");
            entity.Property(e => e.Apdplno)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("APDPLNo");
            entity.Property(e => e.ContractNo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.CreditorRef)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IdNodDetailFromApi).HasMaxLength(100);
            entity.Property(e => e.RealisasiNo)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblNoticeOfDisbursementDetailTemp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_Noti__3214EC076107228B");

            entity.ToTable("Tbl_Notice_Of_Disbursement_DetailTemp");

            entity.Property(e => e.Amount).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.AmountIdr)
                .HasColumnType("decimal(22, 4)")
                .HasColumnName("AmountIDR");
            entity.Property(e => e.Apdpldate).HasColumnName("APDPLDate");
            entity.Property(e => e.Apdplno)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("APDPLNo");
            entity.Property(e => e.ContractNo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.CreditorRef)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RandomString)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.RealisasiNo)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblNoticeOfPayment>(entity =>
        {
            entity.ToTable("Tbl_NoticeOfPayment");

            entity.Property(e => e.AccountName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccountNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cur)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IdNopFromApi).HasMaxLength(100);
            entity.Property(e => e.InterestDays).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.InterestRate).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.NopNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RekNameAcc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblNoticeOfPaymentDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK");

            entity.ToTable("Tbl_NoticeOfPaymentDetail");

            entity.Property(e => e.CreditorRef)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Fee).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.IdNopDetailFromApi)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Interest).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.Outstanding).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.Principal).HasColumnType("decimal(22, 4)");
        });

        modelBuilder.Entity<TblNoticeOfPaymentDetailTemp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tbl_Noti__3214EC07CBA5475B");

            entity.ToTable("Tbl_Notice_Of_Payment_DetailTemp");

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.CreditorRef)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Fee).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.Interest).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.Outstanding).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.Principal).HasColumnType("decimal(22, 4)");
            entity.Property(e => e.RandomString)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblUserSession>(entity =>
        {
            entity.ToTable("Tbl_UserSession");

            entity.Property(e => e.Info).HasMaxLength(50);
            entity.Property(e => e.LastActive).HasColumnType("datetime");
            entity.Property(e => e.SessionId).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
