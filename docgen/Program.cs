using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using Color = DocumentFormat.OpenXml.Wordprocessing.Color;

var outputDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs"));
Directory.CreateDirectory(outputDirectory);
var outputPath = Path.Combine(outputDirectory, "Guia_instalacion_RI_MSSQL_MCP.docx");

using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
var mainPart = document.AddMainDocumentPart();
mainPart.Document = new Document();
var body = mainPart.Document.AppendChild(new Body());

AddParagraph(body, "Guía de instalación de RI MSSQL MCP", 34, bold: true, after: 140);
AddParagraph(body, "Instalación para personal de soporte en Windows", 21, color: "555555", after: 260);
AddParagraph(body,
    "Esta guía permite instalar el servidor MCP publicado y conectarlo con Codex. El paquete es autocontenido: no requiere instalar .NET ni Docker.",
    21, after: 220);

AddHeading(body, "Requisitos");
AddBullet(body, "Windows de 64 bits.");
AddBullet(body, "Codex instalado y con la sesión iniciada.");
AddBullet(body, "Cadena de conexión SQL Server suministrada por el responsable del sistema.");
AddBullet(body, "La cuenta SQL debe tener permisos únicamente de lectura.", after: 220);

AddHeading(body, "Instalación");
AddStep(body, 1, "Descargue el paquete publicado:");
AddHyperlinkParagraph(mainPart, body,
    "https://github.com/AdrianGonzalezA/MSSQLMCP/raw/main/dist/RI-MSSQL-MCP-1.0.0-win-x64.zip");
AddStep(body, 2, "Extraiga todo el ZIP en una carpeta permanente, por ejemplo:");
AddCode(body, @"C:\Herramientas\RiMssqlMcp");
AddStep(body, 3, "Abra PowerShell dentro de la carpeta extraída y ejecute:");
AddCode(body, "Set-ExecutionPolicy -Scope Process Bypass");
AddCode(body, @".\Instalar-MssqlMcp.ps1", after: 130);
AddStep(body, 4, "Indique el nombre del perfil y pegue la cadena de conexión cuando el instalador la solicite. La cadena no se muestra en pantalla.");
AddStep(body, 5, "Cierre Codex y vuelva a abrirlo para cargar el nuevo servidor MCP.", after: 220);

AddHeading(body, "Prueba rápida");
AddParagraph(body, "En una sesión nueva de Codex, escriba:", 21, after: 80);
AddCode(body, "Usa ri-mssql para listar las conexiones disponibles.", after: 180);
AddParagraph(body, "Si Codex devuelve el perfil configurado, la instalación terminó correctamente.", 21, after: 220);

AddHeading(body, "Soporte y seguridad");
AddBullet(body, "No comparta la cadena de conexión por correo ni por chat.");
AddBullet(body, "No mueva la carpeta después de instalar; Codex guarda la ruta absoluta del ejecutable.");
AddBullet(body, "Para desinstalar, ejecute .\\Desinstalar-MssqlMcp.ps1 desde la carpeta del paquete.");
AddBullet(body, "Encrypt=False debe utilizarse solo en la instancia de demostración que no admite el cifrado requerido.");

body.Append(new SectionProperties(
    new PageSize { Width = 11906, Height = 16838 },
    new PageMargin { Top = 700, Right = 850, Bottom = 700, Left = 850, Header = 360, Footer = 360 }));

mainPart.Document.Save();
var validationErrors = new OpenXmlValidator(FileFormatVersions.Microsoft365).Validate(document).ToList();
if (validationErrors.Count > 0)
    throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors.Select(error => error.Description)));
Console.WriteLine(outputPath);

static void AddHeading(Body body, string text) =>
    AddParagraph(body, text, 24, bold: true, before: 120, after: 100, keepNext: true);

static void AddBullet(Body body, string text, int after = 55) =>
    AddParagraph(body, "•  " + text, 20, left: 260, hanging: 220, after: after);

static void AddStep(Body body, int number, string text, int after = 65) =>
    AddParagraph(body, $"{number}.  {text}", 20, boldPrefixLength: number.ToString().Length + 1,
        left: 260, hanging: 260, after: after);

static void AddCode(Body body, string text, int after = 55) =>
    AddParagraph(body, text, 18, font: "Consolas", color: "303030", left: 520, after: after);

static void AddHyperlinkParagraph(MainDocumentPart mainPart, Body body, string url)
{
    var paragraph = new Paragraph(new ParagraphProperties(
        new SpacingBetweenLines { After = "100" },
        new Indentation { Left = "520" }));
    var relationship = mainPart.AddHyperlinkRelationship(new Uri(url), true);
    var hyperlink = new Hyperlink(
        new Run(
            new RunProperties(
                new RunFonts { Ascii = "Aptos", HighAnsi = "Aptos" },
                new Color { Val = "0563C1" },
                new FontSize { Val = "18" },
                new Underline { Val = UnderlineValues.Single }),
            new Text(url)))
    { Id = relationship.Id };
    paragraph.Append(hyperlink);
    body.Append(paragraph);
}

static void AddParagraph(Body body, string text, int size, bool bold = false, string color = "000000",
    string font = "Aptos", int before = 0, int after = 80, int left = 0, int hanging = 0,
    bool keepNext = false, int boldPrefixLength = 0)
{
    var properties = new ParagraphProperties();
    if (keepNext)
        properties.Append(new KeepNext());
    properties.Append(new SpacingBetweenLines { Before = before.ToString(), After = after.ToString(), Line = "250", LineRule = LineSpacingRuleValues.Auto });
    if (left > 0 || hanging > 0)
        properties.Append(new Indentation { Left = left.ToString(), Hanging = hanging.ToString() });

    var paragraph = new Paragraph(properties);
    if (boldPrefixLength > 0)
    {
        paragraph.Append(CreateRun(text[..boldPrefixLength], size, true, color, font));
        paragraph.Append(CreateRun(text[boldPrefixLength..], size, bold, color, font));
    }
    else
    {
        paragraph.Append(CreateRun(text, size, bold, color, font));
    }
    body.Append(paragraph);
}

static Run CreateRun(string text, int size, bool bold, string color, string font)
{
    var properties = new RunProperties();
    properties.Append(new RunFonts { Ascii = font, HighAnsi = font });
    if (bold)
        properties.Append(new Bold());
    properties.Append(new Color { Val = color });
    properties.Append(new FontSize { Val = size.ToString() });
    properties.Append(new FontSizeComplexScript { Val = size.ToString() });
    return new Run(properties, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
}
