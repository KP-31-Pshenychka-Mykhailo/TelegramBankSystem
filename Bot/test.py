import telebot
import requests

TOKEN = "7415665483:AAGfRb41Oyf54_fDZeiZ_R3_nd-C1JzLFNg"
API_URL = "http://localhost:5268"
bot = telebot.TeleBot(TOKEN)

user_data = {}
user_auth_status = {}

# Главное меню
@bot.message_handler(commands=['start'])
def send_welcome(message):
    markup = telebot.types.ReplyKeyboardMarkup(resize_keyboard=True)
    btn1 = telebot.types.KeyboardButton("\U0001F4DD Регистрация")
    btn2 = telebot.types.KeyboardButton("\U0001F511 Вход")
    markup.add(btn1, btn2)
    bot.send_message(message.chat.id, "Привет! Этот бот позволяет управлять вашим аккаунтом. Выберите действие:", reply_markup=markup)

# Регистрация
@bot.message_handler(func=lambda message: message.text == "\U0001F4DD Регистрация")
def registration(message):
    print(message.chat.id)
    user_data[message.chat.id] = {"UserId": message.chat.id}
    bot.send_message(message.chat.id, "Введите ваше ФИО:")
    bot.register_next_step_handler(message, process_registration_snp)

def process_registration_snp(message):
    user_data[message.chat.id]["UserSNP"] = message.text
    bot.send_message(message.chat.id, "Введите ваш номер телефона:")
    bot.register_next_step_handler(message, process_registration_phone)

def process_registration_phone(message):
    user_data[message.chat.id]["UserPhoneNumber"] = message.text
    bot.send_message(message.chat.id, "Введите ваш password:")
    bot.register_next_step_handler(message, process_registration_password)

def process_registration_password(message):
    user_data[message.chat.id]["UserPassword"] = message.text
    response = requests.post(f"{API_URL}/user/logIn", json=user_data[message.chat.id])
    user_auth_status[message.chat.id] = response.status_code == 200
    bot.send_message(message.chat.id, response.text)
    offer_transaction_options(message)

# Вход
@bot.message_handler(func=lambda message: message.text == "\U0001F511 Вход")

def process_login_oldid(message):
    bot.send_message(message.chat.id, "Введите ваш старый ID:")
    bot.register_next_step_handler(message, process_login_password)

def process_login_password(message):
    ID = message.text
    bot.send_message(message.chat.id, "Введите ваш пароль:")
    bot.register_next_step_handler(message, process_login,ID)

def process_login(message, ID):
    data = {"NewUserId": message.chat.id, "OldUserId": ID , "UserPassword": message.text} #Problem BUG FIX that
    response = requests.post(f"{API_URL}/user/signIn", json=data)
    user_auth_status[message.chat.id] = response.status_code == 200
    if "ошибка пароля" in response.text.lower():
        bot.send_message(message.chat.id, "Ошибка пароля! Хотите восстановить аккаунт? Напишите 'Восстановление'.")
        bot.register_next_step_handler(message, account_recovery)
    else:
        bot.send_message(message.chat.id, response.text)
        offer_transaction_options(message)

# Восстановление аккаунта
@bot.message_handler(func=lambda message: "восстановление" in message.text.lower())
def account_recovery(message):
    bot.send_message(message.chat.id, "Введите ваш старый ID:")
    bot.register_next_step_handler(message, process_recovery_old_id)

def process_recovery_old_id(message):
    user_data[message.chat.id] = {"NewUserId": message.chat.id, "OldUserId": message.text}
    bot.send_message(message.chat.id, "Введите ваш номер телефона:")
    bot.register_next_step_handler(message, process_recovery_phone)

def process_recovery_phone(message):
    user_data[message.chat.id]["UserPhoneNumber"] = message.text
    response = requests.post(f"{API_URL}/user/accountRecovery", json=user_data[message.chat.id])
    user_auth_status[message.chat.id] = response.status_code == 200
    bot.send_message(message.chat.id, response.text)
    offer_transaction_options(message)

# Проверка аутентификации
def is_authenticated(message):
    return user_auth_status.get(message.chat.id, False)

# Выбор операции с балансом
def offer_transaction_options(message):
    if not is_authenticated(message):
        bot.send_message(message.chat.id, "Вы не авторизованы! Пожалуйста, войдите или зарегистрируйтесь.")
        return
    markup = telebot.types.ReplyKeyboardMarkup(resize_keyboard=True)
    btn1 = telebot.types.KeyboardButton("\U0001F4B8 Пополнение")
    btn2 = telebot.types.KeyboardButton("\U0001F4B3 Перевод")
    btn3 = telebot.types.KeyboardButton("\U0001F4B0 Снятие")
    btn4 = telebot.types.KeyboardButton("\U00002139 Информация")
    markup.add(btn1, btn2, btn3, btn4)
    bot.send_message(message.chat.id, "Выберите операцию с балансом:", reply_markup=markup)

# Операции с балансом
@bot.message_handler(func=lambda message: message.text == "\U0001F4B8 Пополнение")
def replenishment(message):
    if not is_authenticated(message):
        bot.send_message(message.chat.id, "Вы не авторизованы!")
        return
    bot.send_message(message.chat.id, "Введите сумму для пополнения:")
    bot.register_next_step_handler(message, process_replenishment)

def process_replenishment(message):
    data = {"UserId": message.chat.id, "AmountOfMoney": message.text}
    response = requests.post(f"{API_URL}/operationwithbalance/replenishment", json=data)
    if (response.status_code == 400):
        bot.send_message(message.chat.id, "You can send only integers nums")
    bot.send_message(message.chat.id, response.text)

@bot.message_handler(func=lambda message: message.text == "\U0001F4B3 Перевод")
def transfer(message):
    if not is_authenticated(message):
        bot.send_message(message.chat.id, "Вы не авторизованы!")
        return
    bot.send_message(message.chat.id, "Введите ID получателя:")
    bot.register_next_step_handler(message, process_transfer_recipient)

@bot.message_handler(func=lambda message: message.text == "\U0001F4B0 Снятие")
def withdrawal(message):
    if not is_authenticated(message):
        bot.send_message(message.chat.id, "Вы не авторизованы!")
        return
    bot.send_message(message.chat.id, "Введите сумму для снятия:")
    bot.register_next_step_handler(message, process_withdrawal)

def process_transfer_recipient(message):
    user_data[message.chat.id] = {"UserId": message.chat.id, "RecipientId": message.text}
    bot.send_message(message.chat.id, "Введите сумму для перевода:")

    bot.register_next_step_handler(message, process_transfer_amount)

def process_transfer_amount(message):
    user_data[message.chat.id]["AmountOfMoney"] = message.text
    response = requests.post(f"{API_URL}/operationwithbalance/transfer", json=user_data[message.chat.id])
    if (response.status_code == 400):
        bot.send_message(message.chat.id, "You can send only integers nums")
    bot.send_message(message.chat.id, response.text)

def process_withdrawal(message):
    data = {"UserId": message.chat.id, "AmountOfMoney": message.text}
    response = requests.post(f"{API_URL}/operationwithbalance/withdrawal", json=data)
    bot.send_message(message.chat.id, response.text)

@bot.message_handler(func=lambda message: message.text == "\U00002139 Информация")
def information(message):
    if not is_authenticated(message):
        bot.send_message(message.chat.id, "Вы не авторизованы!")
        return
    response = requests.get(f"{API_URL}/operationwithbalance/showInformation/{message.chat.id}")
    bot.send_message(message.chat.id, response.text)


# Запуск бота
bot.polling(none_stop=True)