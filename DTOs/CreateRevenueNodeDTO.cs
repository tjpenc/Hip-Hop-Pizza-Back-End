namespace HipHopPizzaBackend.DTOs
{
    public class CreateRevenueNodeDTO
    {
        public int OrderId { get; set; }
        public int PaymentTypeId { get; set; }
        public decimal OrderTotal { get; set; }
        public int Tip { get; set; }
        public string OrderType { get; set; }
    }
}
