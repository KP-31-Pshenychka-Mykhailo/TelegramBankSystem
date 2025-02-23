import telebot

user_data = {}
user_auth_status = {}

def is_authenticated(message):
    return user_auth_status.get(message.chat.id, False)

def offer_transaction_options(bot, message):
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
