public static class WebhookEventStore
{
    private static readonly HashSet<string> ProcessedEvents = [];

    public static bool HasProcessed(string eventId)
    {
        return ProcessedEvents.Contains(eventId);
    }

    public static void MarkAsProcessed(string eventId)
    {
        ProcessedEvents.Add(eventId);
    }
}
