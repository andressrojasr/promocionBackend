using System.Reflection;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.DTOs.Actas;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PromocionBackend.Infrastructure.Pdf;

/// <summary>
/// Maqueta el acta de promoción del personal académico replicando la plantilla oficial
/// (MODELO_ACTA_PROMOCION_PERSONAL_ACADEMICO_UTA.docx): tamaño Carta, Times New Roman,
/// colores institucionales, logotipo, tablas y pie con código/página. Solo se llenan los
/// campos de los que el sistema tiene información; RMU, tiempo de dedicación, firmas y el
/// número de anexos quedan en blanco.
/// </summary>
public class ActaPdfBuilder : IActaPdfBuilder
{
    private const string Navy = "#000856";
    private const string SubheadingBlue = "#365F91";
    private const string HeaderFillMembers = "#D9E2F3";
    private const string HeaderFillResults = "#D9E1F2";
    private const string LabelFill = "#DBE5F1";
    private const string ProvisionalRed = "#B00020";
    private const string BorderGrey = "#808080";
    private const string BorderGreyAlt = "#7F7F7F";

    private const float MarginHorizontal = 44.64f; // 893 twips
    private const float MarginVertical = 39.6f;    // 792 twips
    private const float LetterWidth = 612f;
    private const float ContentWidth = LetterWidth - 2 * MarginHorizontal;
    private const float BorderWidth = 0.75f;

    private static readonly string[] Fonts = ["Times New Roman", "Liberation Serif", "Tinos"];

    private static readonly Lazy<byte[]> Logo = new(() =>
    {
        using var stream = typeof(ActaPdfBuilder).Assembly.GetManifestResourceStream("uta-logo.png")
            ?? throw new InvalidOperationException("No se encontró el logotipo institucional embebido.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    });

    public byte[] Build(ActaData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(MarginHorizontal);
                page.MarginVertical(MarginVertical);
                page.DefaultTextStyle(x => x.FontFamily(Fonts).FontSize(12).LineHeight(1.05f));

                page.Content().Column(col =>
                {
                    col.Spacing(5);

                    col.Item().Element(c => Header(c, data.IsProvisional));

                    col.Item().Text(text =>
                    {
                        text.Justify();
                        text.Span("En la ciudad de Ambato a los ");
                        text.Span(data.Day.ToString()).Bold();
                        text.Span(" días del mes de ");
                        text.Span(data.MonthName).Bold();
                        text.Span(" de ");
                        text.Span(data.Year.ToString()).Bold();
                        text.Span(", siendo las ");
                        text.Span(data.Time).Bold();
                        text.Span(", se reúne la Comisión Académica de Escalafón y Promoción del Personal Académico Titular de la Universidad Técnica de Ambato, integrada por:");
                    });

                    const float membersWidth = 9354;
                    col.Item().Element(c => TableFrame(c, BorderGrey, membersWidth)).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(624);
                            columns.RelativeColumn(2494);
                            columns.RelativeColumn(6236);
                        });

                        table.Cell().Element(c => HeaderCell(c, HeaderFillMembers, BorderGrey)).Text("N.°").Bold().AlignCenter();
                        table.Cell().Element(c => HeaderCell(c, HeaderFillMembers, BorderGrey)).Text("NOMBRE").Bold().AlignCenter();
                        table.Cell().Element(c => HeaderCell(c, HeaderFillMembers, BorderGrey)).Text("REPRESENTACIÓN / CARGO EN LA COMISIÓN").Bold().AlignCenter();

                        var index = 1;
                        foreach (var member in data.Members)
                        {
                            table.Cell().Element(c => BodyCell(c, BorderGrey)).Text((index++).ToString()).AlignCenter();
                            table.Cell().Element(c => BodyCell(c, BorderGrey)).Text(member.FullName);
                            table.Cell().Element(c => BodyCell(c, BorderGrey)).Text(member.CargoLabel);
                        }
                    });

                    col.Item().Element(Heading("1. OBJETO Y FINALIDAD DEL ACTA"));
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("La presente acta tiene por objeto dejar constancia de las actuaciones de la Comisión Académica de Escalafón y Promoción del Personal Académico Titular respecto del conocimiento, análisis y resolución de las solicitudes de promoción presentadas por el personal académico titular, con base en los documentos habilitantes registrados en la plataforma institucional y en los requisitos generales y específicos aplicables al grado escalafonario correspondiente. Los resultados consignados constituirán sustento para el informe de promoción que la Comisión presente al Honorable Consejo Universitario para su conocimiento y aprobación.");
                    });

