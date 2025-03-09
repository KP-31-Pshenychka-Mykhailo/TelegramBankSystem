import requests
from config.config import API_URL
from utils.auth import user_auth_status, offer_transaction_options
from handlers import start_handler, registration_handler,  recovery_handler, balance_handler, transfer_handler, withdrawal_handler, info_handler

def register_handlers(bot):
    @bot.message_handler(func=lambda message: message.text == "\U0001F511 Sign in")
    def process_login_oldid(message):
        bot.send_message(message.chat.id, "Enter your old ID:")
        bot.register_next_step_handler(message, process_login_password)

    def process_login_password(message):
        if (message.text == "\U0001F4DD Registration"):
            registration_handler.register_handlers(bot)
            return
        else:
            ID = message.text
            bot.send_message(message.chat.id, "Enter your password:")
            bot.register_next_step_handler(message, process_login, ID)

    def process_login(message, ID):
        data = {"NewUserId": message.chat.id, "OldUserId": ID, "UserPassword": message.text}
        response = requests.post(f"{API_URL}/user/signIn", json=data)
        user_auth_status[message.chat.id] = response.status_code == 200
        bot.send_message(message.chat.id, response.text)
        if "ошибка пароля" in response.text.lower():
            bot.send_message(message.chat.id, "Password error! Want to recover your account? Write 'Recovery'.")
        else:
            offer_transaction_options(bot, message)