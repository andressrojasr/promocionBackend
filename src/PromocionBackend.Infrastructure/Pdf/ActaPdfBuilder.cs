using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.DTOs.Actas;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PromocionBackend.Infrastructure.Pdf;

/// <summary>
/// Maqueta el acta de promoción del personal académico (formato oficial UTA) en PDF,
/// a partir de datos ya resueltos por ActaService. Solo se llenan los campos de los
/// que el sistema tiene información (cédula, nombres, categoría resultante y
/// observación); RMU, tiempo de dedicación y firmas quedan en blanco.
/// </summary>
public class ActaPdfBuilder : IActaPdfBuilder
{
    public byte[] Build(ActaData data)
    {
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("UNIVERSIDAD TÉCNICA DE AMBATO").Bold().FontSize(13);
                    col.Item().AlignCenter().Text("COMISIÓN ACADÉMICA DE ESCALAFÓN Y PROMOCIÓN DEL PERSONAL ACADÉMICO TITULAR").FontSize(9);
                    col.Item().PaddingTop(6).AlignCenter().Text("ACTA DE PROMOCIÓN DEL PERSONAL ACADÉMICO").Bold().FontSize(12);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(6);

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

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("N°");
                            header.Cell().Element(HeaderCell).Text("NOMBRE");
                            header.Cell().Element(HeaderCell).Text("REPRESENTACIÓN / CARGO EN LA COMISIÓN");
                        });

                        var index = 1;
                        foreach (var member in data.Members)
                        {
                            table.Cell().Element(BodyCell).Text((index++).ToString());
                            table.Cell().Element(BodyCell).Text(member.FullName);
                            table.Cell().Element(BodyCell).Text(member.CargoLabel);
                        }
                    });

                    col.Item().PaddingTop(4).Text("1. OBJETO Y FINALIDAD DEL ACTA").Bold();
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("La presente acta tiene por objeto dejar constancia de las actuaciones de la Comisión Académica de Escalafón y Promoción del Personal Académico Titular respecto del conocimiento, análisis y resolución de las solicitudes de promoción presentadas por el personal académico titular, con base en los documentos habilitantes registrados en la plataforma institucional y en los requisitos generales y específicos aplicables al grado escalafonario correspondiente. Los resultados consignados constituirán sustento para el informe de promoción que la Comisión presente al Honorable Consejo Universitario para su conocimiento y aprobación.");
                    });

                    col.Item().PaddingTop(2).Text("2. ANTECEDENTES Y DOCUMENTACIÓN OBJETO DE REVISIÓN").Bold();
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Para el desarrollo de la sesión se consideran los antecedentes y documentos institucionales vinculados al proceso de promoción, conforme a la normativa aplicable.");
                    });

                    col.Item().Text(t => { t.Span("Facultad: ").Bold(); t.Span(data.FacultyName); });
                    col.Item().Text(t => { t.Span("Categoría o nivel escalafonario de origen: ").Bold(); t.Span(data.OriginCategoryLabel ?? "[POR COMPLETAR]"); });
                    col.Item().Text(t => { t.Span("Categoría o nivel escalafonario objeto de promoción: ").Bold(); t.Span(data.DestinationCategoryLabel ?? "[POR COMPLETAR]"); });

                    col.Item().PaddingTop(2).Text("3. PERSONAL ACADÉMICO SUJETO A REVISIÓN").Bold();
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Se deja constancia de las solicitudes de promoción y de la documentación habilitante registrada en la plataforma institucional dentro de los plazos establecidos en la convocatoria, que son sometidas al conocimiento y análisis de la Comisión:");
                    });

                    if (data.ReviewedTeacherNames.Count == 0)
                    {
                        col.Item().Text("- [SIN POSTULANTES REGISTRADOS]");
                    }
                    else
                    {
                        foreach (var name in data.ReviewedTeacherNames)
                        {
                            col.Item().Text($"- {name}");
                        }
                    }

                    col.Item().PaddingTop(2).Text("4. REVISIÓN DE LA DOCUMENTACIÓN Y REQUISITOS").Bold();
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("La Comisión conoce y analiza cada solicitud considerando las disposiciones generales para la promoción, los requisitos generales, los requisitos específicos correspondientes al grado escalafonario al que se postula, las equivalencias y excepcionalidades que resulten aplicables, así como la validez de los documentos habilitantes presentados en formato digital a través de la plataforma institucional.");
                    });

                    col.Item().PaddingTop(2).Text("5. RESULTADOS DEL PROCESO").Bold();
                    col.Item().Text("5.1. Personal académico que se promociona").Bold();
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("Concluido el análisis, la Comisión deja constancia del resultado de las solicitudes de promoción. La promoción no modificará el tiempo de dedicación del personal académico.");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("CÉDULA");
                            header.Cell().Element(HeaderCell).Text("APELLIDOS Y NOMBRES");
                            header.Cell().Element(HeaderCell).Text("CATEGORÍA Y NIVEL ESCALAFONARIO RESULTANTE");
                            header.Cell().Element(HeaderCell).Text("RMU");
                            header.Cell().Element(HeaderCell).Text("TIEMPO DE DEDICACIÓN");
                            header.Cell().Element(HeaderCell).Text("OBSERVACIÓN");
                        });

                        if (data.Approved.Count == 0)
                        {
                            table.Cell().ColumnSpan(6).Element(BodyCell).Text("[SIN REGISTROS]");
                        }

                        foreach (var row in data.Approved)
                        {
                            table.Cell().Element(BodyCell).Text(row.Identification);
                            table.Cell().Element(BodyCell).Text(row.FullName);
                            table.Cell().Element(BodyCell).Text(row.ResultingCategoryLabel);
                            table.Cell().Element(BodyCell).Text("");
                            table.Cell().Element(BodyCell).Text("");
                            table.Cell().Element(BodyCell).Text(row.Observation ?? "");
                        }
                    });

                    col.Item().PaddingTop(2).Text("5.2. Personal académico que no se promociona").Bold();
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span("De conformidad con la documentación revisada, se deja constancia del personal académico cuyo resultado no corresponde a promoción:");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("CÉDULA");
                            header.Cell().Element(HeaderCell).Text("APELLIDOS Y NOMBRES");
                            header.Cell().Element(HeaderCell).Text("OBSERVACIÓN");
                        });

                        if (data.Rejected.Count == 0)
                        {
                            table.Cell().ColumnSpan(3).Element(BodyCell).Text("[SIN REGISTROS]");
                        }

                        foreach (var row in data.Rejected)
                        {
                            table.Cell().Element(BodyCell).Text(row.Identification);
                            table.Cell().Element(BodyCell).Text(row.FullName);
                            table.Cell().Element(BodyCell).Text(row.Observation ?? "");
                        }
                    });

                    col.Item().PaddingTop(2).Text("6. OBSERVACIONES").Bold();
                    col.Item().Text(" ");

                    col.Item().PaddingTop(2).Text("7. RESULTADO FINAL").Bold();
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

                    col.Item().PaddingTop(2).Text("8. FIRMAS").Bold();

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("NOMBRE");
                            header.Cell().Element(HeaderCell).Text("REPRESENTACIÓN / CARGO");
                            header.Cell().Element(HeaderCell).Text("FIRMA");
                        });

                        foreach (var member in data.Members)
                        {
                            table.Cell().Element(SignatureCell).Text(member.FullName);
                            table.Cell().Element(SignatureCell).Text(member.CargoLabel);
                            table.Cell().Element(SignatureCell).Text("");
                        }
                    });

                    col.Item().PaddingTop(2).Text("9. ANEXOS").Bold();
                    col.Item().Text(t =>
                    {
                        t.Justify();
                        t.Span($"Se anexan las hojas/documentos de calificación de promoción del personal académico titular de la {data.FacultyName}.");
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten2).Border(0.5f).Padding(4).DefaultTextStyle(x => x.Bold().FontSize(7));

    private static IContainer BodyCell(IContainer container) =>
        container.Border(0.5f).Padding(4).DefaultTextStyle(x => x.FontSize(7));

    private static IContainer SignatureCell(IContainer container) =>
        container.Border(0.5f).Padding(4).MinHeight(35).DefaultTextStyle(x => x.FontSize(7));
}
