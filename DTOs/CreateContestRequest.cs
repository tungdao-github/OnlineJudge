namespace OnlineJudgeAPI.DTOs
{
    public class CreateContestRequest
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<int> ProblemIds { get; set; }
    }

}
