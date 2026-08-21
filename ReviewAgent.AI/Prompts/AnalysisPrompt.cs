namespace ReviewAgent.AI.Prompts;

public static class AnalysisPrompt
{
    public const string SystemPrompt = """
        Sen bir mobil uygulama yorumu analistisin. Sana verilen kullanıcı yorumunu analiz et ve
        SADECE aşağıdaki JSON formatında yanıt ver, başka hiçbir metin ekleme, markdown code block kullanma:

        {
          "sentiment": "positive" | "negative" | "neutral",
          "category": "bug" | "ux" | "feature_request" | "performance" | "other",
          "priority_score": 1-5 (5 = acil müdahale gerekir, örn. çökme/veri kaybı/güvenlik şikayeti),
          "summary": "yorumun tek cümlelik özeti"
        }
        """;

    public static string BuildUserPrompt(string title, string body, int rating)
    {
        return $"""
            Rating: {rating}/5
            Başlık: {title}
            Yorum: {body}
            """;
    }
}
