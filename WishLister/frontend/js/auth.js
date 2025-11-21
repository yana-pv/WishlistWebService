document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('login-form');
    const registerForm = document.getElementById('register-form');

    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            if (!validateLoginForm()) return;

            const formData = {
                login: document.getElementById('login-username').value.trim(),
                password: document.getElementById('login-password').value
            };

            try {
                const response = await fetch('/api/auth/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(formData),
                    credentials: 'include'
                });

                const data = await response.json();

                if (response.ok) {
                    app.showNotification('Вход выполнен успешно!', 'success');

                    // Устанавливаем куку и даем время для её сохранения
                    await new Promise(resolve => setTimeout(resolve, 200));

                    if (typeof app !== 'undefined' && app) {
                        await app.checkAuth();
                        // Небольшая задержка перед перенаправлением
                        setTimeout(() => {
                            window.location.href = '/';
                        }, 300);
                    }
                    else {
                        setTimeout(() => {
                            location.reload();
                        }, 300);
                    }
                }
                else {
                    app.showNotification(data.message || 'Ошибка входа', 'error');
                }
            }
            catch (error) {
                console.error('Login error:', error);
                if (app) {
                    app.showNotification('Ошибка соединения', 'error');
                }
            }
        });
    }

    if (registerForm) {
        registerForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            if (!validateRegisterForm()) return;

            const formData = {
                username: document.getElementById('register-username').value.trim(),
                email: document.getElementById('register-email').value.trim().toLowerCase(),
                login: document.getElementById('register-login').value.trim(),
                password: document.getElementById('register-password').value,
                confirmPassword: document.getElementById('register-confirm-password').value
            };

            try {
                const response = await fetch('/api/auth/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(formData),
                    credentials: 'include'
                });

                const data = await response.json();

                if (response.ok) {
                    app.showNotification('Регистрация успешна!', 'success');

                    // Устанавливаем куку и даем время для её сохранения
                    await new Promise(resolve => setTimeout(resolve, 200));

                    if (typeof app !== 'undefined' && app) {
                        await app.checkAuth();
                        // Небольшая задержка перед перенаправлением
                        setTimeout(() => {
                            window.location.href = '/';
                        }, 300);
                    }
                    else {
                        setTimeout(() => {
                            location.reload();
                        }, 300);
                    }
                }
                else {
                    app.showNotification(data.message || 'Ошибка регистрации', 'error');
                }
            }
            catch (error) {
                console.error('Registration error:', error);
                if (app) {
                    app.showNotification('Ошибка соединения', 'error');
                }
            }
        });
    }
});

function validateLoginForm() {
    let isValid = true;
    const username = document.getElementById('login-username');
    const password = document.getElementById('login-password');

    // Сброс ошибок
    clearErrors('login');

    // Проверка логина
    if (!username.value.trim()) {
        showError('login-username-error', 'Логин обязателен');
        isValid = false;
    }
    else if (username.value.trim().length < 3) {
        showError('login-username-error', 'Логин должен содержать минимум 3 символа');
        isValid = false;
    }

    // Проверка пароля
    if (!password.value) {
        showError('login-password-error', 'Пароль обязателен');
        isValid = false;
    }
    else if (password.value.length < 6) {
        showError('login-password-error', 'Пароль должен содержать минимум 6 символов');
        isValid = false;
    }

    return isValid;
}

