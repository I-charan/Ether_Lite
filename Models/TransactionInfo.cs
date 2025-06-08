using System.Numerics;

namespace Ether_Lite.Models
{
    public class TransactionInfo
    {
        public string? TxHash { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public decimal ValueInEth { get; set; }
        public BigInteger GasUsed { get; set; }
        public BigInteger BlockNumber { get; set; }
        public DateTime DateTimeUtc { get; set; }
    }
}
