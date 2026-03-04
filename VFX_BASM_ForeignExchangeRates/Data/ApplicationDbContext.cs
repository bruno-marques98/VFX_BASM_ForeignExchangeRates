using Microsoft.EntityFrameworkCore;
using VFX_BASM_ForeignExchangeRates.Models;

namespace VFX_BASM_ForeignExchangeRates.Data
{
    /// <summary>
    /// Entity Framework Core database context for the application.
    /// </summary>
    /// <remarks>
    /// Exposes entity sets and configures model behavior (indexes, constraints) for the application's
    /// domain entities such as <see cref="ForeignExchangeRate"/>.
    /// </remarks>
    public class ApplicationDbContext :DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">The options used by a <see cref="DbContext"/>.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        /// <summary>
        /// Gets the set of <see cref="ForeignExchangeRate"/> entities.
        /// </summary>
        /// <remarks>
        /// Use this property to query and save instances of <see cref="ForeignExchangeRate"/>.
        /// The property delegates to <see cref="DbContext.Set{TEntity}"/> for EF Core behavior.
        /// </remarks>
        public DbSet<ForeignExchangeRate> ForeignExchangeRates => Set<ForeignExchangeRate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ForeignExchangeRate>()
                .HasIndex(f => new { f.BaseCurrency, f.QuoteCurrency })
                .IsUnique();
        }
    }
}
