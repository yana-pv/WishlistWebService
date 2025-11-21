document.addEventListener('DOMContentLoaded', function () {
    document.querySelector('[data-page="profile"]').addEventListener('click', () => {
        app.loadProfile();
    });

    document.getElementById('edit-profile-btn').addEventListener('click', () => {
        app.showEditProfileModal();
    });

    const editProfileForm = document.getElementById('edit-profile-form');
    if (editProfileForm) {
        editProfileForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            await app.saveProfile();
        });
    }

    const avatarUpload = document.getElementById('avatar-upload');
    const changeAvatarBtn = document.getElementById('change-avatar-btn');
    const removeAvatarBtn = document.getElementById('remove-avatar-btn');

    if (changeAvatarBtn && avatarUpload) {
        changeAvatarBtn.addEventListener('click', () => avatarUpload.click());
        avatarUpload.addEventListener('change', (e) => app.handleAvatarUpload(e));
    }

    if (removeAvatarBtn) {
        removeAvatarBtn.addEventListener('click', () => app.removeAvatar());
    }

    document.getElementById('delete-account-btn').addEventListener('click', () => {
        app.deleteAccount();
    });
});

WishListerApp.prototype.loadProfile = async function () {
    try {
        const response = await fetch('/api/user/profile', {
            credentials: 'include'
        });

        if (response.ok) {
            const data = await response.json();
            this.renderProfile(data.user);
            await this.loadProfileStats();

            if (data.user.avatarUrl) {
                this.currentUser.avatarUrl = data.user.avatarUrl;
                this.updateUserInterface();
            }
        }
    } catch (error) {
        console.error('Error loading profile:', error);
    }
};

WishListerApp.prototype.renderProfile = function (user) {
    const avatar = document.getElementById('profile-avatar');
    const username = document.getElementById('profile-username');
    const email = document.getElementById('profile-email');

    if (avatar) {
        avatar.onerror = null;
        avatar.onload = null;

        if (user.avatarUrl) {
            if (user.avatarUrl.startsWith('data:image/')) {
                avatar.src = user.avatarUrl;
                avatar.style.display = 'block';

                avatar.onerror = function () {
                    this.style.display = 'none';
                };

            }
            else {
                avatar.src = user.avatarUrl;
                avatar.style.display = 'block';

                avatar.onerror = function () {
                    console.error('URL avatar failed to load');
                    this.style.display = 'none';
                };
            }
        } else {
            avatar.style.display = 'none';
            avatar.src = '';
        }
    }

    if (username) username.textContent = user.username;
    if (email) email.textContent = user.email;

    this.currentProfile = user;
};

// Метод для проверки доступности изображения
WishListerApp.prototype.testImageLoad = function (url) {
    return new Promise((resolve) => {
        const img = new Image();
        img.onload = () => resolve(true);
        img.onerror = () => resolve(false);
        img.src = url;
    });
};

WishListerApp.prototype.loadProfileStats = async function () {
    try {
        const response = await fetch('/api/user/stats', {
            credentials: 'include'
        });

        if (response.ok) {
            const data = await response.json();
            this.renderProfileStats(data.stats);
        }
    } catch (error) {
        console.error('Error loading profile stats:', error);
    }
};

WishListerApp.prototype.renderProfileStats = function (stats) {
    const statsElement = document.getElementById('profile-stats');

    if (statsElement) {
        statsElement.innerHTML = `
            <div class="stat">
                <span class="stat-number">${stats.WishlistsCount || 0}</span>
                <span class="stat-label">Мои вишлисты</span>
            </div>
            <div class="stat">
                <span class="stat-number">${stats.ItemsCount || 0}</span>
                <span class="stat-label">Мои подарки</span>
            </div>
            <div class="stat">
                <span class="stat-number">${stats.ReservedItemsCount || 0}</span>
                <span class="stat-label">Забронировано</span>
            </div>
        `;
    }
};

WishListerApp.prototype.showEditProfileModal = function () {
    if (!this.currentProfile) return;

    document.getElementById('edit-username').value = this.currentProfile.username;
    document.getElementById('edit-email').value = this.currentProfile.email;

    clearFormErrors('edit-profile');
    this.showModal('edit-profile-modal');
};

