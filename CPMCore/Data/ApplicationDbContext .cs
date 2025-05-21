using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CPMCore.Models;
using System.Data.SqlClient;


namespace CPMCore.Data
{
        public class ApplicationDbContext : IdentityDbContext<ApplicationUser,IdentityRole,string>
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

        public String GetConnectionString()
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            IConfiguration configuration = configurationBuilder.AddUserSecrets<Program>().Build();
            string connectionString = configuration.GetSection("CPMRUNNING")["ConnectionString"].ToString();
            string DbPassword = configuration.GetSection("CPMRUNNING")["DbPassword"];
            string DbUser = configuration.GetSection("CPMRUNNING")["DbUser"];

            var conStrBuilder = new SqlConnectionStringBuilder(
                    connectionString);
            conStrBuilder.Password = DbPassword;
            conStrBuilder.UserID = DbUser;
            conStrBuilder.TrustServerCertificate = true;
            var connection = conStrBuilder.ConnectionString;


            return connection;
        }
    }
      
    }