//var editor = CodeMirror.fromTextArea(document.getElementById('code'), {
//    mode: "c++", // Chỉnh ngôn ngữ cho editor (ví dụ Python)
//    lineNumbers: true,
//    theme: "monokai", // Theme đẹp cho editor
//    tabSize: 4,
//    indentWithTabs: true
//});
//document.getElementById("code").addEventListener("submit", function () {
//    document.getElementById("code").value = editor.getValue(); // Cập nhật giá trị vào textarea
//});


console.log(document.querySelector('#login-form'))
console.log(document.getElementById('login-form'))
//document.querySelector('#login-form').addEventListener('submit', async (event) => {
//    event.preventDefault();

//    const username = document.getElementById('username').value;
//    const password = document.getElementById('password').value;

//    const response = await fetch('http://localhost:5024/api/User/Login', {
//        method: 'P
//            'Content-Type': 'application/json',
//        },
//        body: JSON.stringify({ username, password }),
//    });

//    const result = await response.json();
//    if (response.ok) {
//        alert('Login successful!');
//        window.location.href = 'index.html';
//    } else {
//        alert('Login failed: ' + result.message);
//    }
//});
//document.addEventListener('DOMContentLoaded', () => {
//    const loginForm = document.querySelector('#login-form');
//    if (loginForm) {
//        loginForm.addEventListener('submit', async (event) => {
//            event.preventDefault();

//            const username = document.getElementById('username').value;
//            const password = document.getElementById('password').value;

//            const response = await fetch('http://localhost:5024/api/User/Login', {
//                method: 'POST',
//                headers: { 'Content-Type': 'application/json' },
//                body: JSON.stringify({
//                    username,
//                    password // Rename `password` to `passwordHash`
//                }),
//            });

//            const result = await response.json();
//            if (response.ok) {
//                alert('Login successful!');
//                window.location.href = 'index.html';
//            } else {
//                alert('Login failed: ' + result.message);
//            }
//        });
//    } else {
//        console.log('Login form not found.');
//    }
//});
//document.getElementById('language').addEventListener('change', function () {
//    const language = this.value;
//    let mode = "text/x-c++src"; // Mặc định là C++

//    if (language === "python") mode = "text/x-python";
//    else if (language === "java") mode = "text/x-java";

//    editor.setOption("mode", mode);
//});
document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('login-form');
    if (loginForm) {
        loginForm.addEventListener('submit', async (event) => {
            event.preventDefault();

            const username = document.getElementById('username').value;
            const password = document.getElementById('password').value;

            try {
                const response = await fetch('http://localhost:5024/api/User/Login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username, password }),
                });

                const result = await response.json();
                if (response.ok) {
                    alert('Login successful!');
                    window.location.href = 'index.html';
                } else {
                    alert('Login failed: ' + (result.message || 'Unknown error'));
                }
            } catch (error) {
                console.error('Error:', error);
                alert('An error occurred. Please try again.');
            }
        });
    } else {
        console.log('Login form not found.');
    }
});




document.addEventListener('DOMContentLoaded', async () => {
    const problemSelect = document.getElementById('problem');
    const submitForm = document.getElementById('submit-form');
   
    try {
        // Gọi API backend để lấy danh sách bài toán
        const response = await fetch('http://localhost:5024/api/problems');  // Đảm bảo endpoint đúng
        if (!response.ok) throw new Error('Failed to fetch problems');

        const problems = await response.json();

        // Xóa tất cả các option cũ (nếu có)
        problemSelect.innerHTML = '';

        // Thêm các bài toán vào dropdown
        problems.forEach(problem => {
            const option = document.createElement('option');
            option.value = problem.id;  // ID của bài toán
            option.textContent = "bài " + problem.id + ": " + problem.title;  // Tiêu đề bài toán
            problemSelect.appendChild(option);
        });

    } catch (error) {
        console.error('Error loading problems:', error);
        problemSelect.innerHTML = '<option value="">Error loading problems</option>';
    }
});
