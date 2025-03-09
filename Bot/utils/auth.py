import telebot

user_data = {}
user_auth_status = {}

def is_authenticated(message):
    return user_auth_status.get(message.chat.id, False)

def offer_transaction_options(bot, message):
    if not is_authenticated(message):
        bot.send_message(message.chat.id, "You are not logged in! Please login or register.")
        return
    markup = telebot.types.ReplyKeyboardMarkup(resize_keyboard=True)
    btn1 = telebot.types.KeyboardButton("\U0001F4B8 Replenishment")
    btn2 = telebot.types.KeyboardButton("\U0001F4B3 Transfer")
    btn3 = telebot.types.KeyboardButton("\U0001F4B0 Withdrawal")
    btn4 = telebot.types.KeyboardButton("\U00002139 Information")
    markup.add(btn1, btn2, btn3, btn4)
    bot.send_message(message.chat.id, "Select a balance operation:", reply_markup=markup)
