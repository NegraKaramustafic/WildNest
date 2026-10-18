using eCommerce.Services.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Tests
{
    internal sealed class SqliteDb : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SqliteDb()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseSqlite(_connection)
                .Options;
            Context = new ECommerceDbContext(options);
            Context.Database.EnsureCreated();
        }

        public ECommerceDbContext Context { get; }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
