document.addEventListener('DOMContentLoaded', async () => {
    let response = await fetch('http://localhost:5024/api/Submissions/history');
    const data = await response.json();
    console.log(data);

    function getResultClass(result) {
        const correctCount = (result.match(/TrueTestcase/g) || []).length;
        const totalCount = result.split(/\s+/).filter(Boolean).length; // Đếm số từ (testcase)

        if (correctCount === totalCount) return 'accepted'; // Tất cả đúng → Accepted
        if (correctCount === 0) return 'wrong';             // Không có cái nào đúng → Wrong Answer
        return 'partial';                                   // Đúng một phần → CSS riêng (vd: màu vàng)
    }

    function formatTestcaseResult(result) {
        const correctCount = (result.match(/TrueTestcase/g) || []).length;
        const totalCount = result.split(/\s+/).filter(Boolean).length;
        return `${correctCount}/${totalCount}`; // Ví dụ: "20/30"
    }

    const tbody = document.querySelector("#submissionTable tbody");

    data.forEach(item => {
        const row = document.createElement("tr");
        row.innerHTML = `
            <td>${item.submissionId}</td>
            <td>${item.username}</td>
            <td>${item.problemTitle}</td>
            <td class="result ${getResultClass(item.result)}">${formatTestcaseResult(item.result)}</td>
            <td>${new Date(item.submittedAt).toLocaleString()}</td>
            <td>${item.executionTimeMs}</td>
            <td>${item.memoryUsageBytes}</td>
        `;
        tbody.appendChild(row);
    });
});