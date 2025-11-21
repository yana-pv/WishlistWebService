class Validators {
    static validateWishlist(title, description) {
        const errors = [];

        if (!title || title.trim().length < 2) {
            errors.push('Название должно содержать минимум 2 символа');
        }

        if (title && title.length > 100) {
            errors.push('Название не должно превышать 100 символов');
        }

        if (description && description.length > 500) {
            errors.push('Описание не должно превышать 500 символов');
        }

        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    static validateItem(title, price, desireLevel) {
        const errors = [];

        if (!title || title.trim().length < 2) {
            errors.push('Название товара должно содержать минимум 2 символа');
        }

        if (title && title.length > 100) {
            errors.push('Название товара не должно превышать 100 символов');
        }

        if (price !== null && price !== undefined) {
            if (price < 0) {
                errors.push('Цена не может быть отрицательной');
            }
            if (price > 9999999.99) {
                errors.push('Цена слишком большая');
            }
        }

        if (desireLevel < 1 || desireLevel > 3) {
            errors.push('Уровень желания должен быть от 1 до 3');
        }

        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    static validateUrl(url) {
        if (!url) return false;

        try {
            const urlObj = new URL(url);
            return urlObj.protocol === 'http:' || urlObj.protocol === 'https:';
        }
        catch {
            return false;
        }
    }

    static validateImageFile(file) {
        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp', 'image/bmp'];
        const maxSize = 5 * 1024 * 1024; 

        if (!file) return { isValid: false, error: 'Файл не выбран' };

        if (!allowedTypes.includes(file.type)) {
            return { isValid: false, error: 'Недопустимый формат изображения' };
        }

        if (file.size > maxSize) {
            return { isValid: false, error: 'Размер файла не должен превышать 5MB' };
        }

        return { isValid: true, error: null };
    }

    static validateProfile(username, email) {
        const errors = [];

        if (!username || username.trim().length < 3) {
            errors.push('Имя пользователя должно содержать минимум 3 символа');
        }

        if (username && username.length > 20) {
            errors.push('Имя пользователя не должно превышать 20 символов');
        }

        if (!email || !this.isValidEmail(email)) {
            errors.push('Введите корректный email адрес');
        }

        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    static isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }
}

// Функции для отображения ошибок в формах
function showFormErrors(formId, errors) {
    clearFormErrors(formId);

    errors.forEach(error => {
        let fieldId = '';

        if (error.includes('Название')) {
            fieldId = formId === 'wishlist' ? 'wishlist-title' : 'item-title';
        }
        else if (error.includes('Описание')) {
            fieldId = 'wishlist-description';
        }
        else if (error.includes('Цена')) {
            fieldId = 'item-price';
        }
        else if (error.includes('Уровень желания')) {
            fieldId = 'desire-level';
        }
        else if (error.includes('Тема')) {
            fieldId = 'wishlist-theme';
        }
        else if (error.includes('Имя пользователя')) {
            fieldId = 'edit-username';
        }
        else if (error.includes('email')) {
            fieldId = 'edit-email';
        }

        if (fieldId) {
            const errorElement = document.getElementById(fieldId + '-error');
            if (errorElement) {
                errorElement.textContent = error;
                errorElement.classList.add('show');

                const inputElement = document.getElementById(fieldId);
                if (inputElement) {
                    inputElement.classList.add('error');
                }
            }
        }
    });
}

function clearFormErrors(formId) {
    const errorElements = document.querySelectorAll('.error-message');
    errorElements.forEach(element => {
        if (element.id.includes(formId)) {
            element.classList.remove('show');
            element.textContent = '';
        }
    });

    const inputElements = document.querySelectorAll('input, textarea, select');
    inputElements.forEach(element => {
        if (element.id.includes(formId)) {
            element.classList.remove('error');
        }
    });
}

// Валидация в реальном времени для форм
document.addEventListener('DOMContentLoaded', function () {
    // Валидация вишлиста
    const wishlistForm = document.getElementById('wishlist-form');
    if (wishlistForm) {
        const titleInput = document.getElementById('wishlist-title');
        const descriptionInput = document.getElementById('wishlist-description');

        if (titleInput) {
            titleInput.addEventListener('blur', validateWishlistForm);
        }
        if (descriptionInput) {
            descriptionInput.addEventListener('blur', validateWishlistForm);
        }
    }

    // Валидация подарка
    const itemForm = document.getElementById('item-form');
    if (itemForm) {
        const titleInput = document.getElementById('item-title');
        const priceInput = document.getElementById('item-price');

        if (titleInput) {
            titleInput.addEventListener('blur', validateItemForm);
        }
        if (priceInput) {
            priceInput.addEventListener('blur', validateItemForm);
        }
    }

    // Валидация профиля
    const profileForm = document.getElementById('edit-profile-form');
    if (profileForm) {
        const usernameInput = document.getElementById('edit-username');
        const emailInput = document.getElementById('edit-email');

        if (usernameInput) {
            usernameInput.addEventListener('blur', validateProfileForm);
        }
        if (emailInput) {
            emailInput.addEventListener('blur', validateProfileForm);
        }
    }
});

function validateWishlistForm() {
    const title = document.getElementById('wishlist-title')?.value;
    const description = document.getElementById('wishlist-description')?.value;

    if (!title) return false;

    const validation = Validators.validateWishlist(title, description);

    if (!validation.isValid) {
        showFormErrors('wishlist', validation.errors);
    }
    else {
        clearFormErrors('wishlist');
    }

    return validation.isValid;
}

function validateItemForm() {
    const title = document.getElementById('item-title')?.value;
    const price = document.getElementById('item-price')?.value ? parseFloat(document.getElementById('item-price').value) : null;
    const desireLevel = parseInt(document.querySelector('.heart.active')?.dataset.level || '1');

    if (!title) return false;

    const validation = Validators.validateItem(title, price, desireLevel);

    if (!validation.isValid) {
        showFormErrors('item', validation.errors);
    }
    else {
        clearFormErrors('item');
    }

    return validation.isValid;
}

function validateProfileForm() {
    const username = document.getElementById('edit-username')?.value;
    const email = document.getElementById('edit-email')?.value;

    if (!username || !email) return false;

    const validation = Validators.validateProfile(username, email);

    if (!validation.isValid) {
        showFormErrors('edit-profile', validation.errors);
    } else {
        clearFormErrors('edit-profile');
    }

    return validation.isValid;
}

// Валидация URL для ссылок
function validateLinkUrl(url) {
    if (!url) {
        return { isValid: false, error: 'URL обязателен' };
    }

    if (!Validators.validateUrl(url)) {
        return { isValid: false, error: 'Введите корректный URL (http:// или https://)' };
    }

    return { isValid: true, error: null };
}