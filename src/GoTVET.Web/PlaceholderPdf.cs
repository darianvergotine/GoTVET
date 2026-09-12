using System.Text;

namespace GoTVET.Web;

public static class PlaceholderPdf
{
    public static byte[] Create(string heading, string details)
    {
        var text = $"BT /F1 22 Tf 56 760 Td ({Escape(heading)}) Tj /F1 12 Tf 0 -28 Td ({Escape(details)}) Tj 0 -22 Td (Placeholder file for GoTVET testing. Replace with the official paper.) Tj ET";
        var content = Encoding.ASCII.GetBytes(text);
        var objects = new List<byte[]>
        {
            Obj(1, "<< /Type /Catalog /Pages 2 0 R >>"),
            Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>"),
            Obj(4, $"<< /Length {content.Length} >>\nstream\n{text}\nendstream"),
            Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")
        };

        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(stream.Position);
            stream.Write(obj);
        }

        var xref = stream.Position;
        var xrefBuilder = new StringBuilder();
        xrefBuilder.Append($"xref\n0 {objects.Count + 1}\n");
        xrefBuilder.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            xrefBuilder.Append($"{offsets[i]:D10} 00000 n \n");
        }

        xrefBuilder.Append($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        Write(stream, xrefBuilder.ToString());
        return stream.ToArray();
    }

    private static byte[] Obj(int id, string body) =>
        Encoding.ASCII.GetBytes($"{id} 0 obj\n{body}\nendobj\n");

    private static void Write(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes);
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
