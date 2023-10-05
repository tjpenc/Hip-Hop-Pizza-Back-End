namespace HipHopPizzaBackend.Models
{
    public class Revenue
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int PaymentTypeId { get; set; }
        public decimal OrderTotal { get; set; }
        public int Tip { get; set; }
        public string OrderType { get; set; }
        public DateTime DateClosed { get; set; }
        public PaymentType PaymentType { get; set; }
    }
}
