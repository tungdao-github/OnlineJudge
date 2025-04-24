window.onload = () => {
    fetchProblems();
    setupEventListeners();
};

let allProblems = []; // Store all problems for filtering

async function fetchProblems() {
    const token = localStorage.getItem('token');
    try {
        const response = await fetch('http://localhost:5024/api/problems', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        if (!response.ok) throw new Error("Không thể tải danh sách bài tập!");

        allProblems = await response.json();
        filterProblems();
        renderProblemDropdown(allProblems);

        const problemCountElement = document.getElementById("problem-count");
        problemCountElement.innerHTML = ` ${allProblems.length} `;
    }
    catch (err) {
        console.error("Lỗi khi tải bài tập:", err);
        alert("Lỗi khi tải danh sách bài tập. Vui lòng thử lại.");
    }
}

function setupEventListeners() {
    // Search input
    document.querySelector('input[placeholder="Tìm kiếm bài tập..."]')
        .addEventListener('input', filterProblems);

    // Difficulty filter
    document.querySelectorAll('select')[0]
        .addEventListener('change', filterProblems);

    // Topic filter
    document.querySelectorAll('select')[1]
        .addEventListener('change', filterProblems);

    // Sort filter
    document.querySelectorAll('select')[2]
        .addEventListener('change', filterProblems);
}

function filterProblems() {
    const searchInput = document.querySelector('input[placeholder="Tìm kiếm bài tập..."]').value.toLowerCase();
    const difficultyFilter = document.querySelectorAll('select')[0].value;
    const topicFilter = document.querySelectorAll('select')[1].value;
    const sortFilter = document.querySelectorAll('select')[2].value;

    let filteredProblems = [...allProblems];

    // Apply search filter
    if (searchInput) {
        filteredProblems = filteredProblems.filter(problem =>
            problem.title.toLowerCase().includes(searchInput) ||
            (problem.description && problem.description.toLowerCase().includes(searchInput))
        );
    }

    // Apply difficulty filter
    if (difficultyFilter !== "Tất cả độ khó") {
        filteredProblems = filteredProblems.filter(problem =>
            problem.doKho.toLowerCase() === difficultyFilter.toLowerCase()
        );
    }

    // Apply topic filter
    if (topicFilter !== "Tất cả chủ đề") {
        filteredProblems = filteredProblems.filter(problem =>
            problem.dangBai.toLowerCase() === topicFilter.toLowerCase()
        );
    }

    // Apply sorting
    switch (sortFilter) {
        case "Số lượt giải":
            // Assuming there's a 'solvedCount' property
            filteredProblems.sort((a, b) => (b.solvedCount || 0) - (a.solvedCount || 0));
            break;
        case "Mức độ":
            // Sort by difficulty: Khó -> Trung bình -> Dễ
            filteredProblems.sort((a, b) => {
                const order = { 'khó': 3, 'kho': 3, 'trung bình': 2, 'khá': 2, 'kha': 2, 'dễ': 1, 'de': 1 };
                return (order[b.doKho.toLowerCase()] || 0) - (order[a.doKho.toLowerCase()] || 0);
            });
            break;
        case "Mới nhất":
            // Assuming there's a 'createdAt' property
            filteredProblems.sort((a, b) => new Date(b.createdAt || 0) - new Date(a.createdAt || 0));
            break;
    }

    renderProblemTable(filteredProblems);

    // Update problem count
    const problemCountElement = document.getElementById("problem-count");
    problemCountElement.innerHTML = ` ${filteredProblems.length} `;
}

function renderProblemTable(problems) {
    const tableBody = document.getElementById("problemList");
    tableBody.innerHTML = problems.map(problem => `
        <tr class="hover:bg-gray-50 cursor-pointer">
            <td class="py-3 px-4">${problem.id}</td>
            <td class="py-3 px-4 font-medium text-indigo-600">${problem.title}</td>
            <td class="py-3 px-4">
                ${getDifficultyBadge(problem.doKho)}
            </td>
            <td class="py-3 px-4">
                <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs">${problem.dangBai}</span>
            </td>
            <td class="py-3 px-4">
                <span class="text-green-500">
                    <i class="fas fa-check-circle"></i>
                </span>
            </td>
            <td class="py-3 px-4 bg-green-100 text-green-800 px-2 py-1 rounded text-xs text-center">
                <button data-problem-id="${problem.id}">Chọn</button>
        </tr>
    `).join("");

    tableBody.addEventListener("click", e => {
        if (e.target.tagName === "BUTTON" && e.target.dataset.problemId) {
            const problemId = e.target.dataset.problemId;
            window.location.href = `problem.html?problemId=${problemId}`;
        }
    });
}

function getDifficultyBadge(doKho) {
    let bgClass = "";
    let textClass = "";

    switch (doKho.toLowerCase()) {
        case "de":
        case "dễ":
            bgClass = "bg-green-100";
            textClass = "text-green-800";
            break;
        case "khá":
        case "kha":
        case "trung bình":
            bgClass = "bg-yellow-100";
            textClass = "text-yellow-800";
            break;
        case "khó":
        case "kho":
            bgClass = "bg-red-100";
            textClass = "text-red-800";
            break;
        default:
            bgClass = "bg-gray-100";
            textClass = "text-gray-800";
    }

    return `
        <span class="${bgClass} ${textClass} px-2 py-1 rounded-full text-xs font-medium">
            ${doKho}
        </span>
    `;
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
            inputFormat: "",
            outputFormat: "",
            inputSample: "",
            outputSample: "",
            testCases: []
        })
    });

    if (response.ok) {
        alert("Đã cập nhật!");
        fetchProblems();
    } else {
        alert("Cập nhật thất bại!");
    }
}