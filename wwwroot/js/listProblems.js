// Lấy danh sách bài tập từ backend
async function fetchProblems() {
    const response = await fetch("http://localhost:5024/api/problems");
    const problems = await response.json();

    const tableBody = document.getElementById("problemList");
    const problemSelect = document.getElementById("problemId");

    tableBody.innerHTML = "";
    problemSelect.innerHTML = "";

    problems.forEach(problem => {
        // Thêm vào bảng danh sách bài tập
        let dem = 0;
        const row = `<tr>
            <td>${problem.id}</td>
            <td>${problem.title}</td>
            <td>${problem.description}</td>
            <td><button id = "btn${problem.id}" onclick="selectProblem(${problem.id})">Chọn</button></td>
        </tr>`;
        tableBody.innerHTML += row;
        function selectProblem(problemId) {
            document.getElementById("problemId").value = problemId;
            //window.location.href = "/submit.html?problemId=" + problemId;
            
        }
        // Thêm vào dropdown chọn bài tập để nộp bài
        const option = `<option value="${problem.id}">${problem.name}</option>`;
        problemSelect.innerHTML += option;
        
    });

    let dem = 0;
    problems.forEach(problem => {   
        dem++;
        document.getElementById(`btn${problem.id}`).addEventListener("click", () => {
            window.location.href = "http://localhost:5024/submit.html?problemId=" + problem.id;
        });
    });
}


// Chọn bài tập để nộp bài


//// Lấy danh sách bài nộp từ backend
//async function fetchSubmissions() {
//    const response = await fetch("http://localhost:5024/api/submission");
//    const submissions = await response.json();

//    const tableBody = document.getElementById("submissionList");
//    tableBody.innerHTML = "";

//    submissions.forEach(sub => {
//        const row = `<tr>
//            <td>${sub.id}</td>
//            <td>${sub.problemName}</td>
//            <td>${sub.language}</td>
//            <td>${sub.status}</td>
//            <td>${sub.result || "Chưa có"}</td>
//            <td><textarea id="code-${sub.id}">${sub.code}</textarea></td>
//            <td><button onclick="resubmit(${sub.id})">Nộp lại</button></td>
//        </tr>`;
//        tableBody.innerHTML += row;
//    });
//}

// Hàm nộp bài
//document.getElementById("submitBtn").addEventListener("click", async () => {
//    const problemId = document.getElementById("problemId").value;
//    const language = document.getElementById("language").value;
//    const code = document.getElementById("code").value;

//    if (!code.trim()) {
//        alert("Vui lòng nhập mã nguồn!");
//        return;
//    }

//    const response = await fetch("http://localhost:5000/api/submission", {
//        method: "POST",
//        headers: { "Content-Type": "application/json" },
//        body: JSON.stringify({ problemId, language, code })
//    });

//    const result = await response.json();
//    document.getElementById("result").innerHTML = result.message;

//    fetchSubmissions();
//});

//// Hàm nộp lại bài
//async function resubmit(submissionId) {
//    const code = document.getElementById(`code-${submissionId}`).value;

//    if (!code.trim()) {
//        alert("Mã nguồn không được để trống!");
//        return;
//    }

//    const response = await fetch(`http://localhost:5024/api/submission/${submissionId}`, {
//        method: "PUT",
//        headers: { "Content-Type": "application/json" },
//        body: JSON.stringify({ code })
//    });

//    const result = await response.json();
//    alert(result.message);
//    fetchSubmissions();
//}

// Tải danh sách bài tập và bài nộp khi trang mở
window.onload = () => {
    fetchProblems();
    //fetchSubmissions();
};
