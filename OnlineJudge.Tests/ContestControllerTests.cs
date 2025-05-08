using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OnlineJudgeAPI.Controllers;
using OnlineJudgeAPI.Models;
using OnlineJudgeAPI.DTOs;
using OnlineJudgeAPI.Services;

namespace OnlineJudgeAPI.Tests.Controllers
{
    public class ContestControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly ContestController _controller;

        public ContestControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _controller = new ContestController(_context);
        }

        [Fact]
        public async Task CreateContest_ShouldAddContest()
        {
            var problem = new Problem
            {
                Id = 1,
                Title = "Sample",
                Description = "Sample problem",
                TestCases = new List<TestCase>
                {
                    new TestCase
                    {
                        Input = "1 2",
                        ExpectedOutput = "3"
                    }
                }
            };
            _context.Problems.Add(problem);
            await _context.SaveChangesAsync();

            var request = new CreateContestRequest
            {
                Title = "Test Contest",
                Description = "Test contest description",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(2),
                Problems = new List<ProblemScoreDto>
                {
                    new ProblemScoreDto { ProblemId = 1, Score = 100 }
                }
            };

            var result = await _controller.CreateContest(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var contest = Assert.IsType<Contest>(okResult.Value);
            Assert.Equal("Test Contest", contest.Name);
            Assert.Single(contest.ContestProblems);
        }

        [Fact]
        public void GetContests_ShouldReturnAllContests()
        {
            _context.Contests.AddRange(
                new Contest
                {
                    Name = "Contest 1",
                    Description = "Desc 1",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddHours(2),
                    ContestProblems = new List<ContestProblem>()
                },
                new Contest
                {
                    Name = "Contest 2",
                    Description = "Desc 2",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddHours(2),
                    ContestProblems = new List<ContestProblem>()
                }
            );
            _context.SaveChanges();

            var result = _controller.GetContests();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var contests = Assert.IsType<List<Contest>>(okResult.Value);

            Assert.Equal(2, contests.Count);
        }

        [Fact]
        public void GetContest_ShouldReturnContest_WhenExists()
        {
            var contest = new Contest
            {
                Id = 1,
                Name = "Contest 1",
                Description = "Test contest description",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(2),
                ContestProblems = new List<ContestProblem>()
            };
            _context.Contests.Add(contest);
            _context.SaveChanges();

            var result = _controller.GetContest(1);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedContest = Assert.IsType<Contest>(okResult.Value);

            Assert.Equal("Contest 1", returnedContest.Name);
        }

        [Fact]
        public async Task DeleteContest_ShouldRemoveContest_WhenExists()
        {
            var contest = new Contest
            {
                Id = 2,
                Name = "Contest to delete",
                Description = "Delete me",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(2),
                ContestProblems = new List<ContestProblem>()
            };
            _context.Contests.Add(contest);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteContest(2);
            var okResult = Assert.IsType<NoContentResult>(result);

            Assert.Null(_context.Contests.Find(2));
        }
    }
}
