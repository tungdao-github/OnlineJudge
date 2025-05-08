
public class SubmissionRequest
{
    public string Code { get; set; }
    public int ProblemId { get; set; }
    public string Language { get; set; }
    public string ConnectionId { get; set; }
    public int userId {get; set;}
    public int? ContestId { get; set; }
}
