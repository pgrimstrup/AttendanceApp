using Microsoft.EntityFrameworkCore;

namespace Attendance.Data;

public class AppDbContext : DbContext
{
    public virtual DbSet<CalendarCategory> CalendarCategories { get; set; } = null!;
    public virtual DbSet<CalendarDay> CalendarDays { get; set; } = null!;
    public virtual DbSet<AwayEvent> AwayEvents { get; set; } = null!;
    public virtual DbSet<OverrideEvent> OverrideEvents { get; set; } = null!;
    public virtual DbSet<SportyImport> SportyImports { get; set; } = null!;
    public virtual DbSet<EntraPassImport> EntraPassImports { get; set; } = null!;
    public virtual DbSet<ImportedFile> ImportedFiles { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {

    }

    /// <summary>
    /// A generic method to execute raw SQL queries with parameters and map the results to a list of objects of type T.
    /// Not optimized to be called multiple times in a loop, as it uses reflection to map properties.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sql"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<T> QueryAsync<T>(string sql, IDictionary<string, object?> parameters)
        where T : class, new()
    {
        using var command = Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.CommandType = System.Data.CommandType.Text;
        ArgumentNullException.ThrowIfNull(command.Connection);

        // Add parameters to the command
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = param.Key;
                dbParameter.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }
        }

        // Open the connection if it's not already open
        if (command.Connection.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        // Execute the query and map the results
        using var reader = await command.ExecuteReaderAsync();
        var properties = typeof(T).GetProperties();

        var columnNames = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++) 
            columnNames.Add(reader.GetName(i));

        while (await reader.ReadAsync())
        {
            var instance = new T();
            foreach (var property in properties)
            {
                if(!columnNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                int ordinal = reader.GetOrdinal(property.Name);
                if (!reader.IsDBNull(ordinal))
                {
                    property.SetValue(instance, reader[ordinal]);
                }
            }

            yield return instance;
        }

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CalendarCategory>(e => {
            e.ToTable("CalendarCategory");
            e.HasKey(e => e.Name);
            e.Property(e => e.Name).HasMaxLength(200);
            e.Property(e => e.Color).HasMaxLength(50);
        });

        builder.Entity<CalendarDay>(e => {
            e.ToTable("CalendarDay");
            e.HasKey(e => e.Date);
        });

        builder.Entity<SportyImport>(e => {
            e.ToTable("SportyImport");
            e.HasKey(e => e.PersonId);
            e.Property(e => e.PersonId).ValueGeneratedNever();
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

        builder.Entity<AwayEvent>(e => {
            e.ToTable("AwayEvent");
            e.HasKey(e => new { e.PersonId, e.StartDate });
            e.Property(e => e.Location).HasMaxLength(200);
            e.Property(e => e.EventName).HasMaxLength(200);
            e.Property(e => e.Notes);
        });

        builder.Entity<OverrideEvent>(e => {
            e.ToTable("OverrideEvent");
            e.HasKey(e => new { e.PersonId, e.EventDate });
        }); 

        builder.Entity<ImportedFile>(e => {
            e.ToTable("ImportedFile");
            e.HasKey(e => e.Id);
            e.Property(e => e.Id).ValueGeneratedOnAdd();
            e.Property(e => e.FileType).HasMaxLength(50);
        });
    }
}
