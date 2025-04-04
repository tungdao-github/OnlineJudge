function loadMonacoEditor() {
    return new Promise((resolve) => {
        require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs' } });
        require(["vs/editor/editor.main"], function () {
            resolve();
        });
    });
}

document.addEventListener('DOMContentLoaded', async () => {
    const editorContainer = document.getElementById('editor-container');
    if (!editorContainer) return;

    await loadMonacoEditor();
    const savedCode = localStorage.getItem('savedCode') || "// Write your code here...";
    window.editor = monaco.editor.create(editorContainer, {
        value: savedCode,
        language: "cpp",
        theme: "vs-dark",
        automaticLayout: true
    });

    window.editor.onDidChangeModelContent(() => {
        localStorage.setItem('savedCode', editor.getValue());
    });

    console.log("editor-container = " + window.editor.getValue())
    const submitForm = document.getElementById('submit-form');
    const urlParams = new URLSearchParams(window.location.search);
    console.log(urlParams)
    //var problemid = ;
    //console.log(problemid)

    if (submitForm) {
        submitForm.addEventListener('submit', async (event) => {
            event.preventDefault();

            //const submitButton = document.getElementById('submit-button');
            //submitButton.disabled = true;
            //submitButton.innerText = "Submitting...";
            const token = localStorage.getItem('token'); 
            try {
                const response = await fetch('http://localhost:5024/api/Submissions/submit', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json','Authorization': `Bearer ${token}` },
                    body: JSON.stringify({
                        ProblemId: urlParams.get("problemId"),
                        Code: editor.getValue(),
                        Language: document.getElementById('language').value
                    }),
                });

                const result = await response.json();
                if (response.ok) {
                    alert("submit thanh cong")
                    window.location.href = `result.html?submissionId=${result.submissionId}`;
                } else {
                    alert('Submission failed: ' + (result.message || 'Unknown error'));
                }
            } catch (error) {
                console.error('Error:', error);
                alert('An error occurred.');
            } finally {
                //submitButton.disabled = false;
                //submitButton.innerText = "Submit";
            }
        });
    } else {
        console.log('Submit form not found.');
    }
});


;
