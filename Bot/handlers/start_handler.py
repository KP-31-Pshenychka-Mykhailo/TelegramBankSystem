import telebot

def register_handlers(bot):
    @bot.message_handler(commands=['start'])
    def send_welcome(message):
        markup = telebot.types.ReplyKeyboardMarkup(resize_keyboard=True)
        btn1 = telebot.types.KeyboardButton("\U0001F4DD Registration")
        btn2 = telebot.types.KeyboardButton("\U0001F511 Sign in")
        markup.add(btn1, btn2)
        bot.send_message(message.chat.id, "Hello! This bot allows you to manage your account. Select an action:", reply_markup=markup)
