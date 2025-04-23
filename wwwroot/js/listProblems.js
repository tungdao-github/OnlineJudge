window.onload = () => {
    fetchProblems();
};

async function fetchProblems() {
     const token = localStorage.getItem('token');
     console.log(token)
    try {
        // const token = localStorage.getItem('token');
        // const response = await fetch("http://localhost:5024/api/problems", {
        //     method: 'GET',
        //     headers: {
        //         'Authorization': `Bearer ${token}`, // Attach the token
        //         'Content-Type': 'application/json'
        //     }
        // });

        const response = await fetch('http://localhost:5024/api/problems', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`, // Attach the token
                'Content-Type': 'application/json'
            }
        });
        if (!response.ok) throw new Error("Không thể tải danh sách bài tập!");

        const problems = await response.json();
        renderProblemTable(problems);
        renderProblemDropdown(problems);
    } catch (err) {
        console.error("Lỗi khi tải bài tập:", err);
        alert("Lỗi khi tải danh sách bài tập. Vui lòng thử lại.");
    }
}

function renderProblemTable(problems) {
    const tableBody = document.getElementById("problemList");
    tableBody.innerHTML = problems.map(problem => `
        <tr>
            <td>${problem.id}</td>
            <td>${problem.title}</td>
            <td>${problem.description}</td>
            <td>${problem.doKho}</td>
            <td>${problem.dangBai}</td>
            <td>
                <button data-problem-id="${problem.id}">Chọn</button>
                <button onclick = "deleteProblem(${problem.id})"> 🗑️Xóa </button> 
                <button onclick = "updateProblem(${problem.id})"> Cập nhật </button>
             <td>
               
        </tr>
    `).join("");

    // Gắn 1 lần sự kiện cho toàn bộ bảng thay vì từng button
    tableBody.addEventListener("click", e => {
        if (e.target.tagName === "BUTTON" && e.target.dataset.problemId) {
            const problemId = e.target.dataset.problemId;
            window.location.href = `problem.html?problemId=${problemId}`;
        }
    });
}

function renderProblemDropdown(problems) {
    const select = document.getElementById("problemId");
    if (!select) return;

    select.innerHTML = problems.map(p =>
        `<option value="${p.id}">${p.title}</option>`
    ).join("");
}

  async function deleteProblem(id) {
      if (confirm("Bạn có chắc muốn xóa bài này?")) {
          await fetch(`http://localhost:5024/api/problems/${id}`, {
              method: "DELETE"
          });
          alert("Đã xóa!");
          fetchProblems();
      }
  }

  async function updateProblem(id) {
      const row = document.querySelector(`#problemList tr td:first-child:contains("${id}")`).parentNode;
      const title = row.children[1].querySelector("input").value;
      const description = row.children[2].querySelector("input").value;

      const response = await fetch(`http://localhost:5024/api/problems/${id}`, {
          method: "PUT",
          headers: {
              "Content-Type": "application/json"
          },
          body: JSON.stringify({
              id,
              title,
              description,
              inputFormat: "", // Cập nhật thêm nếu bạn muốn
              outputFormat: "",
              inputSample: "",
              outputSample: "",
              testCases: []
          })
      });

      if (response.ok) {
          alert("Đã cập nhật!");
          loadProblems();
      } else {
          alert("Cập nhật thất bại!");
      }
  }
