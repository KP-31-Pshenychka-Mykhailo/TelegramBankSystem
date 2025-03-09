import telebot
import requests
from config.config import API_URL
from utils.auth import user_data, user_auth_status, offer_transaction_options

def register_handlers(bot):
    @bot.message_handler(func=lambda message: "recovery" in message.text.lower())
    def account_recovery(message):
        bot.send_message(message.chat.id, "Enter your old ID:")
        bot.register_next_step_handler(message, process_recovery_old_id)

    def process_recovery_old_id(message):
        user_data[message.chat.id] = {"NewUserId": message.chat.id, "OldUserId": message.text}
        bot.send_message(message.chat.id, "Enter your phone number:")
        bot.register_next_step_handler(message, process_recovery_phone)

    def process_recovery_phone(message):
        user_data[message.chat.id]["UserPhoneNumber"] = message.text
        response = requests.post(f"{API_URL}/user/accountRecovery", json=user_data[message.chat.id])
        user_auth_status[message.chat.id] = response.status_code == 200
        bot.send_message(message.chat.id, response.text)
        offer_transaction_options(bot, message)
