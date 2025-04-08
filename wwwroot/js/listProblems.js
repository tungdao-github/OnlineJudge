window.onload = () => {
    fetchProblems();
};

async function fetchProblems() {
    try {
        const response = await fetch("http://localhost:5024/api/problems");
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
            <td><button data-problem-id="${problem.id}">Chọn</button></td>
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
