document.addEventListener("DOMContentLoaded", async () => {
    const submissionId = new URLSearchParams(window.location.search).get("submissionId");
    if (!submissionId) {
        document.getElementById("details").innerText = "Invalid submission ID!";
        return;
    }

    async function fetchResult() {
        try {
            const response = await fetch(`http://localhost:5024/api/Submissions/GetResult/${submissionId}`);
            if (!response.ok) throw new Error("Failed to fetch submission result.");

            const result = await response.json();
            document.getElementById("status").innerText = result.status;
            document.getElementById("execution-time").innerText = result.executionTime;
            document.getElementById("memory-used").innerText = result.memoryUsed;
            document.getElementById("details").innerText = result.error || "No error";
            document.getElementById("kq").textContent = result.result;

            if (result.status === "Pending") {
                setTimeout(fetchResult, 2000); // Nếu chưa chấm xong, tự động thử lại sau 2 giây
            }
        } catch (error) {
            document.getElementById("details").innerText = "Error loading result!";
        }
    }

    fetchResult();
});
