// Управление вишлистами друзей
document.addEventListener('DOMContentLoaded', function () {
    // Загрузка вишлистов друзей при переходе на вкладку
    document.querySelector('[data-page="friend-wishlists"]').addEventListener('click', () => {
        app.loadFriendWishlists();
    });
});

// Методы для работы с вишлистами друзей
WishListerApp.prototype.loadFriendWishlists = async function () {
    try {
        const response = await fetch('/api/friend-wishlists', {
            credentials: 'include'
        });

        if (response.ok) {
            const data = await response.json();
            this.renderFriendWishlists(data.wishlists);
        } else {
            console.error('Failed to load friend wishlists');
        }
    } catch (error) {
        console.error('Error loading friend wishlists:', error);
    }
};

WishListerApp.prototype.renderFriendWishlists = function (wishlists) {
    console.log('🔍 ALL FRIEND WISHLISTS DATA:', wishlists);

    const container = document.getElementById('friend-wishlists-container');

    if (!wishlists || wishlists.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <h3>У вас пока нет вишлистов друзей</h3>
                <p>Перейдите по ссылке друга и нажмите "Запомнить вишлист", чтобы добавить его сюда</p>
            </div>
        `;
        return;
    }

    container.innerHTML = wishlists.map(fw => {
        // Используем eventDate из данных вишлиста
        const dateDisplay = fw.eventDate ?
            '📅 ' + new Date(fw.eventDate).toLocaleDateString('ru-RU') :
            '🗓️ Без даты события';

        return `
        <div class="wishlist-card wishlist-theme-${fw.theme.id}" data-wishlist-id="${fw.wishlistId}">
            <h3 class="wishlist-card-title">${this.escapeHtml(fw.title)}</h3> 
            <p>${fw.description || 'Без описания'}</p> 
            <div class="wishlist-meta">
                <span>От: ${this.escapeHtml(fw.friendName)}</span>
                <span>${dateDisplay}</span>
            </div>
            <div class="wishlist-actions">
                <button class="btn btn-primary theme-button-${fw.theme.id}" onclick="app.viewFriendWishlist(${fw.id})">
                    Посмотреть подарки
                </button>
                <button class="btn btn-text" onclick="app.removeFriendWishlist(${fw.id})">
                    Удалить
                </button>
            </div>
        </div>
        `;
    }).join('');
};

WishListerApp.prototype.renderFriendWishlistPage = function (result) {
    this.currentFriendWishlist = result;

    const wishlist = result.wishlist;

    // Обновляем заголовок страницы
    const titleElement = document.getElementById('friend-wishlist-page-title');
    titleElement.textContent = wishlist.title;
    // --- НОВОЕ: Применяем цвет темы к заголовку ---
    titleElement.style.color = wishlist.theme.color;
    // --- /НОВОЕ ---

    // Применяем тему к заголовку
    const header = document.getElementById('friend-wishlist-page-header');
    header.className = `section-header wishlist-theme-${wishlist.theme.id}`;

    // Назначаем обработчики кнопок
    document.getElementById('save-friend-wishlist-btn').onclick = () => this.saveFriendWishlist(result);

    // Назначаем классы кнопкам
    document.getElementById('save-friend-wishlist-btn').className = `btn btn-primary theme-button-${wishlist.theme.id}`;

    // Рендерим подарки
    this.renderFriendWishlistPageItems(wishlist.items || []);
};

WishListerApp.prototype.viewFriendWishlist = async function (friendWishlistId) {
    try {
        const response = await fetch(`/api/friend-wishlists/${friendWishlistId}`, {
            credentials: 'include'
        });

        if (response.ok) {
            const data = await response.json();
            const result = data.wishlist;

            console.log('🔍 Friend wishlist full data:', result);
            console.log('🔍 Description in full data:', result.description);

            // Устанавливаем текущий вишлист друга
            this.currentFriendWishlist = result;

            // Обновляем заголовок страницы
            const titleElement = document.getElementById('friend-wishlist-page-title');
            titleElement.textContent = result.title;

            // Применяем цвет темы к названию
            if (result.theme?.color) {
                titleElement.style.color = result.theme.color;
            }

            // ОБНОВЛЯЕМ ОПИСАНИЕ - КРИТИЧЕСКИ ВАЖНО!
            const descriptionElement = document.getElementById('friend-wishlist-page-description');
            console.log('🔍 Description element found:', !!descriptionElement);

            if (descriptionElement) {
                if (result.description && result.description.trim() !== '') {
                    console.log('🔍 Setting description text:', result.description);
                    descriptionElement.textContent = result.description;
                    descriptionElement.style.display = 'block';
                    // Описание тоже в цвете темы
                    if (result.theme?.color) {
                        descriptionElement.style.color = result.theme.color;
                        descriptionElement.style.opacity = '0.8';
                    }
                } else {
                    console.log('🔍 Hiding description - empty or null');
                    descriptionElement.style.display = 'none';
                }
            }

            // Применяем стили как в моих вишлистах
            this.applyFriendWishlistTheme(result);

            // Рендерим подарки
            this.renderFriendWishlistPageItems(result.items || []);

            // Показываем вкладку
            this.showPage('app-page');
            this.showAppSection('friend-wishlist-page');
        } else {
            this.showNotification('Ошибка загрузки вишлиста друга', 'error');
        }
    } catch (error) {
        console.error('Error loading friend wishlist:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

// Добавьте этот метод в конец файла friends.js
WishListerApp.prototype.viewFriendItemDetails = async function (itemId) {
    try {
        // Находим подарок в текущем вишлисте друга
        const item = this.currentFriendWishlist.items.find(i => i.id === itemId);

        if (item) {
            this.renderFriendItemDetailsModal(item);
            this.showModal('item-details-modal');
        } else {
            this.showNotification('Подарок не найден', 'error');
        }
    } catch (error) {
        console.error('Error loading friend item details:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

WishListerApp.prototype.renderFriendItemDetailsModal = function (item) {
    const container = document.getElementById('item-details-content');

    // Получаем ID темы текущего вишлиста друга
    const themeId = this.currentFriendWishlist?.theme?.id || 1;

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
                ${item.isReserved ? `
                    <div class="reservation-status">
                        ${item.reservedByUserId === this.currentUser.id ?
                '✅ Забронировано вами' :
                '⏳ Забронировано другим пользователем'
            }
                    </div>
                ` : ''}
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
        <div class="item-detail-actions-bottom">
            <!-- Для вишлистов друга кнопка "Редактировать" не показывается -->
        </div>
    `;
};


WishListerApp.prototype.reserveItem = async function (itemId) {
    try {
        console.log('Reserving item:', itemId); // Отладка

        const response = await fetch(`/api/items/${itemId}/reserve`, {
            method: 'POST',
            credentials: 'include'
        });

        console.log('Reserve response status:', response.status); // Отладка

        if (response.ok) {
            this.showNotification('Подарок забронирован!', 'success');

            // Обновляем просмотр вишлиста друга
            if (this.currentFriendWishlist) {
                // Используем ID из хэша URL вместо this.currentFriendWishlist.id
                const hash = window.location.hash;
                if (hash.startsWith('#friend-wishlist/')) {
                    const friendWishlistId = parseInt(hash.split('/')[1]);
                    if (friendWishlistId) {
                        await this.viewFriendWishlist(friendWishlistId);
                    }
                }
            }

            // Обновляем статистику профиля
            await this.loadProfileStats();
        } else {
            const data = await response.json();
            console.log('Reserve error:', data); // Отладка
            this.showNotification(data.message || 'Ошибка бронирования', 'error');
        }
    } catch (error) {
        console.error('Error reserving item:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

WishListerApp.prototype.unreserveItem = async function (itemId) {
    const confirmed = await this.confirmAction(
        'Отмена бронирования',
        'Вы уверены, что хотите отменить бронирование этого подарка?'
    );

    if (!confirmed) return;

    try {
        const response = await fetch(`/api/items/${itemId}/unreserve`, {
            method: 'POST',
            credentials: 'include'
        });

        if (response.ok) {
            this.showNotification('Бронирование отменено', 'success');

            // Обновляем просмотр вишлиста друга
            if (this.currentFriendWishlist) {
                // Используем ID из хэша URL
                const hash = window.location.hash;
                if (hash.startsWith('#friend-wishlist/')) {
                    const friendWishlistId = parseInt(hash.split('/')[1]);
                    if (friendWishlistId) {
                        await this.viewFriendWishlist(friendWishlistId);
                    }
                }
            }

            // Обновляем статистику профиля
            await this.loadProfileStats();
        } else {
            const data = await response.json();
            this.showNotification(data.message || 'Ошибка отмены бронирования', 'error');
        }
    } catch (error) {
        console.error('Error unreserving item:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

WishListerApp.prototype.removeFriendWishlist = async function (friendWishlistId) {
    const confirmed = await this.confirmAction(
        'Удаление вишлиста друга',
        'Вы уверены, что хотите удалить этот вишлист из списка?'
    );

    if (!confirmed) return;

    try {
        const response = await fetch(`/api/friend-wishlists/${friendWishlistId}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (response.ok) {
            this.showNotification('Вишлист друга удален', 'success');
            await this.loadFriendWishlists();
            this.closeModal('friend-wishlist-view-modal');
        } else {
            this.showNotification('Ошибка удаления вишлиста друга', 'error');
        }
    } catch (error) {
        console.error('Error deleting friend wishlist:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

// Добавьте этот метод в конец файла friends.js
WishListerApp.prototype.applyFriendWishlistTheme = function (wishlist) {
    const pageElement = document.getElementById('friend-wishlist-page-page');
    const headerElement = document.getElementById('friend-wishlist-page-header');
    const itemsContainer = document.getElementById('friend-wishlist-page-items-container');

    if (pageElement && headerElement && itemsContainer) {
        // Применяем тему к заголовку
        headerElement.className = `section-header wishlist-theme-${wishlist.theme.id}`;

        // Создаем белый контейнер для подарков
        itemsContainer.style.background = 'white';
        itemsContainer.style.borderRadius = '16px';
        itemsContainer.style.padding = '2rem';
        itemsContainer.style.boxShadow = '0 4px 6px rgba(0, 0, 0, 0.05)';
        itemsContainer.style.marginTop = '1rem';
        itemsContainer.style.minHeight = '400px';
    }
};