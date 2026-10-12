namespace GestaoFrota.API.Services;

/// <summary>
/// Identifica o formato real de uma imagem pelos primeiros bytes (magic
/// numbers), em vez de confiar no Content-Type enviado pelo cliente.
/// </summary>
public static class DetectorImagem
{
    public static string? DetectarContentType(byte[] bytes)
    {
        if (bytes.Length < 12)
        {
            return null;
        }

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return "image/png";
        }

        // WEBP: "RIFF" .... "WEBP"
        if (bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F'
            && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P')
        {
            return "image/webp";
        }

        // HEIC/HEIF: .... "ftyp" + marca "heic"/"heix"/"mif1"...
        if (bytes[4] == 'f' && bytes[5] == 't' && bytes[6] == 'y' && bytes[7] == 'p')
        {
            var marca = System.Text.Encoding.ASCII.GetString(bytes, 8, 4);
            if (marca is "heic" or "heix" or "hevc" or "mif1" or "msf1")
            {
                return "image/heic";
            }
        }

        return null;
    }
}
