namespace OnlineJudgeAPI.DTOs
{
    public class Diem
    {
        public int problemId { get; set; }
        public int score { get; set; }
    }
    public class CreateContestRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<Diem> ProblemIds { get; set; }
    }
}
