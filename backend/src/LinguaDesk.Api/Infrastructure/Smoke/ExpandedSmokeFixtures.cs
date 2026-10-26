using System.Text.Json;

namespace LinguaDesk.Api.Infrastructure.Smoke;

/// <summary>Explicit synthetic external-adapter fixtures, never language detection or quality evidence.</summary>
internal static class ExpandedSmokeFixtures
{
    internal sealed record Fixture(string Language, string Source, string Result, string[] Options);
    internal static readonly Fixture[] Cases =
    [
        new("ar", "التقرير جاهز جدًا. سنرسله اليوم.", "التقرير جاهز. سنرسله اليوم.", ["أصبح التقرير جاهزًا.", "التقرير الآن جاهز."]),
        new("he", "הדוח ממש ממש מוכן. נשלח אותו היום.", "הדוח מוכן. נשלח אותו היום.", ["הדוח כבר מוכן.", "הדוח מוכן כעת."]),
        new("hi", "रिपोर्ट बिल्कुल बिल्कुल तैयार है। हम इसे आज भेजेंगे।", "रिपोर्ट तैयार है। हम इसे आज भेजेंगे।", ["रिपोर्ट अब तैयार है।", "अब रिपोर्ट तैयार है।"]),
        new("ja", "報告書はもうもう完成しています。今日送ります。", "報告書は完成しています。今日送ります。", ["報告書はもう完成しています。", "報告書はすでに完成しています。"]),
        new("ko", "보고서가 정말 정말 준비되었습니다. 오늘 보내겠습니다.", "보고서가 준비되었습니다. 오늘 보내겠습니다.", ["보고서가 이제 준비되었습니다.", "이제 보고서가 준비되었습니다."]),
        new("fil", "Handang handa na ang ulat. Ipapadala namin ito ngayon.", "Handa na ang ulat. Ipapadala namin ito ngayon.", ["Ang ulat ay handa na.", "Nakahanda na ang ulat."]),
        new("ckb", "ڕاپۆرتەکە ئامادەیە.", "ڕاپۆرتەکە ئامادەیە.", ["ئێستا ڕاپۆرتەکە ئامادەیە.", "ڕاپۆرتەکە ئێستا ئامادەیە."]),
        new("sr", "Izveštaj je zaista zaista spreman. Poslaćemo ga danas.", "Izveštaj je spreman. Poslaćemo ga danas.", ["Izveštaj je sada spreman.", "Sada je izveštaj spreman."]),
    ];

    internal static Fixture? Match(string text) => Cases.FirstOrDefault(fixture => fixture.Source == text || fixture.Result == text);
    internal static string? Classify(string source, JsonElement root)
    {
        var hint = root.TryGetProperty("sourceHint", out var element) && element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        if (source == "Rapporten er klar.")
            return hint is "nb" or "nn" ? JsonSerializer.Serialize(new { status = "eligible", language = hint }) : """{"status":"uncertain","language":null}""";
        var fixture = Match(source);
        return fixture is null ? null : hint is not null && hint != fixture.Language
            ? JsonSerializer.Serialize(new { status = "source_mismatch", language = fixture.Language })
            : JsonSerializer.Serialize(new { status = "eligible", language = fixture.Language });
    }
}
