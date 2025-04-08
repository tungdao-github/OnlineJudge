namespace OnlineJudgeAPI.Models
{
    //public class Contest
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public string Password { get; set; } // Mật khẩu bảo vệ kỳ thi
    //    public DateTime StartTime { get; set; }
    //    public DateTime EndTime { get; set; }
    //    public string Code { get; set; }
    //    public bool? IsEvaluated { get; set; }
    //    public ICollection<Problem> Problems { get; set; }
    //    public ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();
    //}
    public class Contest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Code { get; set; }  // Mã kỳ thi
        public string Password { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public ICollection<ContestProblem> ContestProblems { get; set; }
    }

}
