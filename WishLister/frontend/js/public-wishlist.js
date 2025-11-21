class PublicWishlistApp {
    constructor() {
        this.init();
    }

    init() {
        this.loadPublicWishlist();
        this.setupPublicEvents();
    }

    async loadPublicWishlist() {
        const shareToken = this.getShareToken();
        if (!shareToken) return;

        try {
            const response = await fetch(`/api/public/wishlists/${shareToken}`);

            if (response.ok) {
                const data = await response.json();
                this.renderPublicWishlist(data.wishlist);
            } else {
                console.error('Error response:', response.status);
                this.showError('Вишлист не найден');
            }
        } catch (error) {
            console.error('Error loading public wishlist:', error);
            this.showError('Ошибка загрузки вишлиста');
        }
    }

    renderPublicWishlist(wishlist) {
        const container = document.getElementById('public-wishlist-container');
        if (!container) return;

        container.innerHTML = '';

        const isOwner = wishlist.isOwner;

        let eventDateDisplay = '';
        if (wishlist.eventDate) {
            const eventDate = new Date(wishlist.eventDate);
            eventDateDisplay = `📅 ${eventDate.toLocaleDateString('ru-RU')}`;
        }

        container.innerHTML = `
            <div class="public-wishlist-header" id="public-wishlist-header">
                <div class="wishlist-header-main">
                    <h1 id="public-wishlist-title">${this.escapeHtml(wishlist.title)}</h1>
                    <div class="wishlist-header-bottom">
                        <div class="wishlist-info">
                            ${wishlist.description && wishlist.description.trim() !== '' ?
                        `<p id="public-wishlist-description" class="wishlist-description">${this.escapeHtml(wishlist.description)}</p>` :
                        ''
                    }
                            ${eventDateDisplay ?
                        `<div class="wishlist-event-date">${eventDateDisplay}</div>` :
                        ''
                    }
                        </div>
                        <div class="wishlist-page-actions">
                            <a href="/" class="btn btn-outline">← На главную</a>
                        </div>
                    </div>
                </div>
            </div>

            <div class="public-actions" id="public-wishlist-actions" style="display: none;">
                <!-- Будет заполнено ниже -->
            </div>

            <div id="public-wishlist-items" class="items-grid">
                <!-- Подарки будут здесь -->
            </div>

            <!-- Модальное окно для просмотра подарка -->
            <div id="public-item-modal" class="modal">
                <div class="modal-content wide-modal">
                    <div class="modal-header">
                        <h3 id="public-item-modal-title">Детали подарка</h3>
                        <button class="close-modal" onclick="publicWishlistApp.closePublicItemModal()">&times;</button>
                    </div>
                    <div id="public-item-modal-content">
                        <!-- Сюда будет загружаться информация о подарке -->
                    </div>
                </div>
            </div>
        `;

        const headerElement = document.getElementById('public-wishlist-header');
        headerElement.className = `public-wishlist-header wishlist-theme-${wishlist.theme.id}`;

        const backButton = document.querySelector('#public-wishlist-header .btn-outline');
        if (backButton && wishlist.theme?.id) {
            backButton.className = `btn btn-outline theme-button-${wishlist.theme.id}`;
        }

        const actionsElement = document.getElementById('public-wishlist-actions');
        if (!isOwner) {
            actionsElement.innerHTML = `
                <button id="save-friend-wishlist" class="btn btn-primary theme-button-${wishlist.theme.id}">
                    💾 Запомнить вишлист друга
                </button>
                <p class="action-hint">Войдите или зарегистрируйтесь, чтобы сохранить этот вишлист и бронировать подарки</p>
            `;
            actionsElement.style.display = 'block';
        } else {
            actionsElement.innerHTML = `
                <p class="owner-notice">👋 Это ваш вишлист!</p>
                <p class="action-hint">Поделитесь этой ссылкой с друзьями, чтобы они могли посмотреть ваш вишлист</p>
            `;
            actionsElement.style.display = 'block';
        }

        this.currentPublicWishlist = wishlist;

        const itemsContainer = document.getElementById('public-wishlist-items');
        if (wishlist.items && wishlist.items.length > 0) {
            itemsContainer.innerHTML = wishlist.items.map(item => `
                <div class="item-card"> <!-- ИСПРАВЛЕНО: убран класс reserved -->
                    ${item.imageUrl ? `
                        <img src="${item.imageUrl}" alt="${this.escapeHtml(item.title)}" class="item-image"
                             onerror="this.style.display='none'">
                    ` : ''}
                    <h3 class="item-title">${this.escapeHtml(item.title)}</h3>
                    ${item.price ? `<div class="item-price">${item.price.toLocaleString('ru-RU')} ₽</div>` : ''}
                    ${item.description ? `<p class="item-description">${this.escapeHtml(item.description)}</p>` : ''}
                    <div class="desire-level">
                        ${'💖'.repeat(item.desireLevel)}${'🤍'.repeat(3 - item.desireLevel)}
                    </div>
                    <div class="item-meta">
                        <div class="item-actions">
                            <button class="btn btn-outline theme-button-${wishlist.theme.id}" 
                                    onclick="publicWishlistApp.viewPublicItem(${item.id})">
                                Посмотреть
                            </button>
                        </div>
                    </div>
                </div>
            `).join('');

            itemsContainer.className = 'items-grid public-items-container';
        }
        else {
            itemsContainer.innerHTML = `
                <div class="empty-state">
                    <h3>В этом вишлисте пока нет подарков</h3>
                    ${isOwner ? '<p>Добавьте первый подарок в своем аккаунте</p>' : '<p>Ваш друг ещё не добавил желаемые подарки</p>'}
                </div>
            `;
            itemsContainer.className = 'empty-items-container';
        }
    }

    getShareToken() {
        const path = window.location.pathname;
        const parts = path.split('/');
        return parts[parts.length - 1];
    }

    setupPublicEvents() {
        document.addEventListener('click', (e) => {
            if (e.target.id === 'save-friend-wishlist') {
                this.saveFriendWishlist();
            }
        });
    }

    async saveFriendWishlist() {
        const shareToken = this.getShareToken();
        const friendName = prompt('Как зовут вашего друга?', 'Друг');

        if (!friendName) return;

        try {
            const response = await fetch('/api/friend-wishlists/save-from-url', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    shareToken: shareToken,
                    friendName: friendName.trim()
                }),
                credentials: 'include'
            });

            if (response.ok) {
                alert('Вишлист друга сохранен! Теперь вы можете бронировать подарки.');
                window.location.href = '/';
            }
            else if (response.status === 401) {
                alert('Пожалуйста, войдите или зарегистрируйтесь, чтобы сохранить вишлист друга.');
                window.location.href = '/#login';
            }
            else {
                const data = await response.json();
                alert(data.message || 'Ошибка сохранения вишлиста');
            }
        }
        catch (error) {
            console.error('Error saving friend wishlist:', error);
            alert('Ошибка соединения');
        }
    }

    showError(message) {
        const container = document.getElementById('public-wishlist-container');
        if (container) {
            container.innerHTML = `
                <div class="error-state">
                    <h3>${message}</h3>
                    <p>Возможно, ссылка устарела или вишлист был удален</p>
                    <a href="/" class="btn btn-primary">Вернуться на главную</a>
                </div>
            `;
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    viewPublicItem(itemId) {
        const item = this.currentPublicWishlist.items.find(i => i.id === itemId);

        if (item) {
            this.renderPublicItemModal(item);
            this.showPublicModal();
        }
    }

    renderPublicItemModal(item) {
        const container = document.getElementById('public-item-modal-content');
        const isOwner = this.currentPublicWishlist.isOwner;

        container.innerHTML = `
            <div class="item-details-layout">
                <div class="item-details-left">
                    ${item.imageUrl ? `
                        <img src="${item.imageUrl}" alt="${this.escapeHtml(item.title)}" class="item-detail-image" 
                             onerror="this.style.display='none'">
                    ` : ''}
                    <h3 class="item-detail-title">${this.escapeHtml(item.title)}</h3>
                    ${item.price ? `<div class="item-price">${item.price.toLocaleString('ru-RU')} ₽</div>` : ''}
                    <div class="desire-level">
                        ${'💖'.repeat(item.desireLevel)}${'🤍'.repeat(3 - item.desireLevel)}
                    </div>
                    <!-- УБРАНО: статус бронирования для публичного просмотра -->
                </div>
                <div class="item-details-right">
                    ${item.description ? `<p class="item-description"><strong>Описание:</strong> ${this.escapeHtml(item.description)}</p>` : ''}
                    ${item.comment ? `<p class="item-comment"><strong>Комментарий:</strong> ${this.escapeHtml(item.comment)}</p>` : ''}
                    ${item.links && item.links.length > 0 ? `
                        <div class="item-links">
                            <h4>Ссылки на товары:</h4>
                            ${item.links.map(link => `
                                <div class="link-item">
                                    <a href="${link.url}" target="_blank" rel="noopener noreferrer">
                                        ${this.escapeHtml(link.title || link.url)}
                                    </a>
                                    ${link.price ? `<span class="link-price">${link.price.toLocaleString('ru-RU')} ₽</span>` : ''}
                                </div>
                            `).join('')}
                        </div>
                    ` : ''}
                </div>
            </div>
        `;

        document.getElementById('public-item-modal-title').textContent = this.escapeHtml(item.title);
    }

    showPublicModal() {
        document.getElementById('public-item-modal').classList.add('active');
    }

    closePublicItemModal() {
        document.getElementById('public-item-modal').classList.remove('active');
    }

    setupPublicEvents() {
        document.addEventListener('click', (e) => {
            if (e.target.id === 'save-friend-wishlist') {
                this.saveFriendWishlist();
            }

            // Закрытие модального окна при клике на фон
            if (e.target.id === 'public-item-modal') {
                this.closePublicItemModal();
            }
        });

        // Закрытие модального окна по клавише Escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closePublicItemModal();
            }
        });
    }
}

let publicWishlistApp;

document.addEventListener('DOMContentLoaded', () => {
    publicWishlistApp = new PublicWishlistApp();
});