function validateRegisterForm() {
    let isValid = true;
    const username = document.getElementById('register-username');
    const email = document.getElementById('register-email');
    const login = document.getElementById('register-login');
    const password = document.getElementById('register-password');
    const confirmPassword = document.getElementById('register-confirm-password');

    // Сброс ошибок
    clearErrors('register');

    // Проверка имени пользователя
    if (!username.value.trim()) {
        showError('register-username-error', 'Имя пользователя обязательно');
        isValid = false;
    }
    else if (username.value.trim().length < 3) {
        showError('register-username-error', 'Имя должно содержать минимум 3 символа');
        isValid = false;
    }
    else if (!/^[a-zA-Zа-яА-Я0-9_ ]+$/.test(username.value)) {
        showError('register-username-error', 'Имя может содержать только буквы, цифры, пробелы и подчеркивания');
        isValid = false;
    }

    // Проверка email
    if (!email.value.trim()) {
        showError('register-email-error', 'Email обязателен');
        isValid = false;
    }
    else if (!isValidEmail(email.value)) {
        showError('register-email-error', 'Введите корректный email адрес');
        isValid = false;
    }

    // Проверка логина
    if (!login.value.trim()) {
        showError('register-login-error', 'Логин обязателен');
        isValid = false;
    }
    else if (login.value.trim().length < 3) {
        showError('register-login-error', 'Логин должен содержать минимум 3 символа');
        isValid = false;
    }
    else if (!/^[a-zA-Z0-9_]+$/.test(login.value)) {
        showError('register-login-error', 'Логин может содержать только латинские буквы, цифры и подчеркивания');
        isValid = false;
    }

    // Проверка пароля
    if (!password.value) {
        showError('register-password-error', 'Пароль обязателен');
        isValid = false;
    }
    else if (password.value.length < 6) {
        showError('register-password-error', 'Пароль должен содержать минимум 6 символов');
        isValid = false;
    }
    else if (!/(?=.*[a-zA-Z])(?=.*\d)/.test(password.value)) {
        showError('register-password-error', 'Пароль должен содержать буквы и цифры');
        isValid = false;
    }

    // Проверка подтверждения пароля
    if (!confirmPassword.value) {
        showError('register-confirm-password-error', 'Подтвердите пароль');
        isValid = false;
    }
    else if (password.value !== confirmPassword.value) {
        showError('register-confirm-password-error', 'Пароли не совпадают');
        isValid = false;
    }

    return isValid;
}

function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function showError(elementId, message) {
    const errorElement = document.getElementById(elementId);
    errorElement.textContent = message;
    errorElement.classList.add('show');

    const inputElement = document.getElementById(elementId.replace('-error', ''));
    inputElement.classList.add('error');
}

function clearErrors(formType) {
    const errorElements = document.querySelectorAll(`[id$="-error"]`);
    errorElements.forEach(element => {
        if (element.id.startsWith(formType)) {
            element.classList.remove('show');
            element.textContent = '';

            const inputElement = document.getElementById(element.id.replace('-error', ''));
            if (inputElement) {
                inputElement.classList.remove('error');
            }
        }
    });
}

document.addEventListener('DOMContentLoaded', function () {
    // Валидация логина в реальном времени
    const loginInputs = ['login-username', 'login-password'];
    loginInputs.forEach(inputId => {
        const input = document.getElementById(inputId);
        if (input) {
            input.addEventListener('blur', validateLoginForm);
            input.addEventListener('input', function () {
                const errorElement = document.getElementById(inputId + '-error');
                if (errorElement) {
                    errorElement.classList.remove('show');
                    input.classList.remove('error');
                }
            });
        }
    });

    // Валидация регистрации в реальном времени
    const registerInputs = [
        'register-username', 'register-email', 'register-login',
        'register-password', 'register-confirm-password'
    ];

    registerInputs.forEach(inputId => {
        const input = document.getElementById(inputId);
        if (input) {
            input.addEventListener('blur', validateRegisterForm);
            input.addEventListener('input', function () {
                const errorElement = document.getElementById(inputId + '-error');
                if (errorElement) {
                    errorElement.classList.remove('show');
                    input.classList.remove('error');
                }

                if (inputId === 'register-password' || inputId === 'register-confirm-password') {
                    const password = document.getElementById('register-password').value;
                    const confirmPassword = document.getElementById('register-confirm-password').value;

                    if (password && confirmPassword && password !== confirmPassword) {
                        showError('register-confirm-password-error', 'Пароли не совпадают');
                    }
                }
            });
        }
    });
});