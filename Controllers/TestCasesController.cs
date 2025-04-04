using Microsoft.AspNetCore.Mvc;
using OnlineJudgeAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace OnlineJudgeAPI.Controllers
{
    [Route("api/testcases")]
    [ApiController]
    public class TestCasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestCasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách test case của một bài toán
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTestCase(int id)
        {
            var testCase = await _context.TestCases.Where(tc => tc.ProblemId == id).ToListAsync();
            if (testCase == null || !testCase.Any())
            {
                return NotFound(new { message = "No test cases found for this problem." });
            }
            return Ok(testCase);
        }
        //[HttpGet("{problemId}")]
        //public async Task<ActionResult<IEnumerable<TestCase>>> GetTestCases(int problemId)
        //{
        //    return await _context.TestCases.Where(tc => tc.ProblemId == problemId).ToListAsync();
        //}

        // Thêm test case mới
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<TestCase>> AddTestCase(TestCase testCase)
        {
            _context.TestCases.Add(testCase);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTestCase), new { problemId = testCase.ProblemId }, testCase);
        }

        // Xóa test case
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestCase(int id)
        {
            var testCase = await _context.TestCases.FindAsync(id);
            if (testCase == null) return NotFound();

            _context.TestCases.Remove(testCase);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
