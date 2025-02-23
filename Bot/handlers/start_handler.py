import telebot

def register_handlers(bot):
    @bot.message_handler(commands=['start'])
    def send_welcome(message):
        markup = telebot.types.ReplyKeyboardMarkup(resize_keyboard=True)
        btn1 = telebot.types.KeyboardButton("\U0001F4DD Регистрация")
        btn2 = telebot.types.KeyboardButton("\U0001F511 Вход")
        markup.add(btn1, btn2)
        bot.send_message(message.chat.id, "Привет! Этот бот позволяет управлять вашим аккаунтом. Выберите действие:", reply_markup=markup)
