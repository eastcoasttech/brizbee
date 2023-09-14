
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Brizbee.Api.Tests;

[TestClass]
public class ChecksControllerTest
{
    public IConfiguration Configuration { get; set; }

    public SqlContext Context { get; set; }

    private readonly Helper _helper = new();

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ChecksControllerTest()
    {
        // Setup configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json");
        Configuration = configurationBuilder.Build();

        // Setup database context
        var options = new DbContextOptionsBuilder<SqlContext>()
            .UseSqlServer(Configuration["ConnectionStrings:SqlContext"])
            .Options;
        Context = new SqlContext(options);
    }

    [TestInitialize]
    public void PrepareForTest()
    {
        _helper.Prepare();
    }

    [TestCleanup]
    public void CleanupAfterTest()
    {
        _helper.Cleanup();
    }

    [TestMethod]
    public async System.Threading.Tasks.Task CreateCheck_Valid_Succeeds()
    {
        // ----------------------------------------------------------------
        // Arrange
        // ----------------------------------------------------------------

        var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddJsonFile("appsettings.json");
                });
            });

        var client = application.CreateClient();

        // User will be authenticated
        var currentUser = Context.Users!
            .First(u => u.EmailAddress == "test.user.a@brizbee.com");

        var token = GenerateJsonWebToken(currentUser.Id, currentUser.EmailAddress!);

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


        // ----------------------------------------------------------------
        // Act
        // ----------------------------------------------------------------

        var phoneExpenseAccount = await Context.Accounts!.FirstAsync(a => a.Name == "Phone Expense");
        var bankAccount = await Context.Accounts!.FirstAsync(a => a.Name == "Capital One Spark Checking");
        var vendor = await Context.Vendors!.FirstAsync(v => v.Name == "Verizon");

        var content = new
        {
            EnteredOn = new DateTime(2022, 8, 1),
            Number = "DEBIT",
            VendorId = vendor.Id,
            BankAccountId = bankAccount.Id,
            Memo = "Monthly Bill 08/2022",
            CheckExpenseLines = new[]
            {
                    new
                    {
                        AccountId = phoneExpenseAccount.Id,
                        Amount = 150.50M,
                    }
                }
        };
        var json = JsonSerializer.Serialize(content, _options);
        var buffer = Encoding.UTF8.GetBytes(json);
        var byteContent = new ByteArrayContent(buffer);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await client.PostAsync("api/Accounting/Checks", byteContent);


        // ----------------------------------------------------------------
        // Assert
        // ----------------------------------------------------------------

        Assert.IsTrue(response.IsSuccessStatusCode);

        const string balanceOfAccountSql = "SELECT [dbo].[udf_AccountBalance] (@MinDate, @MaxDate, @AccountId);";

        var balanceOfPhoneExpenseAccount = await Context.Database.GetDbConnection().QueryFirstOrDefaultAsync<decimal>(
            balanceOfAccountSql,
            param: new
            {
                MinDate = new DateTime(2022, 8, 1),
                MaxDate = new DateTime(2022, 8, 1),
                AccountId = phoneExpenseAccount!.Id
            });

        Assert.AreEqual(150.50M, balanceOfPhoneExpenseAccount);

        var balanceOfBankAccount = await Context.Database.GetDbConnection().QueryFirstOrDefaultAsync<decimal>(
            balanceOfAccountSql,
            param: new
            {
                MinDate = new DateTime(2022, 8, 1),
                MaxDate = new DateTime(2022, 8, 1),
                AccountId = bankAccount!.Id
            });

        Assert.AreEqual(-150.50M, balanceOfBankAccount);
    }

    private string GenerateJsonWebToken(int userId, string emailAddress)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, emailAddress),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        var token = new JwtSecurityToken(Configuration["Jwt:Issuer"],
            Configuration["Jwt:Issuer"],
            claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
