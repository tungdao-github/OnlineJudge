public class ExamRoom
{
    public int Id { get; set; }
    public string RoomCode { get; set; } // VD: "C5-201"
    public string? RoomName { get; set; }
    public string? SubjectName { get; set; } // VD: "Lập trình C"
    public string? SubjectCode { get; set; } // VD: "CSE101"
    public DateTime? ExamDate { get; set; }
    public string? ExamTime { get; set; } // "18h30"
    public string? ExamType { get; set; } // "Tự luận", "Vấn đáp", "Lập trình"
    public string? ClassCode { get; set; } // VD: "Kế toán - Tài chính"
    public string? Attempt { get; set; } // VD: "Lần 1"
    public string? ExamId { get; set; } // VD: "HP-FIN748"
    public DateTime StartTime { get; set; }
    public ICollection<ExamPaper> ExamPapers { get; set; }
    public ICollection<ExamStudent> Students { get; set; }

}
// public class ExamRoom
// {
//     public int Id { get; set; }
//     public string RoomName { get; set; }
//     public DateTime StartTime { get; set; }
//     public ICollection<ExamPaper> ExamPapers { get; set; }
//     public ICollection<ExamStudent> Students { get; set; }
// }