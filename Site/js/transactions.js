const userId = localStorage.getItem("userId");

async function replenish() {
    const amount = prompt("Введите сумму для пополнения:");
    if (!amount) return;

    const response = await fetch(`${API_URL}/operationwithbalance/replenishment`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ UserId: userId, AmountOfMoney: amount })
    });

    alert(await response.text());
}

async function transfer() {
    const recipientId = prompt("Введите ID получателя:");
    const amount = prompt("Введите сумму перевода:");
    if (!recipientId || !amount) return;

    const response = await fetch(`${API_URL}/operationwithbalance/transfer`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ UserId: userId, RecipientId: recipientId, AmountOfMoney: amount })
    });

    alert(await response.text());
}

async function withdraw() {
    const amount = prompt("Введите сумму для снятия:");
    if (!amount) return;

    const response = await fetch(`${API_URL}/operationwithbalance/withdrawal`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ UserId: userId, AmountOfMoney: amount })
    });

    alert(await response.text());
}
