﻿let count = 1;

document.addEventListener("DOMContentLoaded", async () => {
    function isTokenExpired(token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const exp = payload.exp * 1000; // Convert seconds to milliseconds
            return Date.now() >= exp;
        } catch (err) {
            console.error("Token decode error:", err);
            return true; // Nếu không decode được thì coi như token hết hạn
        }
    }
    const token = localStorage.getItem("token");
    console.log(token)
    if (token == null) {
        alert("phien dang nhap da ket thuc");
        location.href = "/index.html";
    }
    const $ = id => document.getElementById(id);

    const updateFinalResult = (data) => {
        $("status").textContent = data.status || "N/A";
        document.getElementById("score").textContent = data.score ;
        console.log("score = " + data.score)
    

        if (!data.result || !Array.isArray(data.result)) return;

       
    };

    
    const appendTestCaseResult = (data) => {
        const container = document.getElementById("testcase-results");
        const div = document.createElement("div");
        div.style.border = "1px solid #ccc";
        div.style.padding = "10px";
        div.style.marginBottom = "10px";
        div.style.backgroundColor = data.passed ? "#e0ffe0" : "#ffe0e0";
        div.innerHTML = `        
            <h3> Testcase ${count++} </h3>
             <strong>Error:</strong><br><pre>${!data.compilationError ? "Không có" : data.compilationError?.trim() }</pre>
            📥 < strong >Input:</><br><pre>${data.input?.trim()}</pre>
            🧾 <strong>Output:</strong><br><pre>${data.actualOutput?.trim()}</pre>
            🎯 <strong>Expected:</strong><br><pre>${data.expectedOutput?.trim()}</pre>
            ✅ <strong>Passed:</strong> ${data.passed ? "✔️" : "❌"}<br>
            ⏱ <strong>Time:</strong> ${data.executionTimeMs || 'N/A'} ms<br>
            📦 <strong>Memory:</strong> ${data.memoryUsageBytes || 'N/A'} bytes<br>
        `;
        container.appendChild(div);
    };

    // Setup SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/testCaseHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    let connectionId = null;
    document.getElementById('loading-spinner').style.display = 'block';
    connection.on("ReceiveTestCaseResult", (data) => {
        console.log("📩 Received test case result via SignalR:", data);
        document.getElementById('loading-spinner').style.display = 'none';
        appendTestCaseResult(data);
        
    });

    try {
        await connection.start();
        console.log("✅ SignalR connected");

        connectionId = await connection.invoke("GetConnectionId");
        console.log("🔗 Connection ID:", connectionId);

        if (!connectionId) throw new Error("❌ ConnectionId is null or empty!");

        const code = localStorage.getItem('code');
        const language = localStorage.getItem('language');
        const problemId = localStorage.getItem('problemId');
        const token = localStorage.getItem('token');
        const contestId = new URLSearchParams(location.search).get("contestId");
        const examRoomId = new URLSearchParams(location.search).get("examroomId");
        console.log("hello")
        console.log("examroomID= "+ examRoomId)
        if (!token || isTokenExpired(token)) {
            alert("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
            localStorage.removeItem("token");
            window.location.href = "/index.html"; // hoặc /login.html tùy bạn
            return;
        }
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
                ConnectionId: connectionId,
                contestId: contestId == null ? null : Number(contestId),
                examRoomId: examRoomId == null ? null : Number(examRoomId),
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
                const response = await fetch(`http://localhost:5024/api/Submissions/GetResult/${submissionId}`);
                if (!response.ok) throw new Error("Fetch error");

                const data = await response.json();
                console.log(data.score)
                if (data.status !== "Pending" && data.status !== "Running") {
                    updateFinalResult(data);
                } else {
                    const delay = data.status === "Pending" ? 1000 : 300;
                    setTimeout(() => requestAnimationFrame(pollResult), delay);
                }
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
