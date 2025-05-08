using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using OnlineJudgeAPI.Controllers;
using OnlineJudgeAPI.DTOs;
using OnlineJudgeAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineJudgeAPI.Services;

namespace OnlineJudgeAPI.Tests
{
    public class AuthControllerTests
    {
        private AuthController _controller;
        private ApplicationDbContext _context;
        private IConfiguration _configuration;

        public AuthControllerTests()
        {
            // Thiết lập cấu hình JWT fake
            var configData = new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "8b7250d347e34f1b8c2c4e2f19f903f1SuperSecureKey1234"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Thiết lập in-memory database
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            // Seed role
            _context.Roles.Add(new Role { Id = 1, RoleName = "User" });
            _context.SaveChanges();

            _controller = new AuthController(_context, _configuration);
        }

        [Fact]
        public async Task Register_NewUser_ReturnsOk()
        {
            var dto = new RegisterDto
            {
                Username = "testuser",
                Password = "password",
                Email = "test@example.com",
                RoleIds = new List<int> { 1 }
            };

            var result = await _controller.Register(dto);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Dang ki thanh cong", ((dynamic)okResult.Value).message);
        }

        [Fact]
        public async Task Register_ExistingUser_ReturnsBadRequest()
        {
            var dto = new RegisterDto
            {
                Username = "testuser",
                Password = "password",
                Email = "test@example.com",
                RoleIds = new List<int> { 1 }
            };

            // Đăng ký lần 1
            await _controller.Register(dto);

            // Đăng ký lần 2
            var result = await _controller.Register(dto);
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Tai khoan hoac mat khau da ton tai", badResult.Value);
        }

        [Fact]
        public async Task Login_ValidUser_ReturnsOkWithToken()
        {
            // Đăng ký trước
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Password = "password",
                Email = "test@example.com",
                RoleIds = new List<int> { 1 }
            };
            await _controller.Register(registerDto);

            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = "password"
            };

            var result = await _controller.Login(loginDto);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(((dynamic)okResult.Value).token);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Đăng ký trước
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Password = "correctpassword",
                Email = "test@example.com",
                RoleIds = new List<int> { 1 }
            };
            await _controller.Register(registerDto);

            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            var result = await _controller.Login(loginDto);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Tai khoan hoac mat khau khong dung", unauthorized.Value);
        }
    }
}
