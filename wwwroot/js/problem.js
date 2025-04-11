const loadMonacoEditor = () =>
    new Promise(resolve => require(['vs/editor/editor.main'], resolve));

let editor, editorLoaded = false;

const initEditorIfNeeded = async () => {
    if (editorLoaded) return;
    const container = document.getElementById('editor-container');
    if (!container) return;

    await loadMonacoEditor();
    editor = monaco.editor.create(container, {
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
    const token = localStorage.getItem('token');
    const problemId = +new URLSearchParams(location.search).get("problemId");
    if (!problemId) return alert("Không tìm thấy problemId trên URL!");

    const editorContainer = document.getElementById('editor-container');
    const submitForm = document.getElementById('submit-form');
    const languageSelect = document.getElementById('language');

    loadProblem(problemId, token);
    setupEditorLazyLoad(editorContainer);
    setupSubmission(submitForm, token, problemId, languageSelect);
});

async function loadProblem(problemId, token) {
    try {
        const res = await fetch(`http://localhost:5024/api/problems/${problemId}`, {
            headers: { 'Authorization': `Bearer ${token}` },
            cache: 'no-store'
        });

        if (!res.ok) throw new Error();

        const { title, description, inputFormat, outputFormat, inputSample, outputSample } = await res.json();

        document.getElementById("title").textContent = title;
        document.getElementById("description").textContent = description;
        document.getElementById("inputfomat").textContent = inputFormat;
        document.getElementById("outputfomat").textContent = outputFormat;
        document.getElementById("inputSample").textContent = inputSample;
        document.getElementById("outputSample").textContent = outputSample;

    } catch (err) {
        console.error("Lỗi tải đề bài:", err);
        alert("Không thể tải đề bài. Vui lòng thử lại!");
    }
}

function setupEditorLazyLoad(editorContainer) {
    const scrollHandler = () => {
        if (isInViewport(editorContainer)) {
            initEditorIfNeeded();
            window.removeEventListener('scroll', scrollHandler);
        }
    };
    window.addEventListener('scroll', scrollHandler, { passive: true });
    scrollHandler(); // gọi ngay nếu đã hiển thị
}

function setupSubmission(form, token, problemId, languageSelect) {
    form.onsubmit = e => {
        e.preventDefault();
        if (!editorLoaded) return alert("Trình soạn thảo chưa sẵn sàng!");

        const code = editor.getValue().trim();
        if (!code) return alert("Vui lòng nhập mã!");

        const language = languageSelect.value;

        localStorage.setItem('code', code);
        localStorage.setItem('language', language);
        localStorage.setItem('problemId', problemId);
        const urlParams = new URLSearchParams(window.location.search);
        //const problemId = urlParams.get("problemId");
        console.log(urlParams);
        const contestId = urlParams.get("contestId");
        
        
        location.href = `result.html?problemId=` + problemId +"&contestId=" +contestId ;
    };
}
