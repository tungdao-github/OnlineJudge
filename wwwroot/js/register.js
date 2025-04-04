console.log("register-form  :" + document.getElementById('register-form'))
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
            const response = await fetch('http://localhost:5024/api/User/Register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ username, passwordHash: password, email }),
            });

            const result = await response.json();
            if (response.ok) {
                alert('Registration successful!');
                window.location.href = 'login.html';
            } else {
                alert('Registration failed: ' + result.message);
            }
        } catch (error) {
            console.error('Error:', error);
            alert('An error occurred. Please try again.');
        }
    });
});


//document.getElementById('register-form').addEventListener('submit', async (event) => {
//    event.preventDefault();

//    const username = document.getElementById('username').value;
//    const password = document.getElementById('password').value;
//    const email = document.getElementById('email').value;

//    const response = await fetch('http://localhost:5024/api/User/Register', {
//        method: 'POST',
//        headers: {
//            'Content-Type': 'application/json',
//        },
//        body: JSON.stringify({ username, passwordHash: password, email }),
//    });

//    const result = await response.json();
//    if (response.ok) {
//        alert('Registration successful!');
//        window.location.href = 'login.html';
//    } else {
//        alert('Registration failed: ' + result.message);
//    }
//});