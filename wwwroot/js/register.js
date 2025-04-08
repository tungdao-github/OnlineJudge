document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('register-form').addEventListener('submit', async (event) => {
        event.preventDefault();

        const username = document.getElementById('username').value;
        const password = document.getElementById('password').value;
        const email = document.getElementById('email').value;

        if (!username || !password || !email) {
            alert('Please fill in all fields.');
            return;
        }

        try {
            const response = await fetch('http://localhost:5024/api/Auth/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    username,
                    password,
                    email,
                    roleIds: [2]  // ✅ là mảng
                }),
            });

            const contentType = response.headers.get("Content-Type");
            let result = contentType.includes("application/json")
                ? await response.json()
                : await response.text();

            if (response.ok) {
                alert('Registration successful!');
                window.location.href = 'login.html';
            } else {
                alert('Registration failed: ' + (result.message || result));
            }
        } catch (error) {
            console.error('Error:', error);
            alert('An error occurred. Please try again.');
        }
    });
});
