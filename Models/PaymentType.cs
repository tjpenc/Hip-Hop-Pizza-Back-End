namespace HipHopPizzaBackend.Models
{
    public class PaymentType
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public List<Order> Orders { get; set; }
    }
}
