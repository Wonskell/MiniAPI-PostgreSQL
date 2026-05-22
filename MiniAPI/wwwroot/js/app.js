const API_BASE = '';

// Форма регистрации
document.getElementById('registrationForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const name = document.getElementById('name').value.trim();
    const age = parseInt(document.getElementById('age').value);
    const messageDiv = document.getElementById('message');
    
    try {
        const response = await fetch(`${API_BASE}/user`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ name, age })
        });
        
        if (response.ok) {
            const data = await response.json();
            messageDiv.className = 'message success';
            messageDiv.textContent = `Пользователь "${data.name}" успешно зарегистрирован!`;
            
            document.getElementById('registrationForm').reset();
            loadUsers();
        } else {
            const error = await response.json();
            messageDiv.className = 'message error';
            messageDiv.textContent = `Ошибка: ${error.error || 'Не удалось зарегистрировать'}`;
        }
    } catch (error) {
        messageDiv.className = 'message error';
        messageDiv.textContent = `Ошибка соединения: ${error.message}`;
    }
    
    setTimeout(() => {
        messageDiv.style.display = 'none';
    }, 5000);
});

// Кнопка загрузки
document.getElementById('loadUsers').addEventListener('click', loadUsers);

async function loadUsers() {
    const usersList = document.getElementById('usersList');
    usersList.innerHTML = '<div class="loading">Загрузка...</div>';
    
    try {
        const response = await fetch(`${API_BASE}/user/all`);
        
        if (response.ok) {
            const userList = await response.json();
            
            if (userList.length === 0) {
                usersList.innerHTML = '<p style="text-align: center; color: #999;">Пользователи не найдены</p>';
                return;
            }
            
            usersList.innerHTML = userList.map(user => `
                <div class="user-card">
                    <h3>${escapeHtml(user.name)}</h3>
                    <p><strong>Возраст:</strong> ${user.age} лет</p>
                    <p class="user-id"><strong>ID:</strong> ${user.id}</p>
                </div>
            `).join('');
        } else {
            usersList.innerHTML = '<p style="text-align: center; color: #dc3545;">Ошибка загрузки</p>';
        }
    } catch (error) {
        usersList.innerHTML = `<p style="text-align: center; color: #dc3545;">Ошибка: ${error.message}</p>`;
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

window.addEventListener('load', loadUsers);
