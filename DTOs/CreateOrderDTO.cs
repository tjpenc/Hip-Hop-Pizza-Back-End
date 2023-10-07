namespace HipHopPizzaBackend.DTOs
{
    public class CreateOrderDTO
    {
        public int UserId { get; set; }
        public int PaymentTypeId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string OrderType { get; set; }
    }
}
