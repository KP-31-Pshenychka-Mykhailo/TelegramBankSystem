const API_URL = "http://localhost:5268";
const userId = localStorage.getItem("userId");

function toggleTile(tileId) {
    const selectedTile = document.getElementById(tileId);
    const isSelected = selectedTile.classList.contains('active');
    
    // Закрываем все плитки
    const tiles = document.querySelectorAll('.tile');
    tiles.forEach(tile => {
        tile.classList.remove('active');
    });

    // Открываем выбранную плитку, только если она была закрыта
    if (!isSelected) {
        selectedTile.classList.add('active');
    }
}

async function handleReplenish(event) {
    event.preventDefault();
    const form = event.target;
    const amount = parseFloat(form.amount.value);

    try {
        const response = await fetch(`${API_URL}/operationwithbalance/replenishment`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ 
                UserId: userId, 
                AmountOfMoney: amount 
            })
        });

        const message = await response.text();
        alert(message);
        
        if (response.ok) {
            form.reset();
            toggleTile('replenish-tile');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Произошла ошибка при выполнении операции');
    }
}

async function handleTransfer(event) {
    event.preventDefault();
    const form = event.target;
    const recipientId = form.recipientId.value;
    const amount = parseFloat(form.amount.value);

    try {
        const response = await fetch(`${API_URL}/operationwithbalance/transfer`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ 
                UserId: userId, 
                RecipientId: recipientId, 
                AmountOfMoney: amount 
            })
        });

        const message = await response.text();
        alert(message);
        
        if (response.ok) {
            form.reset();
            toggleTile('transfer-tile');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Произошла ошибка при выполнении операции');
    }
}

async function handleWithdraw(event) {
    event.preventDefault();
    const form = event.target;
    const amount = parseFloat(form.amount.value);

    try {
        const response = await fetch(`${API_URL}/operationwithbalance/withdrawal`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ 
                UserId: userId, 
                AmountOfMoney: amount 
            })
        });

        const message = await response.text();
        alert(message);
        
        if (response.ok) {
            form.reset();
            toggleTile('withdraw-tile');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Произошла ошибка при выполнении операции');
    }
}
