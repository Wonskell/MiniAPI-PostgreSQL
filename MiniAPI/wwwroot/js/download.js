const API_BASE = '';

// Скачать JSON файл
document.getElementById('downloadJson').addEventListener('click', async () => {
    try {
        const response = await fetch(`${API_BASE}/user/all`);
        
        if (response.ok) {
            const users = await response.json();
            
            // Создаем Blob с JSON данными
            const blob = new Blob([JSON.stringify(users, null, 2)], { type: 'application/json' });
            
            // Создаем ссылку для скачивания
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `users_${new Date().toISOString().split('T')[0]}.json`;
            document.body.appendChild(a);
            a.click();
            
            // Очистка
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
            
            alert('Файл успешно скачан!');
        } else {
            alert('Ошибка при загрузке данных');
        }
    } catch (error) {
        alert(`Ошибка: ${error.message}`);
    }
});

// Просмотреть JSON
document.getElementById('viewJson').addEventListener('click', async () => {
    const preview = document.getElementById('jsonPreview');
    const content = document.getElementById('jsonContent');
    
    try {
        const response = await fetch(`${API_BASE}/user/all`);
        
        if (response.ok) {
            const users = await response.json();
            content.textContent = JSON.stringify(users, null, 2);
            preview.style.display = 'block';
        } else {
            alert('Ошибка при загрузке данных');
        }
    } catch (error) {
        alert(`Ошибка: ${error.message}`);
    }
});
