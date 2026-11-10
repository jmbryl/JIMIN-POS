using EnterprisePOS.Domain;

namespace EnterprisePOS.Services
{
    /// <summary>Keeps checkout logic independent of the Windows printing API.</summary>
    public interface IReceiptPrinter
    {
        void Print(ReceiptData receipt);
        void Preview(ReceiptData receipt);
    }
}
