namespace CEX_DEX_Parser.Data.Entities
{
    public class TelegramSubscriber
    {
        public string ChatId { get; set; } = string.Empty;
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }
}