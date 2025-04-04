async function register() {
    let response = await fetch('http://localhost:5024/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            username: document.getElementById('regUsername').value,
            email: document.getElementById('regEmail').value,
            password: document.getElementById('regPassword').value,
            roleIds: [2] // Giả sử user mặc định có role ID 2
        })
    });
    alert(await response.text());
}

async function login() {
    let response = await fetch('http://localhost:5024/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            username: document.getElementById('loginUsername').value,
            password: document.getElementById('loginPassword').value
        })
    });

    if (!response.ok) {
        alert("Login failed! Please check your credentials.");
        return;
    }

    let result = await response.json();
    localStorage.setItem("token", result.token);
    alert(result.token)
    console.log(result.token)
    // Giải mã JWT để lấy thông tin Role ID
    let payload = JSON.parse(atob(result.token.split('.')[1]));
    //let roles = payload["role"];
    let roles = Object.keys(payload).find(key => key.includes("role")) ? payload[Object.keys(payload).find(key => key.includes("role"))] : null;

    console.log(roles)
    if (Array.isArray(roles)) {
        
        if (roles.includes("Admin")) {
            window.location.href = "/admin.html";
        } else {
            window.location.href = "/user.html";
        }
    } else {
        if (roles === "Admin") {
            window.location.href = "/admin.html";
        } else {
            window.location.href = "/user.html";
        }
    }
}