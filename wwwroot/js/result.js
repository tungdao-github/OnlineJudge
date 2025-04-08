document.addEventListener("DOMContentLoaded", async () => {
    const $ = id => document.getElementById(id);

    const updateFinalResult = (data) => {
        $("status").textContent = data.status || "N/A";
        $("executionTimeMs").textContent = data.executionTime ? `${data.executionTime} ms` : "N/A";
        $("memoryUsageBytes").textContent = data.memoryUsageBytes ? `${data.memoryUsageBytes} bytes` : "N/A";
        $("details").textContent = data.compilationError || "No error";

        if (!data.result || !Array.isArray(data.result)) return;

        data.result.forEach((test, index) => {
            appendTestCaseResult({
                index: index + 1,
                input: test.input,
                output: test.actualOutput,
                expectedOutput: test.expectedOutput,
                passed: test.passed,
                executionTime: test.executionTimeMs,
                memoryUsage: test.memoryUsageBytes
            });
        });
    };

    const appendTestCaseResult = (data) => {
        const container = document.getElementById("testcase-results");
        const div = document.createElement("div");
        div.style.border = "1px solid #ccc";
        div.style.padding = "10px";
        div.style.marginBottom = "10px";
        div.style.backgroundColor = data.passed ? "#e0ffe0" : "#ffe0e0";
        div.innerHTML = `
            <strong>🧪 Test Case ${data.actualOutput}</strong><br>
            📥 <strong>Input:</strong><br><pre>${data.input?.trim()}</pre>
            🧾 <strong>Output:</strong><br><pre>${data.actualOutput?.trim()}</pre>
            //🎯 <strong>Expected:</strong><br><pre>${data.expectedOutput?.trim()}</pre>
            //✅ <strong>Passed:</strong> ${data.passed ? "✔️" : "❌"}<br>
            //⏱ <strong>Time:</strong> ${data.executionTimeMs || 'N/A'} ms<br>
            //📦 <strong>Memory:</strong> ${data.memoryUsageBytes || 'N/A'} bytes<br>
        `;
        container.appendChild(div);
    };

    // Setup SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/testCaseHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    let connectionId = null;

    connection.on("ReceiveTestCaseResult", (data) => {
        console.log("📩 Received test case result via SignalR:", data);
        appendTestCaseResult(data);
    });

    try {
        // Start SignalR connection
        await connection.start();
        console.log("✅ SignalR connected");

        // Get connectionId
        connectionId = await connection.invoke("GetConnectionId");
        console.log("🔗 Connection ID:", connectionId);

        if (!connectionId) throw new Error("❌ ConnectionId is null or empty!");

        // Get submission data
        const code = localStorage.getItem('code');
        const language = localStorage.getItem('language');
        const problemId = localStorage.getItem('problemId');
        const token = localStorage.getItem('token');

        // Call /submit
        const submitResponse = await fetch('http://localhost:5024/api/Submissions/submit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                ProblemId: Number(problemId),
                Code: code,
                Language: language,
                ConnectionId: connectionId
            }),
            cache: 'no-store'
        });

        let result;
        if (submitResponse.headers.get("content-type")?.includes("application/json")) {
            result = await submitResponse.json();
        } else {
            const text = await submitResponse.text();
            throw new Error(text || "Unexpected error");
        }

        if (!submitResponse.ok) {
            throw new Error(result.message || JSON.stringify(result.errors) || "Submission failed");
        }

        const submissionId = result.submissionId || result.id;
        if (!submissionId) {
            $("details").textContent = "Invalid submission ID.";
            return;
        }

        // Poll result until complete
        const pollResult = async () => {
            try {
                //const response = await fetch(`http://localhost:5024/api/Submissions/GetResult/${submissionId}`);
                //if (!response.ok) throw new Error("Fetch error");

                //const data = await response.json();
                //if (data.status !== "Pending" && data.status !== "Running") {
                //    updateFinalResult(data);
                //} else {
                //    const delay = data.status === "Pending" ? 1000 : 300;
                //    setTimeout(() => requestAnimationFrame(pollResult), delay);
                //}
            } catch (err) {
                console.warn("Retrying fetch:", err.message);
                setTimeout(() => requestAnimationFrame(pollResult), 1000);
            }
        };

        pollResult();

    } catch (err) {
        console.error("❌ Submission failed:", err);
        $("details").textContent = err.message || "Unexpected error!";
    }
});
