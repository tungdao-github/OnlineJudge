

const loadMonacoEditor = () =>
    new Promise(resolve => require(['vs/editor/editor.main'], resolve));

let editor, editorLoaded = false;

const initEditorIfNeeded = async () => {
    if (editorLoaded) return;
    await loadMonacoEditor();
    editor = monaco.editor.create(document.getElementById('editor-container'), {
        value: localStorage.getItem('savedCode') || "// Viết code tại đây...",
        language: "cpp",
        theme: "vs-dark",
        automaticLayout: true,
        minimap: { enabled: false },
        fontSize: 14
    });
    editor.onDidChangeModelContent(() => {
        localStorage.setItem('savedCode', editor.getValue());
    });
    editorLoaded = true;
};

const isInViewport = el => {
    const r = el.getBoundingClientRect();
    return r.top < window.innerHeight && r.bottom >= 0;
};

document.addEventListener('DOMContentLoaded', () => {
    const editorContainer = document.getElementById('editor-container');
    const submitForm = document.getElementById('submit-form');
    const languageSelect = document.getElementById('language');

    const token = localStorage.getItem('token');
    const problemId = +new URLSearchParams(location.search).get("problemId");

    if (!editorContainer || !submitForm || !languageSelect || !problemId || !token)
        return alert("Thiếu dữ liệu để gửi bài!");

    const scrollHandler = () => {
        if (isInViewport(editorContainer)) {
            initEditorIfNeeded();
            window.removeEventListener('scroll', scrollHandler);
        }
    };
    window.addEventListener('scroll', scrollHandler, { passive: true });
    scrollHandler();

    submitForm.onsubmit = async e => {
        e.preventDefault();
        const code = editorLoaded ? editor.getValue() : '';
        const language = languageSelect.value;

        if (!code.trim()) return alert("Vui lòng nhập mã trước khi gửi!");

        try {
            const res = await fetch('http://localhost:5024/api/Submissions/submit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`,
                },
                body: JSON.stringify({ ProblemId: problemId, Code: code, Language: language }),
                cache: 'no-store'
            });

            const result = await res.json();
            if (!res.ok) throw new Error(result.message || 'Lỗi không rõ'); 

            location.href = `result.html?submissionId=${result.submissionId}`;
        } catch (err) {
            alert('Lỗi: ' + err.message);
        }
    };
});
