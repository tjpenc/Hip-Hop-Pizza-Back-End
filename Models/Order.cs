namespace HipHopPizzaBackend.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? PaymentTypeId { get; set; }
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string OrderType { get; set; }
        public decimal? TotalPrice { get; set; }
        public bool isOpen { get; set; }
        public decimal? Tip { get; set; }
        public DateTime? DateClosed { get; set; }
        public string? Comments { get; set; }
        public User User { get; set; }
        public PaymentType PaymentType { get; set; }
        public List<Item> Items { get; set; }
    }
}
