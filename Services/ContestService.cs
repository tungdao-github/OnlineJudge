// using Microsoft.EntityFrameworkCore;
// using OnlineJudgeAPI.Models;
// using OnlineJudgeAPI.Services;

// public class ContestService : IContestService
// {
//     private readonly ApplicationDbContext _context;

//     public ContestService(ApplicationDbContext context)
//     {
//         _context = context;
//     }

//     public async Task<List<Contest>> GetAllAsync()
//     {
//         return await _context.Contests.Include(c => c.Problems).ToListAsync();
//     }

//     public async Task<Contest?> GetByIdAsync(int id)
//     {
//         return await _context.Contests
//             .Include(c => c.Problems)
//             .FirstOrDefaultAsync(c => c.Id == id);
//     }

//     public async Task CreateAsync(Contest contest)
//     {
//         _context.Contests.Add(contest);
//         await _context.SaveChangesAsync();
//     }

//     public async Task UpdateAsync(Contest contest)
//     {
//         _context.Contests.Update(contest);
//         await _context.SaveChangesAsync();
//     }

//     public async Task DeleteAsync(int id)
//     {
//         var contest = await _context.Contests.FindAsync(id);
//         if (contest != null)
//         {
//             _context.Contests.Remove(contest);
//             await _context.SaveChangesAsync();
//         }
//     }

//     public async Task<List<ContestUser>> GetContestUsersAsync(int contestId)
//     {
//         return await _context.ContestUsers
//             .Include(cu => cu.User)
//             .Where(cu => cu.ContestId == contestId)
//             .ToListAsync();
//     }

//     public async Task AddUserToContestAsync(string userId, int contestId)
//     {
//         var contestUser = new ContestUser
//         {
//             UserId = userId,
//             ContestId = contestId
//         };
//         _context.ContestUsers.Add(contestUser);
//         await _context.SaveChangesAsync();
//     }
// }
