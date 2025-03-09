import telebot
import requests
from config.config import API_URL
from utils.auth import is_authenticated

def register_handlers(bot):
    @bot.message_handler(func=lambda message: message.text == "\U00002139 Information")
    def information(message):
        if not is_authenticated(message):
            bot.send_message(message.chat.id, "You are not logged in!")
            return
        response = requests.get(f"{API_URL}/operationwithbalance/showInformation/{message.chat.id}")
        bot.send_message(message.chat.id, response.text)