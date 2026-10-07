const downloadButton = document.getElementById('downloadJson');
const previewButton = document.getElementById('viewJson');
const message = document.getElementById('exportMessage');

async function exportUsers(preview) {
    downloadButton.disabled = true;
    previewButton.disabled = true;
    message.hidden = true;
    document.getElementById('jsonPreview').hidden = true;
    try {
        const response = await fetch('/user/all');
        if (!response.ok) throw new Error('Не удалось загрузить данные. Попробуйте ещё раз.');
        const json = JSON.stringify(await response.json(), null, 2);
        if (preview) {
            document.getElementById('jsonContent').textContent = json;
            document.getElementById('jsonPreview').hidden = false;
        } else {
            const url = URL.createObjectURL(new Blob([json], { type: 'application/json;charset=utf-8' }));
            const link = document.createElement('a');
            link.href = url;
            link.download = 'users_' + new Date().toISOString().slice(0, 10) + '.json';
            document.body.append(link);
            link.click();
            link.remove();
            setTimeout(() => URL.revokeObjectURL(url), 1000);
            message.textContent = 'Файл передан браузеру для скачивания.';
            message.className = 'message success';
            message.hidden = false;
        }
    } catch (error) {
        message.textContent = error.message;
        message.className = 'message error';
        message.hidden = false;
    } finally {
        downloadButton.disabled = false;
        previewButton.disabled = false;
    }
}
downloadButton.addEventListener('click', () => exportUsers(false));
previewButton.addEventListener('click', () => exportUsers(true));
