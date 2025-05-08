using Xunit;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Controllers;
using OnlineJudgeAPI.Models;
using OnlineJudgeAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using OnlineJudgeAPI.Services;

public class ProblemsControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly ProblemsController _controller;

    public ProblemsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB per test
            .EnableSensitiveDataLogging()
            .Options;

        _context = new ApplicationDbContext(options);
        _controller = new ProblemsController(_context);
    }

    [Fact]
    public async Task CreateProblem_ShouldAddProblem()
    {
        var dto = new ProblemCreateDTO
        {
            Title = "Test Problem",
            Description = "This is a test",
            InputFormat = "int",
            Constraints = "1 <= n <= 100",
            OutputFormat = "int",
            InputSample = "1",
            OutputSample = "2",
            DoKho = "Easy",
            DangBai = "Math",
            TestCases = new List<TestCase>
            {
                new TestCase
                {
                    Input = "1",
                    ExpectedOutput = "2"
                }
            }
        };

        var result = await _controller.CreateProblem(dto);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var problem = Assert.IsType<Problem>(okResult.Value);
        Assert.Equal("Test Problem", problem.Title);
    }

    [Fact]
    public async Task GetProblems_ShouldReturnList()
    {
        _context.Problems.Add(new Problem
        {
            Title = "P1",
            Description = "desc1",
            InputFormat = "int",
            Constraints = "n > 0",
            OutputFormat = "int",
            InputSample = "1",
            OutputSample = "2",
            DoKho = "Easy",
            DangBai = "Loop",
            TestCases = new List<TestCase> {
                new TestCase { Input = "1", ExpectedOutput = "2" }
            }
        });
        _context.Problems.Add(new Problem
        {
            Title = "P2",
            Description = "desc2",
            InputFormat = "int",
            Constraints = "n > 0",
            OutputFormat = "int",
            InputSample = "3",
            OutputSample = "6",
            DoKho = "Medium",
            DangBai = "Math",
            TestCases = new List<TestCase> {
                new TestCase { Input = "3", ExpectedOutput = "6" }
            }
        });
        await _context.SaveChangesAsync();

        var result = await _controller.GetProblems();
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetProblem_WithValidId_ShouldReturnProblem()
    {
        var problem = new Problem
        {
            Title = "FindMe",
            Description = "A simple test",
            InputFormat = "int",
            Constraints = "n >= 0",
            OutputFormat = "int",
            InputSample = "5",
            OutputSample = "10",
            DoKho = "Easy",
            DangBai = "Basic",
            TestCases = new List<TestCase>
            {
                new TestCase { Input = "5", ExpectedOutput = "10" }
            }
        };

        _context.Problems.Add(problem);
        await _context.SaveChangesAsync();

        var result = await _controller.GetProblem(problem.Id);
        var returned = Assert.IsType<Problem>(result.Value);
        Assert.Equal("FindMe", returned.Title);
    }

    [Fact]
    public async Task DeleteProblem_ShouldRemoveProblem()
    {
        var problem = new Problem
        {
            Title = "ToDelete",
            Description = "To be removed",
            InputFormat = "int",
            Constraints = "n > 0",
            OutputFormat = "int",
            InputSample = "7",
            OutputSample = "14",
            DoKho = "Easy",
            DangBai = "Demo",
            TestCases = new List<TestCase>
            {
                new TestCase { Input = "7", ExpectedOutput = "14" }
            }
        };
        _context.Problems.Add(problem);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteProblem(problem.Id);
        var ok = Assert.IsType<NoContentResult>(result);

        var dbProblem = await _context.Problems.FindAsync(problem.Id);
        Assert.Null(dbProblem);
    }
}
