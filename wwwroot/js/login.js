const BASE_URL = "http://localhost:5024/api/auth";

const decodeJwt = (token) => {
    try {
        const payload = token.split('.')[1];
        return JSON.parse(atob(payload));
    } catch (e) {
        console.error("JWT decode error:", e);
        return {};
    }
};

async function register() {
    const username = document.getElementById('regUsername').value;
    const email = document.getElementById('regEmail').value;
    const password = document.getElementById('regPassword').value;

    try {
        const res = await fetch(`${BASE_URL}/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password, roleIds: [2] })
        });

        const text = await res.text();
        //alert(text);
    } catch (err) {
        console.error("Register error:", err);
        alert("Đăng ký thất bại!");
    }
}

async function login() {
    const username = document.getElementById('loginUsername').value;
    const password = document.getElementById('loginPassword').value;

    try {
        const res = await fetch(`${BASE_URL}/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (!res.ok) {
            alert("Login failed! Please check your credentials.");
            return;
        }

        const  result  = await res.json();
        localStorage.setItem("token", result.token);
        console.log(result.userId);
        localStorage.setItem("userId", result.userId);
        alert(result.userId);
        // console.log("Token:", token);

        const payload = decodeJwt(result.token);
        const roleKey = Object.keys(payload).find(k => k.toLowerCase().includes("role"));
        const roles = payload[roleKey];
        console.log(roles);
        console.log(Array.isArray(roles));
        Array.isArray(roles) ? roles.includes("Admin") : roles == "Admin";
        if (Array.isArray(roles) ? roles.includes("Admin") : roles == "Admin") {
            console.log("Admin")
            window.location.href = "/admin.html";
        } else {
            console.log("user")
            window.location.href = "/user.html";
        }
    } catch (err) {
        console.error("Login error:", err);
        alert("Lỗi đăng nhập!");
    }
}
