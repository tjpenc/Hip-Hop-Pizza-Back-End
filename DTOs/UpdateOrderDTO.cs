namespace HipHopPizzaBackend.DTOs
{
    public class UpdateOrderDTO
    {
        public int UserId { get; set; }
        public int PaymentTypeId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string OrderType { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Tip { get; set; }
        public string Comments { get; set; }
    }
}
