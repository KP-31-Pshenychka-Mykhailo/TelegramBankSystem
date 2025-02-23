const API_URL = "http://localhost:5268";

async function login() {
    const id = document.getElementById("login-id").value;
    const password = document.getElementById("login-password").value;

    const response = await fetch(`${API_URL}/user/signIn`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ NewUserId: id, OldUserId: id, UserPassword: password })
    });

    if (response.status === 200) {
        localStorage.setItem("userId", id); // Сохраняем ID
        window.location.href = "dashboard.html";
    } else {
        alert("Ошибка входа! Проверьте данные.");
    }
}
