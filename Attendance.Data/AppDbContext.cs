using Microsoft.EntityFrameworkCore;

namespace Attendance.Data;

public class AppDbContext : DbContext
{
    public virtual DbSet<CalendarDay> CalendarDays { get; set; } = null!;
    public virtual DbSet<SpecialEvent> SpecialEvents { get; set; } = null!;
    public virtual DbSet<RecurringEvent> RecurringEvents { get; set; } = null!;
    public virtual DbSet<SportyImport> SportyImports { get; set; } = null!;
    public virtual DbSet<EntraPassImport> EntraPassImports { get; set; } = null!;
    //public virtual DbSet<DailySummary> DailySummaries { get; set; } = null!;
    //public virtual DbSet<YearlySummary> YearlySummaries { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CalendarDay>(e => {
            e.ToTable("CalendarDay");
            e.HasKey(e => e.Date);
        });

        builder.Entity<SportyImport>(e => {
            e.ToTable("SportyImport");
            e.HasKey(e => e.PersonId);
            e.Property(e => e.FirstName).HasMaxLength(100);
            e.Property(e => e.LastName).HasMaxLength(100);
            e.Property(e => e.CardNumber).HasMaxLength(50);
            e.Property(e => e.FALNumber).HasMaxLength(50);
            e.Property(e => e.PNZNumber).HasMaxLength(50);
        });

        builder.Entity<EntraPassImport>(e => {
            e.ToTable("EntraPassImport");
            e.HasKey(e => new { e.EventTime, e.CardNumber });
            e.Property(e => e.CardNumber).HasMaxLength(50);
            e.Property(e => e.CardUserName).HasMaxLength(100);
            e.Property(e => e.EventMessage).HasMaxLength(200);
            e.Property(e => e.Location).HasMaxLength(100);
            e.Property(e => e.CardInfo1).HasMaxLength(100);
            e.Property(e => e.CardInfo2).HasMaxLength(100);
        });

        builder.Entity<SpecialEvent>(e => {
            e.ToTable("SpecialEvent");
            e.HasKey(e => e.Id);
            e.Property(e => e.Description).HasMaxLength(200);
        });

        builder.Entity<RecurringEvent>(e => {
            e.ToTable("RecurringEvent");
            e.HasKey(e => e.Id);
            e.Property(e => e.Description).HasMaxLength(200);
        });

        //builder.Entity<DailySummary>(e => {
        //    e.ToTable("DailySummary");
        //    e.HasKey(e => new { e.PersonId, e.Date });
        //    e.Property(e => e.RegistrationRange).HasMaxLength(50);
        //});
    }
}
