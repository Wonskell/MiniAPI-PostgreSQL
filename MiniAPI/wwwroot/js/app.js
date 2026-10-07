const byId = id => document.getElementById(id);
let listRequest = 0;
let statisticsRequest = 0;

async function request(url, options) {
    const response = await fetch(url, options);
    if (!response.ok) {
        const body = await response.json().catch(() => ({}));
        throw new Error(body.error || 'Не удалось выполнить запрос. Попробуйте ещё раз.');
    }
    return response.status === 204 ? null : response.json();
}

function showMessage(text, isError = false) {
    const message = byId('message');
    message.textContent = text;
    message.className = isError ? 'message error' : 'message success';
    message.hidden = false;
}

function addCell(row, text) {
    const cell = document.createElement('td');
    cell.textContent = text;
    row.append(cell);
    return cell;
}

function renderUsers(users) {
    const body = byId('usersList');
    body.replaceChildren();
    for (const user of users) {
        const row = document.createElement('tr');
        const nameCell = addCell(row, user.name);
        const id = document.createElement('span');
        id.className = 'user-id';
        id.textContent = user.id;
        nameCell.append(id);
        addCell(row, user.age);
        addCell(row, user.departmentName || 'Без отдела');
        const action = addCell(row, '');
        const button = document.createElement('button');
        button.className = 'btn btn-danger';
        button.textContent = 'Удалить';
        button.setAttribute('aria-label', 'Удалить пользователя ' + user.name);
        button.addEventListener('click', async () => {
            if (!window.confirm('Удалить пользователя «' + user.name + '»?')) return;
            button.disabled = true;
            try {
                await request('/user/' + encodeURIComponent(user.id), { method: 'DELETE' });
                showMessage('Пользователь удалён.');
                await Promise.all([loadUsers(), loadStatistics()]);
            } catch (error) { showMessage(error.message, true); }
            finally { button.disabled = false; }
        });
        action.append(button);
        body.append(row);
    }
}

async function loadUsers() {
    const requestId = ++listRequest;
    const status = byId('listStatus');
    const name = byId('searchName').value.trim();
    const min = byId('minAge').value === '' ? 0 : Number(byId('minAge').value);
    const max = byId('maxAge').value === '' ? 150 : Number(byId('maxAge').value);
    if (min > max) { status.textContent = 'Возраст «от» не должен превышать возраст «до».'; return; }
    status.textContent = 'Загрузка…';
    try {
        let users;
        if (name) {
            const response = await fetch('/user/search?' + new URLSearchParams({ name }));
            if (response.status === 404) users = [];
            else if (response.ok) users = [await response.json()];
            else throw new Error('Не удалось выполнить поиск.');
            users = users.filter(user => user.age >= min && user.age <= max);
        } else if (min !== 0 || max !== 150) {
            users = await request('/user/filter?' + new URLSearchParams({ minAge: min, maxAge: max }));
        } else {
            users = await request(byId('sort').value === 'age' ? '/user/sorted' : '/user/all');
        }
        if (requestId !== listRequest) return;
        users.sort(byId('sort').value === 'age'
            ? (a, b) => a.age - b.age || a.name.localeCompare(b.name)
            : (a, b) => new Date(b.createdAt) - new Date(a.createdAt) || a.id.localeCompare(b.id));
        renderUsers(users);
        status.textContent = users.length ? 'Найдено записей: ' + users.length : 'Пользователей не найдено.';
    } catch (error) {
        if (requestId !== listRequest) return;
        byId('usersList').replaceChildren();
        status.textContent = error.message;
    }
}

async function loadStatistics() {
    const requestId = ++statisticsRequest;
    try {
        const [summary, departments] = await Promise.all([request('/user/stats'), request('/departments/stats')]);
        if (requestId !== statisticsRequest) return;
        byId('summary').textContent = 'Всего: ' + summary.totalUsers + ' · Средний возраст: ' + summary.averageAge;
        byId('departmentStats').replaceChildren();
        for (const department of departments) {
            const row = document.createElement('tr');
            addCell(row, department.name);
            addCell(row, department.userCount);
            addCell(row, department.averageAge ?? '—');
            byId('departmentStats').append(row);
        }
    } catch {
        if (requestId !== statisticsRequest) return;
        byId('summary').textContent = 'Не удалось загрузить статистику.';
        byId('departmentStats').replaceChildren();
    }
}

async function loadDepartments() {
    try {
        const departments = await request('/departments');
        const select = byId('department');
        const previous = select.value;
        select.replaceChildren(new Option('Без отдела', ''));
        for (const department of departments) select.add(new Option(department.name, department.id));
        if ([...select.options].some(option => option.value === previous)) select.value = previous;
        select.disabled = false;
        byId('departmentHint').textContent = '';
    } catch {
        byId('department').disabled = true;
        byId('departmentHint').textContent = 'Отделы не загрузились. Нажмите «Обновить», чтобы повторить.';
    }
}

byId('userForm').addEventListener('submit', async event => {
    event.preventDefault();
    const button = byId('addUser');
    const name = byId('name').value.trim();
    if (!name) { showMessage('Введите имя пользователя.', true); return; }
    button.disabled = true;
    try {
        const user = await request('/user', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, age: Number(byId('age').value), departmentId: byId('department').value ? Number(byId('department').value) : null })
        });
        byId('userForm').reset();
        byId('filterForm').reset();
        showMessage('Пользователь «' + user.name + '» добавлен.');
        await Promise.all([loadUsers(), loadStatistics()]);
    } catch (error) { showMessage(error.message, true); }
    finally { button.disabled = false; }
});
byId('filterForm').addEventListener('submit', event => { event.preventDefault(); loadUsers(); });
byId('resetFilters').addEventListener('click', () => { byId('filterForm').reset(); loadUsers(); });
byId('refresh').addEventListener('click', () => { loadUsers(); loadStatistics(); loadDepartments(); });
loadUsers();
loadStatistics();
loadDepartments();
