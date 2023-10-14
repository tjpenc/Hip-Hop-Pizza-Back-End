namespace HipHopPizzaBackend.DTOs
{
    public class CloseOrderDTO
    {
        public int Tip { get; set; }
        public string Comments { get; set; }
        public int? PaymentTypeId { get; set; }
    }
}
