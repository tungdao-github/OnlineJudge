document.getElementById("addProblemForm").addEventListener("submit", async function (event) {
    event.preventDefault();

    const title = document.getElementById("title").value;
    const description = document.getElementById("description").value;
    const inputFormat = document.getElementById("inputFormat").value;
    const outputFormat = document.getElementById("outputFormat").value;
    const inputSample = document.getElementById("inputSample").value;
    const constraints = document.getElementById("constraints").value;
    const outputSample = document.getElementById("outputSample").value;
    const doKho = document.getElementById("doKho").value;
    const dangBai = document.getElementById("dangBai").value;
    console.log(inputSample + " " + outputSample)
    const token = localStorage.getItem("token") || sessionStorage.getItem("token");
    console.log(token)
    try {
        console.log("Request Headers:", {
            "Authorization": "Bearer " + token,
            "Content-Type": "application/json"
        });
        // Step 1: Add problem
        let response = await fetch("/api/problems", {
            method: "POST",
            headers: { "Authorization": "Bearer " + token, "Content-Type": "application/json" },
            body: JSON.stringify({
                title,
                description,
                inputFormat,
                constraints,
                outputFormat,
                inputSample,
                outputSample,
                doKho,
                dangBai,
                contestId: 1,
                testCases: []
            })
        });

        let problem = await response.json();
        let testCases = [];
        document.querySelectorAll(".test-case").forEach(tc => {
            let input = tc.querySelector(".input").value;
            let output = tc.querySelector(".output").value;
            testCases.push({ input, expectedOutput: output, problemId: problem.id });
        });
        console.log("Problem ID:", problem.id);
        console.log("Test Cases:", JSON.stringify(testCases));

        if (!problem.id) {
            console.error("Problem ID is missing! The first API call may have failed.");
            document.getElementById("message").textContent = "Lỗi khi thêm bài tập!";
            return;
        }

        // Step 2: Add test cases
        let testCaseResponse = await fetch(`/api/problems/${problem.id}/testcases`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                testCases: testCases.map(tc => ({
                    input: tc.input,
                    expectedOutput: tc.expectedOutput,
                    problemId: problem.id // ✅ Send only problemId, not problem object
                }))
            })
        });

        document.getElementById("message").textContent = "Bài tập và test case đã được thêm thành công!";
        document.getElementById("addProblemForm").reset();
    } catch (error) {
        console.error("Lỗi khi thêm bài tập:", error);
        document.getElementById("message").textContent = "Lỗi khi thêm bài tập!";
    }
});
let dem = 1
function addTestCase() {
    let testCaseDiv = document.createElement("div");
    testCaseDiv.classList.add("test-case");
    testCaseDiv.innerHTML = `
        <label>Input${dem}:</label>
        <textarea class="input" required></textarea>
        <label>Output${dem++}:</label>
        <textarea class="output" required></textarea>
    `;
    document.getElementById("testCases").appendChild(testCaseDiv);
}
