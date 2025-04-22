
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineJudgeAPI.Models;
using Microsoft.AspNetCore.Authorization;
using OnlineJudgeAPI.DTOs;
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

     
        [HttpGet]
        public async Task<ActionResult<List<Problem>>> GetProblems() {
            return await _context.Problems.ToListAsync();
        }
      
        [HttpGet("{id}")]
        public async Task<ActionResult<Problem>> GetProblem(int id) {
            var problem = await _context.Problems.FirstAsync(p => p.Id == id);
            if(problem == null) return NotFound("Khong tim thay problem");
             return problem;
        }
        [HttpPost]
        public async Task<ActionResult<Problem>> CreateProblem([FromBody]  ProblemCreateDTO dto) {
           
           var problem = new Problem{
                        Title = dto.Title,
                        Description = dto.Description,
                        InputFormat = dto.InputFormat,
                        Constraints = dto.Constraints,
                        OutputFormat = dto.OutputFormat,
                        InputSample = dto.InputSample,
                        OutputSample = dto.OutputSample,
                        DoKho = dto.DoKho,
                        DangBai = dto.DangBai,
                        
                        TestCases = dto.TestCases

           };
            _context.Problems.Add(problem);
           await _context.SaveChangesAsync();
           return Ok(problem);
           }
        // [Authorize(Roles = "Admin")]
        // [HttpPost]
        // public async Task<IActionResult> CreateProblem([FromBody] ProblemCreateDTO dto)
        // {
        //     //var contest = await _context.Contests.FindAsync(dto.ContestId);
        //     //if (contest == null) return BadRequest("Contest not found");

        //     var problem = new Problem
        //     {
        //         Title = dto.Title,
        //         Description = dto.Description,
        //         InputFormat = dto.InputFormat,
        //         Constraints = dto.Constraints,
        //         OutputFormat = dto.OutputFormat,
        //         InputSample = dto.InputSample,
        //         OutputSample = dto.OutputSample,
        //         DoKho = dto.DoKho,
        //         DangBai = dto.DangBai,
        //         //ExpectedOutput = dto.ExpectedOutput,
        //         TestCases = dto.TestCases
        //     };

        //     _context.Problems.Add(problem);
        //     await _context.SaveChangesAsync();

        //     return Ok(problem);
        // }
        //[Authorize(Roles = "Admin")]
        //[HttpPost]
        //public async Task<IActionResult> CreateProblem([FromBody] Problem problem)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        //        return BadRequest(new { errors });
        //    }
        //    _context.Problems.Add(problem);
        //    await _context.SaveChangesAsync();
        //    return Ok(problem);
        //}
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
        // [Authorize(Roles = "Admin")]
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

        
        public class TestCaseRequest
        {
            public List<TestCase> TestCases { get; set; }
        }

       
        // [Authorize(Roles = "1")]
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
