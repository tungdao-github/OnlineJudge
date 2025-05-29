// Giả sử JWT đã được lưu trong localStorage
const token = localStorage.getItem("token");

const studentInfo = document.getElementById("studentInfo");
const problemList = document.getElementById("problemList");
const errorDiv = document.getElementById("error");
const examRoomId = new URLSearchParams(location.search).get("examroomid");
if (!token) {
    errorDiv.innerText = "Bạn chưa đăng nhập.";
} else {
    fetch("http://localhost:5024/api/ExamRoom/my-problems/${examRoomId}", {
            method: "GET",
            headers: {
                "Authorization": "Bearer " + token,
                "Content-Type": "application/json"
            },
            body: JSON.stringify(examRoomId)
        })
        .then(response => {
            if (!response.ok) {
                throw new Error("Không tìm thấy dữ liệu hoặc chưa được phân mã đề.");
            }
            return response.json();
        })
        .then(data => {
            
            studentInfo.innerHTML = `
        <p><strong>Họ tên:</strong> ${data.fullName}</p>
        <p><strong>Mã đề:</strong> ${data.examPaperCode}</p>
      `;

            if (data.problems.length === 0) {
                problemList.innerHTML = "<p>Không có bài tập nào.</p>";
                return;
            }

            data.problems.forEach(problem => {
                const div = document.createElement("div");
                div.className = "problem";
                div.innerHTML = `
          <h3><a href = "/problem.html?problemId=${problem.problemId}&contestId=null$examroomid=${examRoomId}">${problem.title} <a/></h3>
          <p>${problem.description}</p>
        
          <button onclick = "myFunction(${problem.problemId}, ${examRoomId})"> Vào Thi </button>
        `;
                problemList.appendChild(div);
                
            });
        })
        .catch(error => {
            errorDiv.innerText = error.message;
        });
}

function myFunction(problemId, examRoomId) {
    window.location.href ="/problem.html?problemId=" + problemId  + "&contestId=null&examroomid="+ examRoomId
}