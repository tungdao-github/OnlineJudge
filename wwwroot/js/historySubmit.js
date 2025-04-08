document.addEventListener('DOMContentLoaded', async () => {
    let response = await fetch('http://localhost:5024/api/Submissions/history');

    const data = await response.json();
    console.log(data)
    function getResultClass(result) {
        if (result.includes("Accepted")) return 'accepted';
        if (result.includes("Wrong Answer")) return 'wrong';
        if (result.includes("Compilation failed")) return 'compile';
        return '';
    }

    const tbody = document.querySelector("#submissionTable tbody");

    data.forEach(item => {
        const row = document.createElement("tr");

        row.innerHTML = `
                <td>${item.submissionId}</td>
                <td>${item.username}</td>
                <td>${item.problemTitle}</td>
                <td class="result ${getResultClass(item.result)}">${item.result}</td>
                <td>${new Date(item.submittedAt).toLocaleString()}</td>
                <td>${item.executionTimeMs}</td>
                <td>${item.memoryUsageBytes}</td>
              `;

        tbody.appendChild(row);
    });
});