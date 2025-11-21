class WishListerApp {
    constructor() {
        this.currentUser = null;
        this.currentWishlist = null;
        this.currentItem = null;
        this.themes = [];
        this.linkUrls = new Map();
        this.init();
    }

    async init() {
        const isMainPage = window.location.pathname === '/' || window.location.pathname === '/index.html';

        if (isMainPage)
        {
            const isAuthenticated = await this.checkAuth();

            if (isAuthenticated) {
                this.showPage('app-page');
                await this.loadMyWishlists();
                this.showAppSection('my-wishlists'); 
            }
            else {
                this.showPage('landing-page');
            }
        }
        else {
            await this.checkAuth();
        }

        await this.loadThemes();
        this.setupEventListeners();
        this.handleUrlNavigation();

        window.addEventListener('hashchange', () => {
            this.handleUrlNavigation();
        });
    }

    async checkAuth() {
        try {
            const response = await fetch('/api/auth/check', {
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();

                this.currentUser = {
                    id: data.userId,
                    username: data.username,
                    avatarUrl: data.avatarUrl || null 
                };

                this.updateUserInterface();
                this.loadUserAvatar();

                if (this.currentUser) {
                    this.showPage('app-page');
                    await this.loadMyWishlists();
                    return true;
                }
            } else {
                this.currentUser = null;
            }
        } catch (error) {
            console.error('Auth check failed:', error);
            this.currentUser = null;
        }
        return false;
    }

    async loadUserAvatar() {
        try {
            const response = await fetch('/api/user/profile', {
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
                if (data.user.avatarUrl) {
                    this.currentUser.avatarUrl = data.user.avatarUrl;
                    this.updateUserInterface();
                }
            }
        } catch (error) {
            console.error('Error loading user avatar:', error);
        }
    }

    handleUrlNavigation() {
        const hash = window.location.hash.substring(1);

        if (hash === 'login') {
            this.showPage('login-page');
        }
        else if (hash === 'register') {
            this.showPage('register-page');
        }
        else if (hash === 'wishlists' && this.currentUser) {
            this.showPage('app-page');
            this.showAppSection('my-wishlists');
        }
        else if (hash === 'friends' && this.currentUser) {
            this.showPage('app-page');
            this.showAppSection('friend-wishlists');
        }
        else if (hash === 'profile' && this.currentUser) {
            this.showPage('app-page');
            this.showAppSection('profile');
        }
        else if (hash.startsWith('wishlist/')) {
            const wishlistId = parseInt(hash.split('/')[1]);
            if (wishlistId && this.currentUser) {
                this.showWishlistPage(wishlistId);
            }
        }
        else if (hash.startsWith('friend-wishlist/')) { 
            const friendWishlistId = parseInt(hash.split('/')[1]);
            if (friendWishlistId && this.currentUser) {
                this.showFriendWishlistPage(friendWishlistId);
            }
        }
        else if (!hash && this.currentUser) {
            this.showPage('app-page');
            this.showAppSection('my-wishlists');
        }
    };

    async loadThemes() {
        try {
            const response = await fetch('/api/themes');
            if (response.ok) {
                const data = await response.json();
                this.themes = data.themes || [];
            }
        } catch (error) {
            console.error('Failed to load themes:', error);
        }
    }

    async loadMyWishlists() {
        try {
            const response = await fetch('/api/wishlists', {
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
                this.renderMyWishlists(data.wishlists);
            } else {
                console.error('Failed to load wishlists');
            }
        } catch (error) {
            console.error('Error loading wishlists:', error);
        }
    }

    renderMyWishlists(wishlists) {
        const container = document.getElementById('my-wishlists-container');

        if (!wishlists || wishlists.length === 0) {
            container.innerHTML = `
            <div class="empty-state">
                <h3>У вас пока нет вишлистов</h3>
                <p>Создайте первый вишлист и начните добавлять желаемые подарки!</p>
                <button class="btn btn-primary" onclick="app.showCreateWishlistModal()">
                    Создать первый вишлист
                </button>
            </div>
        `;
            return;
        }

        container.innerHTML = wishlists.map(wishlist => `
        <div class="wishlist-card wishlist-theme-${wishlist.theme.id}" data-wishlist-id="${wishlist.id}">
            <h3 class="wishlist-card-title">${this.escapeHtml(wishlist.title)}</h3>
            <p>${wishlist.description || 'Без описания'}</p>
            <div class="wishlist-meta">
                <span>${wishlist.itemCount || 0} подарков</span>
                <span>${wishlist.eventDate ?
                '📅 ' + new Date(wishlist.eventDate).toLocaleDateString('ru-RU') :
                '🗓️ ' + new Date(wishlist.createdAt).toLocaleDateString('ru-RU')}</span>
            </div>
            <div class="wishlist-actions">
                <!-- ИЗМЕНЕНО: Кнопка открывает страницу -->
                <button class="btn btn-outline theme-button-${wishlist.theme.id}" 
                        onclick="window.location.hash = 'wishlist/${wishlist.id}'">
                    Посмотреть
                </button>
                <button class="btn btn-text" onclick="app.deleteWishlist(${wishlist.id})">
                    Удалить
                </button>
            </div>
        </div>
    `).join('');
    }

    setupEventListeners() {
        document.getElementById('login-btn').addEventListener('click', () => this.showPage('login-page'));
        document.getElementById('register-btn').addEventListener('click', () => this.showPage('register-page'));
        document.getElementById('go-to-register').addEventListener('click', () => this.showPage('register-page'));
        document.getElementById('go-to-login').addEventListener('click', () => this.showPage('login-page'));
        document.getElementById('back-to-landing-from-login').addEventListener('click', () => this.showPage('landing-page'));
        document.getElementById('back-to-landing-from-register').addEventListener('click', () => this.showPage('landing-page'));

        document.querySelectorAll('.nav-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const page = e.target.dataset.page;
                this.showAppSection(page);
            });
        });

        document.getElementById('logout-btn').addEventListener('click', () => this.logout());

        this.setupModalListeners();

        document.getElementById('create-wishlist-btn').addEventListener('click', () => {
            this.showCreateWishlistModal();
        });

        const wishlistForm = document.getElementById('wishlist-form');
        if (wishlistForm) {
            wishlistForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                await this.saveWishlist();
            });
        }

        document.querySelectorAll('.theme-option').forEach(option => {
            option.addEventListener('click', function () {
                document.querySelectorAll('.theme-option').forEach(opt => {
                    opt.classList.remove('selected');
                });
                this.classList.add('selected');
            });
        });
    }

    showPage(pageId) {
        document.querySelectorAll('.page').forEach(page => {
            page.classList.remove('active');
        });
        document.getElementById(pageId).classList.add('active');
    }

    showAppSection(sectionId) {
        document.querySelectorAll('.app-section').forEach(section => {
            section.classList.remove('active');
        });

        document.querySelectorAll('.nav-btn').forEach(btn => {
            btn.classList.remove('active');
        });

        const section = document.getElementById(`${sectionId}-page`);
        if (section) {
            section.classList.add('active');
        }

        const navBtn = document.querySelector(`[data-page="${sectionId}"]`);
        if (navBtn) {
            navBtn.classList.add('active');
        }

        if (sectionId !== 'wishlist-page' && sectionId !== 'friend-wishlist-page') { 
            const wishlistPage = document.getElementById('wishlist-page-page');
            if (wishlistPage) {
                wishlistPage.className = 'app-section'; 
            }

            const friendWishlistPage = document.getElementById('friend-wishlist-page-page'); 
            if (friendWishlistPage) {
                friendWishlistPage.className = 'app-section'; 
            }
        }

        if (sectionId === 'friend-wishlists') {
            this.loadFriendWishlists();
        } else if (sectionId === 'profile') {
            this.loadProfile();
            this.loadProfileStats();
        }
    };

    updateUserInterface() {
        if (this.currentUser) {
            document.getElementById('user-greeting').textContent = `Привет, ${this.currentUser.username}`;

            const avatarElement = document.getElementById('user-avatar');
            if (this.currentUser.avatarUrl) {
                avatarElement.src = this.currentUser.avatarUrl;
                avatarElement.style.display = 'block';

                avatarElement.onerror = () => {
                    avatarElement.style.display = 'none';
                    avatarElement.src = '';
                };

            } else {
                avatarElement.style.display = 'none';
                avatarElement.src = '';
            }
        }
    }

    showNotification(message, type = 'info') {
        const notification = document.getElementById('notification');
        notification.textContent = message;
        notification.className = `notification ${type} show`;

        setTimeout(() => {
            notification.classList.remove('show');
        }, 3000);
    }

    async logout() {
        try {
            await fetch('/api/auth/logout', {
                method: 'POST',
                credentials: 'include'
            });
            this.currentUser = null;
            this.showPage('landing-page');
            this.showNotification('Вы успешно вышли из системы', 'success');
        } catch (error) {
            console.error('Logout failed:', error);
        }
    }

    goToMyWishlists() {
        // Убираем текущий вишлист
        this.currentWishlist = null;

        // Меняем хэш
        window.location.hash = 'wishlists';

        // Явно показываем вкладку
        this.showAppSection('my-wishlists');
    };

    goToFriends() {
        // Убираем текущий вишлист друга
        this.currentFriendWishlist = null;

        // Меняем хэш
        window.location.hash = 'friends';

        // Явно показываем вкладку
        this.showAppSection('friend-wishlists');
    };

    setupModalListeners() {
        // Закрытие модальных окон
        document.querySelectorAll('.modal').forEach(modal => {
            modal.addEventListener('click', (e) => {
                if (e.target === modal) {
                    this.closeModal(modal.id);
                }
            });
        });

        // Закрытие по кнопкам
        document.getElementById('close-wishlist-modal').addEventListener('click', () => this.closeModal('wishlist-modal'));
        document.getElementById('close-item-modal').addEventListener('click', () => this.closeModal('item-modal'));
        document.getElementById('close-edit-profile-modal').addEventListener('click', () => this.closeModal('edit-profile-modal'));
    }

    showModal(modalId) {
        document.getElementById(modalId).classList.add('active');
    }

    closeModal(modalId) {
        document.getElementById(modalId).classList.remove('active');
    }

    confirmAction(title, message) {
        return new Promise((resolve) => {
            document.getElementById('confirm-title').textContent = title;
            document.getElementById('confirm-message').textContent = message;

            const confirmModal = document.getElementById('confirm-modal');
            confirmModal.classList.add('active');

            document.getElementById('cancel-confirm').onclick = () => {
                confirmModal.classList.remove('active');
                resolve(false);
            };

            document.getElementById('confirm-action').onclick = () => {
                confirmModal.classList.remove('active');
                resolve(true);
            };
        });
    }

    // Методы для вишлистов
    showCreateWishlistModal() {
        document.getElementById('wishlist-modal-title').textContent = 'Создать вишлист';
        document.getElementById('wishlist-form').reset();
        document.querySelectorAll('.theme-option').forEach(opt => opt.classList.remove('selected'));
        document.querySelector('.theme-option').classList.add('selected');
        clearFormErrors('wishlist');
        this.showModal('wishlist-modal');
    }

    async editWishlist(wishlistId) {
        try {
            const response = await fetch(`/api/wishlists/${wishlistId}`, {
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
                const wishlist = data.wishlist;

                document.getElementById('wishlist-modal-title').textContent = 'Редактировать вишлист';
                document.getElementById('wishlist-title').value = wishlist.title;
                document.getElementById('wishlist-description').value = wishlist.description || '';

                if (wishlist.eventDate) {
                    document.getElementById('wishlist-event-date').value =
                        new Date(wishlist.eventDate).toISOString().split('T')[0];
                }

                document.querySelectorAll('.theme-option').forEach(opt => {
                    opt.classList.remove('selected');
                    if (parseInt(opt.dataset.themeId) === wishlist.theme.id) {
                        opt.classList.add('selected');
                    }
                });

                this.currentWishlist = wishlist;
                this.showModal('wishlist-modal');
            }
        }
        catch (error) {
            console.error('Error loading wishlist for edit:', error);
            this.showNotification('Ошибка загрузки вишлиста', 'error');
        }
    }

    async saveWishlist() {
        if (!validateWishlistForm()) return;

        const formData = {
            title: document.getElementById('wishlist-title').value.trim(),
            description: document.getElementById('wishlist-description').value.trim(),
            eventDate: document.getElementById('wishlist-event-date').value || null,
            themeId: parseInt(document.querySelector('.theme-option.selected').dataset.themeId)
        };

        try {
            const url = this.currentWishlist ?
                `/api/wishlists/${this.currentWishlist.id}` :
                '/api/wishlists';

            const method = this.currentWishlist ? 'PUT' : 'POST';

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
                    this.currentWishlist ? 'Вишлист обновлен' : 'Вишлист создан',
                    'success'
                );
                this.closeModal('wishlist-modal');
                this.currentWishlist = null;
                await this.loadMyWishlists();
            }
            else {
                this.showNotification(data.message || 'Ошибка сохранения', 'error');
            }
        }
        catch (error) {
            console.error('Error saving wishlist:', error);
            this.showNotification('Ошибка соединения', 'error');
        }
    }

    async deleteWishlist(wishlistId) {
        const confirmed = await this.confirmAction(
            'Удаление вишлиста',
            'Вы уверены, что хотите удалить этот вишлист? Все подарки также будут удалены.'
        );

        if (!confirmed) return;

        try {
            const response = await fetch(`/api/wishlists/${wishlistId}`, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (response.ok) {
                this.showNotification('Вишлист удален', 'success');
                await this.loadMyWishlists();
                this.closeModal('wishlist-view-modal');
            }
            else {
                this.showNotification('Ошибка удаления вишлиста', 'error');
            }
        } catch (error) {
            console.error('Error deleting wishlist:', error);
            this.showNotification('Ошибка соединения', 'error');
        }
    }

    async showWishlistPage(wishlistId) {
        try {
            const response = await fetch(`/api/wishlists/${wishlistId}`, {
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
                const wishlist = data.wishlist;
                this.currentWishlist = wishlist;

                document.getElementById('wishlist-page-title').textContent = wishlist.title;

                const titleElement = document.getElementById('wishlist-page-title');
                titleElement.style.color = wishlist.theme.color;

                const descriptionElement = document.getElementById('wishlist-page-description');
                if (descriptionElement) {
                    if (wishlist.description) {
                        descriptionElement.textContent = wishlist.description;
                        descriptionElement.style.display = 'block';
                    }
                    else {
                        descriptionElement.style.display = 'none';
                    }
                }

                const header = document.getElementById('wishlist-page-header');
                header.className = `section-header wishlist-theme-${wishlist.theme.id}`;

                document.getElementById('share-wishlist-page-btn').onclick = () => this.shareWishlist(wishlist);
                document.getElementById('edit-wishlist-page-btn').onclick = () => this.editWishlist(wishlist.id);
                document.getElementById('add-item-page-btn').onclick = () => this.showCreateItemModal();
                document.getElementById('share-wishlist-page-btn').className = `btn btn-outline theme-button-${wishlist.theme.id}`;
                document.getElementById('edit-wishlist-page-btn').className = `btn btn-outline theme-button-${wishlist.theme.id}`;
                document.getElementById('add-item-page-btn').className = `btn btn-primary theme-button-${wishlist.theme.id}`;
                document.getElementById('back-to-wishlists-btn').className = `btn btn-outline theme-button-${wishlist.theme.id}`;

                this.renderWishlistPageItems(wishlist.items || []);

                this.showPage('app-page');
                this.showAppSection('wishlist-page');
            } else {
                this.showNotification('Ошибка загрузки вишлиста', 'error');
            }
        }
        catch (error) {
            console.error('Error loading wishlist:', error);
            this.showNotification('Ошибка соединения', 'error');
        }
    };

    async showFriendWishlistPage(friendWishlistId) {
        try {
            const response = await fetch(`/api/friend-wishlists/${friendWishlistId}`, {
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
                const result = data.wishlist;

                this.currentFriendWishlist = result;

                const titleElement = document.getElementById('friend-wishlist-page-title');
                titleElement.textContent = result.title;

                if (result.theme?.color) {
                    titleElement.style.color = result.theme.color;
                }

                const descriptionElement = document.getElementById('friend-wishlist-page-description');

                if (descriptionElement) {
                    if (result.description && result.description.trim() !== '') {
                        descriptionElement.textContent = result.description;
                        descriptionElement.style.display = 'block';

                        if (result.theme?.color) {
                            descriptionElement.style.color = result.theme.color;
                            descriptionElement.style.opacity = '0.8';
                        }
                    }
                    else {
                        descriptionElement.style.display = 'none';
                    }
                }

                this.applyFriendWishlistTheme(result);
                this.renderFriendWishlistPageItems(result.items || []);

                this.showPage('app-page');
                this.showAppSection('friend-wishlist-page');
            } else {
                this.showNotification('Ошибка загрузки вишлиста друга', 'error');
            }
        }
        catch (error) {
            console.error('Error loading friend wishlist:', error);
            this.showNotification('Ошибка соединения', 'error');
        }
    };
   
    applyFriendWishlistTheme (wishlist) {
        const pageElement = document.getElementById('friend-wishlist-page-page');
        const headerElement = document.getElementById('friend-wishlist-page-header');
        const itemsContainer = document.getElementById('friend-wishlist-page-items-container');

        if (pageElement && headerElement && itemsContainer) {
            headerElement.className = `section-header wishlist-theme-${wishlist.theme.id}`;

            itemsContainer.style.background = 'white';
            itemsContainer.style.borderRadius = '16px';
            itemsContainer.style.padding = '2rem';
            itemsContainer.style.boxShadow = '0 4px 6px rgba(0, 0, 0, 0.05)';
            itemsContainer.style.marginTop = '1rem';
            itemsContainer.style.minHeight = '400px';

            const backButton = document.querySelector('#friend-wishlist-page-header .btn-outline');
            if (backButton) {
                backButton.className = `btn btn-outline theme-button-${wishlist.theme.id}`;
            }
        }
    };

    shareWishlist(wishlist) {
        const shareUrl = `${window.location.origin}/wishlist/${wishlist.shareToken}`;

        navigator.clipboard.writeText(shareUrl).then(() => {
            this.showNotification('Ссылка скопирована в буфер обмена!', 'success');
        }).catch(() => {
            // Fallback для старых браузеров
            const tempInput = document.createElement('input');
            tempInput.value = shareUrl;
            document.body.appendChild(tempInput);
            tempInput.select();
            document.execCommand('copy');
            document.body.removeChild(tempInput);
            this.showNotification('Ссылка скопирована!', 'success');
        });
    };

    renderWishlistPageItems(items) {
        const container = document.getElementById('wishlist-page-items-container');

        if (!items || items.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <h3>В этом вишлисте пока нет подарков</h3>
                    <p>Добавьте первый подарок, нажав кнопку выше</p>
                </div>
            `;
            return;
        }

        const themeId = this.currentWishlist?.theme?.id || 1;

        container.innerHTML = items.map(item => `
            <div class="item-card" data-item-id="${item.id}"> <!-- УБРАНО: ${item.isReserved ? 'reserved' : ''} -->
                ${item.imageUrl ? `
                    <img src="${item.imageUrl}" alt="${this.escapeHtml(item.title)}" class="item-image" 
                         onerror="this.style.display='none'">
                ` : ''}
                <h4 class="item-title">${this.escapeHtml(item.title)}</h4>
                ${item.price ? `<div class="item-price">${item.price.toLocaleString('ru-RU')} ₽</div>` : ''}
                <div class="desire-level">
                    ${'💖'.repeat(item.desireLevel)}${'🤍'.repeat(3 - item.desireLevel)}
                </div>
                <div class="item-meta">
                    <div class="item-actions">
                        <button class="btn btn-outline theme-button-${themeId}" onclick="app.viewItemDetails(${item.id})">
                             Посмотреть
                        </button>
                        <button class="btn btn-danger" onclick="app.deleteItem(${item.id})">
                             Удалить
                        </button>
                    </div>
                </div>
            </div>
        `).join('');
    };

    renderFriendWishlistPageItems = function (items) {
        const container = document.getElementById('friend-wishlist-page-items-container');

        if (!items || items.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <h3>В этом вишлисте пока нет подарков</h3>
                    <p>Ваш друг ещё не добавил желаемые подарки</p>
                </div>
            `;
            return;
        }

        const themeId = this.currentFriendWishlist?.theme?.id || 1;

        const currentUserId = this.currentUser?.id;

        container.innerHTML = items.map(item => {
            const isReservedByMe = item.isReserved && item.reservedByUserId === currentUserId;

            return `
            <div class="item-card ${item.isReserved ? 'reserved' : ''}" data-item-id="${item.id}">
                ${item.imageUrl ? `
                    <img src="${item.imageUrl}" alt="${this.escapeHtml(item.title)}" class="item-image" 
                         onerror="this.style.display='none'">
                ` : ''}
                <h4 class="item-title">${this.escapeHtml(item.title)}</h4>
                ${item.price ? `<div class="item-price">${item.price.toLocaleString('ru-RU')} ₽</div>` : ''}
                <div class="desire-level">
                    ${'💖'.repeat(item.desireLevel)}${'🤍'.repeat(3 - item.desireLevel)}
                </div>
                <div class="item-meta">
                    <div class="item-actions">
                        <!-- Кнопка "Посмотреть" -->
                        <button class="btn btn-outline theme-button-${themeId}" onclick="app.viewFriendItemDetails(${item.id})">
                            Посмотреть
                        </button>
                
                        ${!item.isReserved ?
                        `<button class="btn btn-danger" onclick="app.reserveItem(${item.id})">
                            Забронировать
                        </button>` :
                        isReservedByMe ?
                            `<button class="btn btn-outline btn-outline-danger" onclick="app.unreserveItem(${item.id})">
                            Отменить бронь
                        </button>` :
                            `<button class="btn btn-outline btn-outline-danger" disabled>
                            Уже забронирован
                        </button>`
                    }
                    </div>
                </div>
            </div>
        `}).join('');
    };


    // МЕТОДЫ ДЛЯ РАБОТЫ С ПОДАРКАМИ
    async refreshCurrentWishlist() {
        const hash = window.location.hash;
        if (hash.startsWith('#wishlist/')) {
            const wishlistId = parseInt(hash.split('/')[1]);
            if (wishlistId) {
                await this.showWishlistPage(wishlistId);
            }
        }
    }

    showCreateItemModal() {
        if (!this.currentWishlist) return;

        document.getElementById('item-modal-title').textContent = 'Добавить подарок';
        document.getElementById('item-form').reset();
        this.setDesireLevel(1);
        this.removeImage();
        this.clearLinks();
        clearFormErrors('item');

        this.currentItem = null;
        this.closeModal('wishlist-view-modal');
        this.showModal('item-modal');
    }

    async viewItemDetails(itemId) {
        try {
            const response = await fetch(`/api/items/${itemId}`, {
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
                const item = data.item;

                this.renderItemDetailsModal(item);
                this.showModal('item-details-modal');
            }
            else {
                this.showNotification('Ошибка загрузки подарка', 'error');
            }
        }
        catch (error) {
            console.error('Error loading item details:', error);
            this.showNotification('Ошибка соединения', 'error');
        }
    };

    renderItemDetailsModal(item) {
        const container = document.getElementById('item-details-content');
        const themeId = this.currentWishlist?.theme?.id || 1;

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
                <button class="btn btn-outline theme-button-${themeId}" onclick="app.editItem(${item.id})">
                    Редактировать
                </button>
            </div>
        `;
    };

    // Вспомогательная функция для экранирования HTML
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    };
}

const app = new WishListerApp();

