using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Library.API.Entities
{
    public class LibraryContextFactory : IDesignTimeDbContextFactory<LibraryContext>
    {
        private readonly IConfiguration _configuration;

        public LibraryContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LibraryContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LibraryContext>();
            var connectionString = _configuration["connectionStrings:libraryDBConnectionString"];
            optionsBuilder.UseSqlServer(connectionString);

            return new LibraryContext(optionsBuilder.Options);
        }
    }
}
