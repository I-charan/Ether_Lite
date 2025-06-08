document.getElementById("checkWalletBtn").addEventListener("click", checkWallet);

async function checkWallet() {
    const address = document.getElementById("address").value;
    const network = document.getElementById("network").value;
    const resultDiv = document.getElementById("result");

    if (!address) {
        showAlert("Please enter a wallet address.", "warning");
        return;
    }

    try {
        // Show loading state
        resultDiv.style.display = "block";
        resultDiv.className = "alert alert-info";
        resultDiv.innerHTML = `<div class="spinner-border spinner-border-sm" role="status">
                              <span class="visually-hidden">Loading...</span>
                              </div> Fetching wallet data...`;

        // Map frontend network names to backend endpoint patterns
        const endpointMap = {
            ethereum: 'ethereum/mainnet',   // ✅ This is needed if the <select> value is "ethereum"
            mainnet: 'ethereum/mainnet',   // ✅ Optional alias if user selects "mainnet"
            sepolia: 'ethereum/sepolia',
            arb: 'arb',
            polygonmainnet: 'polygon/mainnet',
            op: 'op'
        };


        const endpoint = endpointMap[network.toLowerCase()] || network;
        const response = await fetch(`/api/wallet-info/${endpoint}/${address}`);

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        console.log("API response payload:", data);
        displayWalletInfo(data, network);
    } catch (error) {
        showAlert(error.message || "Error fetching wallet info. Please try again.", "danger");
        console.error("Error:", error);
    }
}

function displayWalletInfo(data, network) {
    const resultDiv = document.getElementById("result");
    resultDiv.style.display = "block";

    // 🛡️ Check valid structure
    if (!data || typeof data !== 'object') {
        showAlert("No wallet data found", "warning");
        return;
    }

    // 🌐 Normalize property names
    const walletAddress = data.walletAddress || data.address || "N/A";
    const balanceEth = parseFloat(data.currentBalanceInEth || data.balanceInEth || data.balance || 0);
    const gasGwei = parseFloat(data.currentGasPriceInGwei || data.gasPriceInGwei || data.gasPrice || 0);
    const tx = data.lastTransaction || data.lastTx || null;

    const currencySymbol = getCurrencySymbol(network);
    const currencyName = getCurrencyName(network);

    let html = `
        <h5>🧾 ${currencyName} Wallet Summary</h5>
        <p><strong>💼 Wallet:</strong> ${walletAddress}</p>
        <p><strong>💰 Current Balance:</strong> ${isNaN(balanceEth) ? 'N/A' : balanceEth.toFixed(5)} ${currencySymbol}</p>
        <p><strong>⛽ Current Gas Price:</strong> ${isNaN(gasGwei) ? 'N/A' : gasGwei.toFixed(5)} Gwei</p>
    `;

    // 🧾 Add Last Transaction (if present)
    if (tx) {
        const date = new Date(tx.dateTimeUtc || tx.dateTime).toLocaleString();
        html += `
            <hr>
            <h5>🔁 Last Transaction</h5>
            <p><strong>📅 Date:</strong> ${date}</p>
            <p><strong>📤 From:</strong> ${tx.from}</p>
            <p><strong>📥 To:</strong> ${tx.to}</p>
            <p><strong>💸 Amount Sent:</strong> ${parseFloat(tx.valueInEth || tx.value || 0).toFixed(5)} ${currencySymbol}</p>
            <p><strong>🔗 Tx Hash:</strong> <a href="${getExplorerUrl(network, tx.txHash)}" target="_blank">${tx.txHash}</a></p>
        `;
    }

    resultDiv.className = "alert alert-success";
    resultDiv.innerHTML = html;
}


function getExplorerUrl(network, txHash) {
    const explorers = {
        'mainnet': `https://etherscan.io/tx/${txHash}`,
        'sepolia': `https://sepolia.etherscan.io/tx/${txHash}`,
        'arb': `https://arbiscan.io/tx/${txHash}`,
        'polygonmainnet': `https://polygonscan.com/tx/${txHash}`,
        'op': `https://optimistic.etherscan.io/tx/${txHash}`
    };
    return explorers[network.toLowerCase()] || `https://etherscan.io/tx/${txHash}`;
}

// Rest of the helper functions remain the same
function showAlert(message, type) {
    const resultDiv = document.getElementById("result");
    resultDiv.style.display = "block";
    resultDiv.className = `alert alert-${type}`;
    resultDiv.innerText = message;
}

function getCurrencySymbol(network) {
    const symbols = {
        'mainnet': 'ETH',
        'sepolia': 'ETH',
        'arb': 'ETH',
        'polygonmainnet': 'MATIC',
        'op': 'ETH'
    };
    return symbols[network.toLowerCase()] || 'ETH';
}

function getCurrencyName(network) {
    const names = {
        'mainnet': 'Ethereum Mainnet',
        'sepolia': 'Ethereum Sepolia',
        'arb': 'Arbitrum Mainnet',
        'polygonmainnet': 'Polygon Mainnet',
        'op': 'Optimism Mainnet'
    };
    return names[network.toLowerCase()] || 'Ethereum';
}