WishListerApp.prototype.saveProfile = async function () {
    if (!validateProfileForm()) return;

    const formData = {
        username: document.getElementById('edit-username').value.trim(),
        email: document.getElementById('edit-email').value.trim().toLowerCase(),
        avatarUrl: this.currentProfile.avatarUrl
    };

    try {
        const response = await fetch('/api/user/profile', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData),
            credentials: 'include'
        });

        const data = await response.json();

        if (response.ok) {
            this.showNotification('Профиль обновлен', 'success');
            this.closeModal('edit-profile-modal');
            this.currentProfile = data.user;
            this.renderProfile(data.user);
            this.currentUser.avatarUrl = data.user.avatarUrl;
            this.updateUserInterface();
        }
        else {
            this.showNotification(data.message || 'Ошибка обновления профиля', 'error');
        }
    }
    catch (error) {
        console.error('Error saving profile:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

WishListerApp.prototype.handleAvatarUpload = async function (event) {
    const file = event.target.files[0];
    if (!file) return;

    const validation = Validators.validateImageFile(file);
    if (!validation.isValid) {
        this.showNotification(validation.error, 'error');
        return;
    }

    const reader = new FileReader();
    reader.onload = async (e) => {
        const imageData = e.target.result;

        try {
            const response = await fetch('/api/user/profile', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    username: this.currentProfile.username,
                    email: this.currentProfile.email,
                    avatarUrl: imageData
                }),
                credentials: 'include'
            });

            if (response.ok) {
                const updatedUser = await response.json();
                this.currentProfile = updatedUser.user;
                this.renderProfile(updatedUser.user);
                this.currentUser.avatarUrl = updatedUser.user.avatarUrl;
                this.updateUserInterface();

                this.showNotification('Аватар обновлен', 'success');
            } else {
                console.error('Failed to update profile');
                this.showNotification('Ошибка обновления профиля', 'error');
            }
        }
        catch (error) {
            console.error('Update error:', error);
            this.showNotification('Ошибка соединения', 'error');
        }
    };

    reader.onerror = (error) => {
        console.error('FileReader error:', error);
        this.showNotification('Ошибка чтения файла', 'error');
    };

    reader.readAsDataURL(file);
    event.target.value = '';
};

WishListerApp.prototype.removeAvatar = async function () {
    const confirmed = await this.confirmAction(
        'Удаление аватара',
        'Вы уверены, что хотите удалить аватар?'
    );

    if (!confirmed) return;

    try {
        const response = await fetch('/api/user/profile', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                username: this.currentProfile.username,
                email: this.currentProfile.email,
                avatarUrl: null 
            }),
            credentials: 'include'
        });

        if (response.ok) {
            const data = await response.json();
            this.currentProfile = data.user;
            this.renderProfile(data.user);
            this.showNotification('Аватар удален', 'success');
        } else {
            this.showNotification('Ошибка удаления аватара', 'error');
        }
    } catch (error) {
        console.error('Error removing avatar:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};

WishListerApp.prototype.deleteAccount = async function () {
    const confirmed = await this.confirmAction(
        'Удаление аккаунта',
        'ВНИМАНИЕ: Это действие невозможно отменить! Все ваши вишлисты, подарки и данные будут безвозвратно удалены. Введите ваш пароль для подтверждения:'
    );

    if (!confirmed) return;

    const password = prompt('Введите ваш пароль для подтверждения удаления аккаунта:');
    if (!password) return;

    try {
        const response = await fetch('/api/user/profile', {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                confirmPassword: password
            }),
            credentials: 'include'
        });

        if (response.ok) {
            this.showNotification('Аккаунт успешно удален', 'success');
            setTimeout(() => {
                this.currentUser = null;
                this.showPage('landing-page');
            }, 2000);
        }
        else {
            const data = await response.json();
            this.showNotification(data.message || 'Ошибка удаления аккаунта', 'error');
        }
    } catch (error) {
        console.error('Error deleting account:', error);
        this.showNotification('Ошибка соединения', 'error');
    }
};