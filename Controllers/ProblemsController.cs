
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineJudgeAPI.Models;
using Microsoft.AspNetCore.Authorization;
namespace OnlineJudgeAPI.Controllers
{
    [Route("api/problems")]
    [ApiController]
    public class ProblemsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProblemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/problems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Problem>>> GetProblems()
        {
            return await _context.Problems.ToListAsync();
        }

        // GET: /api/problems/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Problem>> GetProblem(int id)
        {
            var problem = await _context.Problems.FindAsync(id);

            if (problem == null)
            {
                return NotFound();
            }

            return problem;
        }

        // POST: /api/problems
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Problem>> CreateProblem([FromBody]Problem problem)
        {
            _context.Problems.Add(problem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProblem), new { id = problem.Id }, problem);
        }

        // PUT: /api/problems/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProblem(int id, Problem problem)
        {
            if (id != problem.Id)
            {
                return BadRequest();
            }

            _context.Entry(problem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Problems.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: /api/problems/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblem(int id)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null)
            {
                return NotFound();
            }

            _context.Problems.Remove(problem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // API thêm test case số lượng lớn
        //[HttpPost("{id}/testcases")]
        //public async Task<IActionResult> AddTestCases(int id, List<TestCase> testCases)
        //{
        //    var problem = await _context.Problems.FindAsync(id);
        //    if (problem == null)
        //    {
        //        return NotFound();
        //    }

        //    foreach (var testCase in testCases)
        //    {
        //        testCase.ProblemId = id;
        //    }

        //    _context.TestCases.AddRange(testCases);
        //    await _context.SaveChangesAsync();
        //    return Ok(new { message = "Test cases added successfully!" });
        //}
        public class TestCaseRequest
        {
            public List<TestCase> TestCases { get; set; }
        }

        //[HttpPost("{id}/testcases")]
        //public async Task<IActionResult> AddTestCases(int id, [FromBody] TestCaseRequest request)
        //{
        //    var problem = await _context.Problems.FindAsync(id);
        //    if (problem == null)
        //    {
        //        return NotFound();
        //    }
        //    Console.WriteLine(request.TestCases);
        //    if (request?.TestCases == null || !request.TestCases.Any())
        //    {
        //        return BadRequest(new { message = "The testCases field is required." });
        //    }

        //    foreach (var testCase in request.TestCases)
        //    {
        //        testCase.ProblemId = id;
        //    }

        //    _context.TestCases.AddRange(request.TestCases);
        //    await _context.SaveChangesAsync();
        //    return Ok(new { message = "Test cases added successfully!" });
        //}
        [HttpPost("{id}/testcases")]
        public async Task<IActionResult> AddTestCases(int id, [FromBody] TestCaseRequest request)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null)
            {
                return NotFound();
            }

            if (request?.TestCases == null || !request.TestCases.Any())
            {
                return BadRequest(new { message = "The testCases field is required." });
            }

            foreach (var testCase in request.TestCases)
            {
                testCase.ProblemId = id;  // ✅ Ensure ProblemId is assigned
                testCase.Problem = null;  // ✅ Avoid unnecessary serialization
            }

            _context.TestCases.AddRange(request.TestCases);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Test cases added successfully!" });
        }

    }
}
