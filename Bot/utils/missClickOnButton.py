"""
from handlers import start_handler, registration_handler, auth_handler, recovery_handler, balance_handler, transfer_handler, withdrawal_handler, info_handler


#Обработчик мискликов
def missClick(message):
    match message:
        case "\U0001F4DD Регистрация":
            registration_handler.register_handlers(bot)
        case "\U0001F511 Вход":
            auth_handler.register_handlers(bot)
        case "\U0001F4B8 Пополнение":
            balance_handler.register_handlers(bot)
        case "\U0001F4B3 Перевод":
            transfer_handler.register_handlers(bot)
        case "\U0001F4B0 Снятие":
            withdrawal_handler.register_handlers(bot)
        case "\U00002139 Информация":
            info_handler.register_handlers(bot)
"""