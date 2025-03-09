import telebot
from config.config import TOKEN
from handlers import start_handler, registration_handler, auth_handler, recovery_handler, balance_handler, transfer_handler, withdrawal_handler, info_handler

bot = telebot.TeleBot(TOKEN)

start_handler.register_handlers(bot)
registration_handler.register_handlers(bot)
auth_handler.register_handlers(bot)
recovery_handler.register_handlers(bot)
balance_handler.register_handlers(bot)
transfer_handler.register_handlers(bot)
withdrawal_handler.register_handlers(bot)
info_handler.register_handlers(bot)


@bot.message_handler(func=lambda message: True)
def unknown_command(message):
    bot.send_message(message.chat.id, "Unknown command. Try again.")


try:
    bot.polling(none_stop=True)
except Exception as e:
    print(f"Exeption wih start  bot: {str(e)}")