import telebot
import requests
from config.config import API_URL
from utils.auth import is_authenticated
from handlers import start_handler, registration_handler, auth_handler, recovery_handler, balance_handler, transfer_handler, withdrawal_handler, info_handler

def register_handlers(bot):
    @bot.message_handler(func=lambda message: message.text == "\U0001F4B8 Replenishment")
    def replenishment(message):
        if not is_authenticated(message):
            bot.send_message(message.chat.id, "You are not logged in!")
            return
        bot.send_message(message.chat.id, "Enter the amount to top up:")
        bot.register_next_step_handler(message, process_replenishment)

    def process_replenishment(message):
        if (message.text == "\U0001F4B3 Transfer" ):
            transfer_handler.register_handlers(bot)
            return
        elif(message.text == "\U0001F4B0 Withdrawal"):
            withdrawal_handler.register_handlers(bot)
            return
        elif(message.text == "\U00002139 Information"):
            info_handler.register_handlers(bot)
            return
        else:
            data = {"UserId": message.chat.id, "AmountOfMoney": message.text}
            response = requests.post(f"{API_URL}/operationwithbalance/replenishment", json=data)
            bot.send_message(message.chat.id, response.text)