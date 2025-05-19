using System.ComponentModel.DataAnnotations.Schema;

public class ExamStudent
{
    public int Id { get; set; }

    public int ExamRoomId { get; set; }
    public ExamRoom ExamRoom { get; set; }
    
    
    public User User { get; set; }
    public int UserId { get; set; }

    public int ExamPaperId { get; set; }
    public ExamPaper ExamPaper { get; set; }
   
    public string FullName { get; set; }
    public string IdentityCard { get; set; } // Số giấy tờ
    public string SeatCode { get; set; } // Vị trí thi: A1, A2,...
    public string ExamCode { get; set; } // Mã đề thi (nếu có)
    public string FeeStatus { get; set; } // VD: "Nộp lệ phí", "Chưa nộp"
}
