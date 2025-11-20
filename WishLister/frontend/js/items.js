// Обработчики для модального окна подарка
document.addEventListener('DOMContentLoaded', function () {
    const itemForm = document.getElementById('item-form');
    if (itemForm) {
        itemForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (typeof app !== 'undefined' && app) {
                await app.saveItem();
            }
        });
    }

    // Уровень желания (сердечки)
    document.querySelectorAll('.heart').forEach(heart => {
        heart.addEventListener('click', function () {
            const level = parseInt(this.dataset.level);
            if (typeof app !== 'undefined' && app) {
                app.setDesireLevel(level);
            }
        });
    });

    // Загрузка изображения
    const imagePreview = document.getElementById('image-preview');
    const imageInput = document.getElementById('item-image');

    if (imagePreview && imageInput) {
        imagePreview.addEventListener('click', () => imageInput.click());
        imageInput.addEventListener('change', (e) => {
            if (typeof app !== 'undefined' && app) {
                app.handleImageUpload(e);
            }
        });

        const removeImageBtn = document.getElementById('remove-image-btn');
        if (removeImageBtn) {
            removeImageBtn.addEventListener('click', () => {
                if (typeof app !== 'undefined' && app) {
                    app.removeImage();
                }
            });
        }
    }

    // Генерация AI ссылок
    const itemTitleInput = document.getElementById('item-title');
    if (itemTitleInput) {
        let timeout;
        itemTitleInput.addEventListener('input', function () {
            clearTimeout(timeout);
            timeout = setTimeout(() => {
                if (this.value.length > 2 && typeof app !== 'undefined' && app) {
                    app.generateAILinks(this.value);
                }
            }, 1000);
        });
    }

    // Добавление ручных ссылок
    const addManualLinkBtn = document.getElementById('add-manual-link');
    if (addManualLinkBtn) {
        addManualLinkBtn.addEventListener('click', () => {
            if (typeof app !== 'undefined' && app) {
                app.addManualLink();
            }
        });
    }
});


// Методы для работы с подарками
WishListerApp.prototype.showCreateItemModal = function () {
    if (!this.currentWishlist) return;

    document.getElementById('item-modal-title').textContent = 'Добавить подарок';
    document.getElementById('item-form').reset();
    this.setDesireLevel(1);
    this.removeImage();
    this.clearLinks();
    clearFormErrors('item');

    this.currentItem = null;
    this.showModal('item-modal');
};

WishListerApp.prototype.editItem = async function (itemId) {
    try {
        const response = await fetch(`/api/items/${itemId}`, {
            credentials: 'include'
        });

        if (response.ok) {
            const data = await response.json();
            const item = data.item;

            document.getElementById('item-modal-title').textContent = 'Редактировать подарок';
            document.getElementById('item-title').value = item.title;
            document.getElementById('item-description').value = item.description || '';
            document.getElementById('item-price').value = item.price || '';
            document.getElementById('item-comment').value = item.comment || '';

            this.setDesireLevel(item.desireLevel);

            // --- ИЗМЕНЕНО: Загрузка изображения ---
            if (item.imageUrl) {
                this.loadImage(item.imageUrl); // <-- Устанавливает preview и this.currentImageData
            } else {
                this.removeImage(); // <-- Убирает preview и this.currentImageData
            }
            // --- /ИЗМЕНЕНО ---

            // Загрузка ссылок
            this.loadItemLinks(item.links || []);

            this.currentItem = item;
            this.showModal('item-modal');
        }
    } catch (error) {
        console.error('Error loading item for edit:', error);
        this.showNotification('Ошибка загрузки подарка', 'error');
    }
};