                    col.Item().Element(Heading("2. ANTECEDENTES Y DOCUMENTACIÓN OBJETO DE REVISIÓN"));
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Para el desarrollo de la sesión se consideran los antecedentes y documentos institucionales vinculados al proceso de promoción, conforme a la normativa aplicable.");
                    });

                    const float categoryWidth = 10454;
                    col.Item().Element(c => TableFrame(c, BorderGreyAlt, categoryWidth)).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(5227);
                            columns.RelativeColumn(5227);
                        });

                        void Row(string label, string value)
                        {
                            table.Cell().Element(c => LabelCell(c, BorderGreyAlt)).Text(label).Bold();
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text(value);
                        }

                        Row("Facultad:", data.FacultyName);
                        Row("Categoría o nivel escalafonario de origen:", data.OriginCategoryLabel);
                        Row("Categoría o nivel escalafonario objeto de promoción:", data.DestinationCategoryLabel);
                    });

                    col.Item().Element(Heading("3. PERSONAL ACADÉMICO SUJETO A REVISIÓN"));
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Se deja constancia de las solicitudes de promoción y de la documentación habilitante registrada en la plataforma institucional dentro de los plazos establecidos en la convocatoria, que son sometidas al conocimiento y análisis de la Comisión:");
                    });

                    if (data.ReviewedTeacherNames.Count == 0)
                    {
                        col.Item().Text("- [SIN POSTULANTES REGISTRADOS]").Bold();
                    }
                    else
                    {
                        foreach (var name in data.ReviewedTeacherNames)
                        {
                            col.Item().Text($"- {name}").Bold();
                        }
                    }

                    col.Item().Element(Heading("4. REVISIÓN DE LA DOCUMENTACIÓN Y REQUISITOS"));
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("La Comisión conoce y analiza cada solicitud considerando las disposiciones generales para la promoción, los requisitos generales, los requisitos específicos correspondientes al grado escalafonario al que se postula, las equivalencias y excepcionalidades que resulten aplicables, así como la validez de los documentos habilitantes presentados en formato digital a través de la plataforma institucional.");
                    });

                    col.Item().Element(Heading("5. RESULTADOS DEL PROCESO"));
                    col.Item().Element(Heading("5.1. Personal académico que se promociona", SubheadingBlue, topPadding: 0));
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Concluido el análisis, la Comisión deja constancia del resultado de las solicitudes de promoción. La promoción no modificará el tiempo de dedicación del personal académico.");
                    });

                    const float approvedWidth = 10614;
                    col.Item().Element(c => TableFrame(c, BorderGreyAlt, approvedWidth)).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1284);
                            columns.RelativeColumn(2246);
                            columns.RelativeColumn(2352);
                            columns.RelativeColumn(1183);
                            columns.RelativeColumn(1606);
                            columns.RelativeColumn(1943);
                        });

                        foreach (var title in new[] { "CÉDULA", "APELLIDOS Y NOMBRES", "CATEGORÍA Y NIVEL ESCALAFONARIO RESULTANTE", "RMU", "TIEMPO DE DEDICACIÓN", "OBSERVACIÓN" })
                        {
                            table.Cell().Element(c => HeaderCell(c, HeaderFillResults, BorderGreyAlt)).Text(title).Bold().AlignCenter();
                        }

                        if (data.Approved.Count == 0)
                        {
                            table.Cell().ColumnSpan(6).Element(c => BodyCell(c, BorderGreyAlt)).Text("[SIN REGISTROS]");
                        }

                        foreach (var row in data.Approved)
                        {
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text(row.Identification);
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text(row.FullName);
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text(row.ResultingCategoryLabel);
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text("");
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text("");
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text(row.Observation ?? "");
                        }
                    });

                    col.Item().Element(Heading("5.2. Personal académico que no se promociona", SubheadingBlue, topPadding: 5));
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("De conformidad con la documentación revisada, se deja constancia del personal académico cuyo resultado no corresponde a promoción:");
                    });

                    col.Item().Element(c => TableFrame(c, BorderGreyAlt, approvedWidth)).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1703);
                            columns.RelativeColumn(3352);
                            columns.RelativeColumn(5559);
                        });

                        foreach (var title in new[] { "CÉDULA", "APELLIDOS Y NOMBRES", "OBSERVACIÓN" })
                        {
                            table.Cell().Element(c => HeaderCell(c, HeaderFillResults, BorderGreyAlt)).Text(title).Bold().AlignCenter();
                        }

                        if (data.Rejected.Count == 0)
                        {
                            table.Cell().ColumnSpan(3).Element(c => BodyCell(c, BorderGreyAlt)).Text("[SIN REGISTROS]");
                        }

                        foreach (var row in data.Rejected)
                        {
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text(row.Identification);
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text(row.FullName);
                            table.Cell().Element(c => BodyCell(c, BorderGreyAlt)).Text(row.Observation ?? "");
                        }
                    });

                    col.Item().Element(Heading("6. OBSERVACIONES"));
                    if (data.IsProvisional)
                    {
                        col.Item().Text(t =>
                        {
                            t.Justify();
                            t.Span($"Existen {data.PendingCount} solicitud{(data.PendingCount == 1 ? string.Empty : "es")} en plazo de apelación o con apelación en trámite; el acta definitiva se emitirá una vez resueltas.")
                                .FontColor(ProvisionalRed);
                        });
                    }
                    else
                    {
                        col.Item().Text(" ");
                    }

                    col.Item().Element(Heading("7. RESULTADO FINAL"));
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Concluida la revisión de la documentación correspondiente al personal académico titular relacionado en la presente acta, la Comisión deja constancia de los resultados consignados en los numerales precedentes, diferenciando al personal académico cuyo resultado corresponde a promoción y aquel cuyo resultado no corresponde a promoción, conforme a la documentación revisada y certificada dentro del proceso.");
                    });
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Para constancia de lo actuado, suscriben la presente acta los integrantes y responsables que intervienen en la sesión.");
                    });

                    col.Item().Element(Heading("8. FIRMAS"));

                    const float signaturesWidth = 9694;
                    col.Item().Element(c => TableFrame(c, BorderGrey, signaturesWidth)).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2494);
                            columns.RelativeColumn(5046);
                            columns.RelativeColumn(2154);
                        });

                        table.Cell().Element(c => HeaderCell(c, HeaderFillMembers, BorderGrey)).Text("NOMBRE").Bold().AlignCenter();
                        table.Cell().Element(c => HeaderCell(c, HeaderFillMembers, BorderGrey)).Text("REPRESENTACIÓN / CARGO").Bold().AlignCenter();
                        table.Cell().Element(c => HeaderCell(c, HeaderFillMembers, BorderGrey)).Text("FIRMA").Bold().AlignCenter();

                        foreach (var member in data.Members)
                        {
                            table.Cell().Element(c => BodyCell(c, BorderGrey, minHeight: 38.25f)).Text(member.FullName);
                            table.Cell().Element(c => BodyCell(c, BorderGrey, minHeight: 38.25f)).Text(member.CargoLabel);
                            table.Cell().Element(c => BodyCell(c, BorderGrey, minHeight: 38.25f)).Text("");
                        }
                    });

                    col.Item().Element(Heading("9. ANEXOS"));
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Se anexan ");
                        t.Span("________").Bold();
                        t.Span(" hojas/documentos de calificación de promoción del personal académico titular de la ");
                        t.Span(data.FacultyName).Bold();
                        t.Span(".");
                    });
                });

                page.Footer().AlignCenter().Width(504).PaddingTop(4).Row(row =>
                {
                    row.RelativeItem(6276).Text("Código: UTA-SGC-A-1-1-P1-T1").FontSize(8);
                    row.RelativeItem(3816).Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8));
                        text.Span("Página: ");
                        text.CurrentPageNumber();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void Header(IContainer container, bool isProvisional)
    {
        container.Row(row =>
        {
            row.ConstantItem(72).Height(66).Image(Logo.Value).FitArea();

            row.RelativeItem().Column(titles =>
            {
                foreach (var line in new[]
                         {
                             "UNIVERSIDAD TÉCNICA DE AMBATO",
                             "COMISIÓN ACADÉMICA DE ESCALAFÓN Y PROMOCIÓN DEL PERSONAL ACADÉMICO TITULAR",
                             "ACTA DE PROMOCIÓN DEL PERSONAL ACADÉMICO"
                         })
                {
                    titles.Item().PaddingTop(2).Text(line).Bold().FontColor(Navy).AlignCenter();
                }

                if (isProvisional)
                {
                    titles.Item().PaddingTop(4).Text("ACTA PROVISIONAL — PENDIENTE DE RESOLUCIÓN DE APELACIONES")
                        .Bold().FontColor(ProvisionalRed).AlignCenter();
                }
            });

            row.ConstantItem(72);
        });
    }

    private static Func<IContainer, IContainer> Heading(string text, string color = Navy, float topPadding = 5) =>
        container =>
        {
            container.PaddingTop(topPadding).Text(text).Bold().FontColor(color);
            return container;
        };

    private static IContainer TableFrame(IContainer container, string borderColor, float widthTwips) =>
        container
            .AlignCenter()
            .Width(Math.Min(widthTwips / 20f, ContentWidth))
            .BorderRight(BorderWidth)
            .BorderBottom(BorderWidth)
            .BorderColor(borderColor);

    private static IContainer CellBase(IContainer container, string borderColor, string? fill, float minHeight) =>
        container
            .BorderTop(BorderWidth)
            .BorderLeft(BorderWidth)
            .BorderColor(borderColor)
            .Background(fill ?? Colors.White)
            .MinHeight(minHeight)
            .PaddingVertical(3.5f)
            .PaddingHorizontal(4)
            .AlignMiddle()
            .DefaultTextStyle(x => x.FontSize(8).LineHeight(1f));

    private static IContainer HeaderCell(IContainer container, string fill, string borderColor) =>
        CellBase(container, borderColor, fill, 0);

    private static IContainer LabelCell(IContainer container, string borderColor) =>
        CellBase(container, borderColor, LabelFill, 0);

    private static IContainer BodyCell(IContainer container, string borderColor, float minHeight = 0) =>
        CellBase(container, borderColor, null, minHeight);
}
