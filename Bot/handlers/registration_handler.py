import requests
from config.config import API_URL
from utils.auth import user_data, user_auth_status, offer_transaction_options
from handlers import start_handler, registration_handler, auth_handler, recovery_handler, balance_handler, transfer_handler, withdrawal_handler, info_handler


def register_handlers(bot):
    @bot.message_handler(func=lambda message: message.text == "\U0001F4DD Registration")
    def registration(message):
        user_data[message.chat.id] = {"UserId": message.chat.id}
        bot.send_message(message.chat.id, "Enter your full name:")
        bot.register_next_step_handler(message, process_registration_snp)

    def process_registration_snp(message):
        if (message.text == "\U0001F511 Sign in"):
            auth_handler.register_handlers(bot)
            return
        else:
            user_data[message.chat.id]["UserSNP"] = message.text
            bot.send_message(message.chat.id, "Enter your phone number:")
            bot.register_next_step_handler(message, process_registration_phone)

    def process_registration_phone(message):
        user_data[message.chat.id]["UserPhoneNumber"] = message.text
        bot.send_message(message.chat.id, "Enter your password:")
        bot.register_next_step_handler(message, process_registration_password)

    def process_registration_password(message):
        user_data[message.chat.id]["UserPassword"] = message.text
        response = requests.post(f"{API_URL}/user/logIn", json=user_data[message.chat.id])
        user_auth_status[message.chat.id] = response.status_code == 200
        bot.send_message(message.chat.id, response.text)
        offer_transaction_options(bot, message)