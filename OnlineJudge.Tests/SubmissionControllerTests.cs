using Microsoft.EntityFrameworkCore;
using Xunit;
using OnlineJudge.Models;
using OnlineJudgeAPI.Controllers;
using Moq;
using Microsoft.AspNetCore.Mvc;
using OnlineJudgeAPI.Services;
using OnlineJudgeAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using OnlineJudgeAPI.DTOs;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using OnlineJudgeAPI.Models;
using System.Security.Claims;

namespace OnlineJudge.Tests.Controllers
{
    public class SubmissionsControllerTests
    {
        private SubmissionsController CreateController(ApplicationDbContext dbContext)
        {
            var codeExecutor = new Mock<CodeExecutor>();
            var submissionService = new Mock<ISubmissionService>();
            var queue = new Mock<SubmissionQueue>();
            var cache = new Mock<ICacheService>();
            var notify = new Mock<INotificationService>();
            var contestHub = new Mock<IHubContext<ContestHub>>();
            var leaderboardHub = new Mock<IHubContext<LeaderboardHub>>();
            var leaderboardService = new Mock<ILeaderboardService>();

            return new SubmissionsController(
                dbContext,
                codeExecutor.Object,
                submissionService.Object,
                queue.Object,
                cache.Object,
                notify.Object,
                contestHub.Object,
                leaderboardHub.Object,
                leaderboardService.Object
            );
        }

        [Fact]
        public async Task Submit_ReturnsBadRequest_WhenRequestIsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDb_Null")
                            .Options;
            var dbContext = new ApplicationDbContext(options);
            var controller = CreateController(dbContext);

            var result = await controller.SubmitCode(null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid request", badRequest.Value);
        }

        [Fact]
        public async Task Submit_ReturnsBadRequest_WhenCodeIsEmpty()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDb_EmptyCode")
                            .Options;
            var dbContext = new ApplicationDbContext(options);
            
            var controller = CreateController(dbContext);

            var request = new OnlineJudgeAPI.Controllers.SubmissionRequest
            {
                Code = "",
                Language = "cpp",
                ProblemId = 1,
                userId = 1
            };

            var result = await controller.SubmitCode(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid request", badRequest.Value);
        }

        [Fact]
        public async Task Submit_ReturnsOk_WhenValidRequest()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDb_Valid")
                            .Options;
            var dbContext = new ApplicationDbContext(options);

            var submissionService = new Mock<ISubmissionService>();
            var codeExecutor = new Mock<CodeExecutor>();
            codeExecutor
                .Setup(x => x.RunCodeAsync(It.IsAny<string>(),           // executablePath
        It.IsAny<List<OnlineJudgeAPI.Models.TestCase>>(),   // testCases
        It.IsAny<string>(),           // language
        It.IsAny<string>(),           // connectionId
        It.IsAny<int>()))
                .ReturnsAsync(new ExecutionResult
                {
                    StandardOutput = "Hello, World!",
                    StandardError = "",
                    TotalExecutionTimeMs = 10,
                    TotalMemoryUsageBytes = 1024
                });

            var queue = new Mock<SubmissionQueue>();
            var cache = new Mock<ICacheService>();
            var notify = new Mock<INotificationService>();
            var contestHub = new Mock<IHubContext<ContestHub>>();
            var leaderboardHub = new Mock<IHubContext<LeaderboardHub>>();
            var leaderboardService = new Mock<ILeaderboardService>();

            var controller = new SubmissionsController(
                dbContext,
                codeExecutor.Object,
                submissionService.Object,
                queue.Object,
                cache.Object,
                notify.Object,
                contestHub.Object,
                leaderboardHub.Object,
                leaderboardService.Object
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim("userId", "3")
        }, "mock"))
                }
            };
            var request = new OnlineJudgeAPI.Controllers.SubmissionRequest
            {
                ProblemId = 150,
                Code = "print('Hello, World!')",
                Language = "python",
            
                userId = 3,
                ConnectionId = "1",
                contestId = 1
            };
            dbContext.Problems.Add(new Problem
            {
                Id = 150,
                Title = "Test Problem",
                Description = "Test description", // 👈 thêm dòng này
                maxScore = 100,
                TestCases = new List<OnlineJudgeAPI.Models.TestCase>
    {
        new OnlineJudgeAPI.Models.TestCase
        {
            Id = 1,
            Input = "test",
            ExpectedOutput = "test"
        }
    }
            });
            await dbContext.SaveChangesAsync();

            var result = await controller.SubmitCode(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            // dynamic output = okResult.Value;
            var output = Assert.IsType<SubmissionResult22>(okResult.Value);

            Assert.Equal("Hello, World!", output.Output);
            Assert.Equal("", output.Error);
            Assert.Equal(10, output.ExecutionTime);
            Assert.Equal(1024, output.MemoryUsed);

        }

    }
}

public class SubmissionResult22
{
    public string Output { get; set; }
    public string Error { get; set; }
    public int ExecutionTime { get; set; }
    public int MemoryUsed { get; set; }
}