WishListerApp.prototype.saveItem = async function () {
    if (!validateItemForm()) return;

    if (!this.currentWishlist) {
        console.log('No current wishlist');
        return;
    }

    const activeHearts = document.querySelectorAll('.heart.active[data-level]');
    let desireLevel = 1; // По умолчанию

    if (activeHearts.length > 0) {
        const levels = Array.from(activeHearts).map(heart => parseInt(heart.dataset.level));
        desireLevel = Math.max(...levels);
    } else {
        const firstHeart = document.querySelector('.heart[data-level]');
        if (firstHeart) {
            desireLevel = parseInt(firstHeart.dataset.level);
        }
    }


    const formData = {
        title: document.getElementById('item-title').value.trim(),
        description: document.getElementById('item-description').value.trim() || null,
        price: document.getElementById('item-price').value ?
            parseFloat(document.getElementById('item-price').value) : null,
        comment: document.getElementById('item-comment').value.trim() || null,
        desireLevel: desireLevel, // <-- Теперь передаётся правильно
        wishlistId: this.currentWishlist.id,
        links: this.getLinksData()
    };

    const imageData = this.getImageData();
    if (imageData) {
        formData.imageData = imageData;
    }

    try {
        const url = this.currentItem ?
            `/api/items/${this.currentItem.id}` :
            '/api/items';

        const method = this.currentItem ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData),
            credentials: 'include'
        });

        const data = await response.json();

        if (response.ok) {
            this.showNotification(
                this.currentItem ? 'Подарок обновлен' : 'Подарок добавлен',
                'success'
            );
            this.closeModal('item-modal');
            this.currentItem = null;

            // Обновляем вишлист
            if (this.currentWishlist) {
                if (typeof app !== 'undefined' && app) {
                    // Обновляем список вишлистов
                    await app.loadMyWishlists();

                    // ОБНОВЛЯЕМ ТЕКУЩИЙ ВИШЛИСТ ЕСЛИ МЫ НА ЕГО СТРАНИЦЕ
                    await app.refreshCurrentWishlist();
                }
            }

        } else {
            this.showNotification(data.message || 'Ошибка сохранения', 'error');
        }
    } catch (error) {
        console.error('Error saving item:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

WishListerApp.prototype.deleteItem = async function (itemId) {
    const confirmed = await this.confirmAction(
        'Удаление подарка',
        'Вы уверены, что хотите удалить этот подарок?'
    );

    if (!confirmed) return;

    try {
        const response = await fetch(`/api/items/${itemId}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (response.ok) {
            this.showNotification('Подарок удален', 'success');

            // Обновляем вишлист
            if (this.currentWishlist) {
                if (typeof app !== 'undefined' && app) {
                    // Обновляем список вишлистов
                    await app.loadMyWishlists();

                    // ОБНОВЛЯЕМ ТЕКУЩИЙ ВИШЛИСТ ЕСЛИ МЫ НА ЕГО СТРАНИЦЕ
                    await app.refreshCurrentWishlist();
                }
            }
        } else {
            this.showNotification('Ошибка удаления подарка', 'error');
        }
    } catch (error) {
        console.error('Error deleting item:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

// Работа с изображениями
WishListerApp.prototype.handleImageUpload = function (event) {
    const file = event.target.files[0];
    if (!file) return;

    const validation = Validators.validateImageFile(file);
    if (!validation.isValid) {
        this.showNotification(validation.error, 'error');
        return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
        // --- ДОБАВЛЕНО: Устанавливаем currentImageData ---
        this.currentImageData = e.target.result;
        // --- /ДОБАВЛЕНО ---
        this.loadImage(e.target.result);
    };
    reader.readAsDataURL(file);
};

WishListerApp.prototype.loadImage = function (imageData) {
    const preview = document.getElementById('image-preview');
    const removeBtn = document.getElementById('remove-image-btn');

    preview.innerHTML = `<img src="${imageData}" alt="Preview">`;
    removeBtn.style.display = 'block';

    // --- ДОБАВЛЕНО: Устанавливаем currentImageData ---
    this.currentImageData = imageData;
    // --- /ДОБАВЛЕНО ---
};

WishListerApp.prototype.removeImage = function () {
    const preview = document.getElementById('image-preview');
    const removeBtn = document.getElementById('remove-image-btn');
    const fileInput = document.getElementById('item-image');

    preview.innerHTML = '<span>+ Добавить изображение</span>';
    removeBtn.style.display = 'none';
    fileInput.value = '';

    // --- ДОБАВЛЕНО: Обнуляем currentImageData ---
    this.currentImageData = null;
    // --- /ДОБАВЛЕНО ---
};

WishListerApp.prototype.getImageData = function () {
    return this.currentImageData;
};

// Уровень желания
WishListerApp.prototype.setDesireLevel = function (level) {
    console.log('Setting desire level:', level); // <-- ДОБАВЬ ЛОГ

    const hearts = document.querySelectorAll('.heart');
    const desireText = document.getElementById('desire-text');

    hearts.forEach((heart, index) => {
        if (index < level) {
            heart.classList.add('active');
        } else {
            heart.classList.remove('active');
        }
    });

    const texts = {
        1: 'Немного хочу',
        2: 'Очень хочу',
        3: 'Мечтаю об этом!'
    };

    desireText.textContent = texts[level] || 'Немного хочу';
};

// Работа со ссылками
WishListerApp.prototype.generateAILinks = async function (itemTitle) {
    try {
        const encodedTitle = encodeURIComponent(itemTitle);
        const response = await fetch(`/api/links/ai/${encodedTitle}`);

        if (response.ok) {
            const data = await response.json();
            this.renderAILinks(data.links);
        }
    } catch (error) {
        console.error('Error generating AI links:', error);
    }
};

WishListerApp.prototype.renderAILinks = function (links) {
    const container = document.getElementById('ai-links');

    if (!links || links.length === 0) {
        container.innerHTML = '<p class="no-links">Нет предложенных ссылок</p>';
        return;
    }

    container.innerHTML = `
        <h4>Предложенные ссылки:</h4>
        ${links.map(link => `
            <div class="smart-link-item">
                <span class="link-text">${this.escapeHtml(link.title || 'Без названия')}</span>
                <button type="button" class="btn btn-outline find-btn" 
                        onclick="window.open('${link.url}', '_blank');">
                    Найти
                </button>
            </div>
        `).join('')}
    `;
};

WishListerApp.prototype.addManualLink = function () {
    const urlInput = document.getElementById('manual-link-url');
    const url = urlInput.value.trim();

    if (!url) {
        this.showNotification('Введите URL', 'error');
        return;
    }

    const validation = validateLinkUrl(url);
    if (!validation.isValid) {
        this.showNotification(validation.error, 'error');
        return;
    }

    this.addLinkToList(url, '', null, false); // <-- Убран текст 'Ручная ссылка'
    urlInput.value = '';
};

WishListerApp.prototype.addLinkToList = function (url, title, price, isFromAI) {
    const container = document.getElementById('manual-links-list');
    const linkId = 'link-' + Date.now();

    this.linkUrls.set(linkId, url); 

    const linkHtml = `
        <div class="manual-link-item" id="${linkId}">
            <button type="button" class="btn btn-text remove-link-btn" onclick="app.removeLink('${linkId}')">×</button>
            <a href="${this.escapeHtml(url)}" target="_blank" class="manual-link-url">
                ${this.escapeHtml(url)} <!-- <-- Показываем только URL -->
            </a>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', linkHtml);

    return linkId; 
};

WishListerApp.prototype.selectLink = function (url) {
    // Помечаем выбранную ссылку
    document.querySelectorAll('.link-item').forEach(item => {
        item.classList.remove('selected');
    });

    // --- ИСПРАВЛЕНО: Находим элемент по ID из Map ---
    let linkId = null;
    for (const [id, storedUrl] of this.linkUrls.entries()) {
        if (storedUrl === url) {
            linkId = id;
            break;
        }
    }

    if (linkId) {
        const selectedItem = document.getElementById(linkId);
        if (selectedItem) {
            selectedItem.classList.add('selected');
        }
    }
};

WishListerApp.prototype.removeLink = function (linkId) {
    const linkElement = document.getElementById(linkId);
    if (linkElement) {
        linkElement.remove();
    }
};

WishListerApp.prototype.clearLinks = function () {
    document.getElementById('manual-links-list').innerHTML = '';
};

WishListerApp.prototype.loadItemLinks = function (links) {
    this.clearLinks();

    links.forEach(link => {
        // --- ИСПРАВЛЕНО: Получаем ID при добавлении ---
        const linkId = this.addLinkToList(
            link.url,
            link.title || 'Без названия',
            link.price,
            link.isFromAI
        );

        if (link.isSelected && linkId) {
            this.selectLinkById(linkId); // <-- Вызываем метод из app.js
        }
    });
};

WishListerApp.prototype.selectLinkById = function (linkId) {
    // Помечаем выбранную ссылку
    document.querySelectorAll('.manual-link-item').forEach(item => {
        item.classList.remove('selected');
    });

    const selectedItem = document.getElementById(linkId);
    if (selectedItem) {
        selectedItem.classList.add('selected');
    }
};

WishListerApp.prototype.getLinksData = function () {
    const links = [];

    // Только ручные ссылки
    document.querySelectorAll('#manual-links-list .manual-link-item').forEach(item => {
        const linkId = item.id;
        const url = this.linkUrls.get(linkId); 

        if (url) { 
            const linkUrlElement = item.querySelector('.manual-link-url');
            const linkTitle = linkUrlElement ? linkUrlElement.textContent : url; 
            const isSelected = item.classList.contains('selected');

            links.push({
                url: url,
                title: linkTitle, 
                price: null,
                isFromAI: false,
                isSelected: isSelected
            });
        }
    });

    return links;
};