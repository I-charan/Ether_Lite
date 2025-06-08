namespace Ether_Lite.Models
{
    public class WalletInfoResult
    {
        public string? Message { get; set; }
        public required string WalletAddress { get; set; }
        public decimal CurrentBalanceInEth { get; set; }
        public decimal CurrentGasPriceInGwei { get; set; }
        public string? Network { get; set; }
        public long? ScannedBlock { get; set; }
        public TransactionInfo? LastTransaction { get; set; }
    }
}
