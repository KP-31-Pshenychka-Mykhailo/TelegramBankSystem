import telebot
import requests
from config.config import API_URL
from utils.auth import user_data, is_authenticated
from handlers import start_handler, registration_handler, auth_handler, recovery_handler, balance_handler, transfer_handler, withdrawal_handler, info_handler

def register_handlers(bot):
    @bot.message_handler(func=lambda message: message.text == "\U0001F4B3 Перевод")
    def transfer(message):
        if not is_authenticated(message):
            bot.send_message(message.chat.id, "Вы не авторизованы!")
            return
        bot.send_message(message.chat.id, "Введите ID получателя:")
        bot.register_next_step_handler(message, process_transfer_recipient)

    def process_transfer_recipient(message):
        if (message.text == "\U0001F4B8 Пополнение" ):
            balance_handler.register_handlers(bot)
            return
        elif(message.text == "\U0001F4B0 Снятие"):
            withdrawal_handler.register_handlers(bot)
            return
        elif(message.text == "\U00002139 Информация"):
            info_handler.register_handlers(bot)
        else:
            user_data[message.chat.id] = {"UserId": message.chat.id, "RecipientId": message.text}
            bot.send_message(message.chat.id, "Введите сумму для перевода:")
            bot.register_next_step_handler(message, process_transfer_amount)

    def process_transfer_amount(message):
        user_data[message.chat.id]["AmountOfMoney"] = message.text
        response = requests.post(f"{API_URL}/operationwithbalance/transfer", json=user_data[message.chat.id])
        bot.send_message(message.chat.id, response.text)