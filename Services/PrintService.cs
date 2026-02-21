using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OpticaPro.Models;
using OpticaPro.Views;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpticaPro.Services
{
    public class PrintService
    {
        public PrintService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ==========================================
        // 1. GENERAR CERTIFICADO
        // ==========================================
        public void GenerateCertificate(CertificateData data, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                    // Encabezado
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(data.ClinicName ?? "OPTICA PRO").FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Text(data.ClinicAddress ?? "").FontSize(10);
                            col.Item().Text($"Telf: {data.ClinicPhone} | RUC: {data.ClinicRuc}").FontSize(10);
                            col.Item().PaddingTop(10).Text("CERTIFICADO OPTOMÉTRICO").FontSize(16).Bold().AlignCenter().FontColor(Colors.Black);
                        });
                    });

                    // Contenido
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Datos Paciente
                        col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(txt => { txt.Span("Paciente: ").Bold(); txt.Span(data.PatientName); });
                                c.Item().Text(txt => { txt.Span("Edad: ").Bold(); txt.Span(data.PatientAge + " años"); });
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text(txt => { txt.Span("Fecha: ").Bold(); txt.Span(data.Date); });
                            });
                        });

                        col.Item().Height(20);

                        // Tabla
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyleHeader).Text("OJO");
                                header.Cell().Element(CellStyleHeader).Text("ESFERA");
                                header.Cell().Element(CellStyleHeader).Text("CILINDRO");
                                header.Cell().Element(CellStyleHeader).Text("EJE");
                                header.Cell().Element(CellStyleHeader).Text("ADD");
                                header.Cell().Element(CellStyleHeader).Text("A.V.");
                            });

                            // OD
                            table.Cell().Element(CellStyleMain).Text("OD").Bold();
                            table.Cell().Element(CellStyle).Text(data.SphereOD);
                            table.Cell().Element(CellStyle).Text(data.CylOD);
                            table.Cell().Element(CellStyle).Text(data.AxisOD);
                            table.Cell().Element(CellStyle).Text(data.AddOD);
                            table.Cell().Element(CellStyle).Text(data.AvOD);

                            // OI
                            table.Cell().Element(CellStyleMain).Text("OI").Bold();
                            table.Cell().Element(CellStyle).Text(data.SphereOI);
                            table.Cell().Element(CellStyle).Text(data.CylOI);
                            table.Cell().Element(CellStyle).Text(data.AxisOI);
                            table.Cell().Element(CellStyle).Text(data.AddOI);
                            table.Cell().Element(CellStyle).Text(data.AvOI);
                        });

                        col.Item().Height(25);

                        // Diagnóstico
                        col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Diagnóstico:").Bold();
                            c.Item().Text(data.Diagnosis).FontSize(10);
                            c.Item().Height(10);
                            c.Item().Text("Recomendaciones:").Bold();
                            c.Item().Text(data.Recommendation).FontSize(10);
                        });
                    });

                    // Footer
                    page.Footer().Column(col =>
                    {
                        col.Item().AlignRight().Width(200).Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor(Colors.Black);
                            c.Item().AlignCenter().Text(data.DoctorName).Bold();
                            c.Item().AlignCenter().Text(data.DoctorSpecialty).FontSize(9);
                            c.Item().AlignCenter().Text($"Lic: {data.DoctorLicense}").FontSize(9);
                        });
                        col.Item().PaddingTop(30).AlignCenter().Text("Generado por OpticaPro").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        // ==========================================
        // 2. GENERAR HISTORIAL
        // ==========================================
        public void GenerateHistory(Patient patient, List<ClinicalExam> exams, List<Order> orders, string filePath)
        {
            var settings = SettingsService.Current;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    // --- Encabezado ---
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(settings.ClinicName ?? "HISTORIAL CLÍNICO").FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().Text(settings.ClinicAddress ?? "Dirección no registrada");
                            col.Item().Text($"Telf: {settings.ClinicPhone}");
                        });
                        row.ConstantItem(150).AlignRight().Column(col =>
                        {
                            col.Item().Text("HISTORIAL PACIENTE").FontSize(12).Bold();
                            col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                        });
                    });

                    // --- Contenido ---
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Datos Paciente
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(txt => { txt.Span("Paciente: ").Bold(); txt.Span(patient.FullName.ToUpper()); });
                                c.Item().Text(txt => { txt.Span("DNI: ").Bold(); txt.Span(patient.Dni); });
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(txt => { txt.Span("Edad: ").Bold(); txt.Span($"{patient.Age} años"); });
                                c.Item().Text(txt => { txt.Span("Teléfono: ").Bold(); txt.Span(patient.Phone); });
                            });
                        });

                        col.Item().Height(15);

                        // --- Sección Exámenes ---
                        col.Item().Text("REGISTRO DE CONSULTAS Y EXÁMENES").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        if (exams != null && exams.Any())
                        {
                            foreach (var exam in exams)
                            {
                                col.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                {
                                    c.Item().Background(Colors.Grey.Lighten4).Padding(5).Text($"📅 Consulta: {exam.Date}").Bold();

                                    // RX Final
                                    c.Item().PaddingTop(5).Text("Refracción Final:").Bold().FontSize(9);
                                    c.Item().Table(t =>
                                    {
                                        t.ColumnsDefinition(cd => { cd.ConstantColumn(30); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });

                                        // Header simple
                                        t.Header(h => { h.Cell().Text(""); h.Cell().Text("ESF"); h.Cell().Text("CIL"); h.Cell().Text("EJE"); h.Cell().Text("ADD"); h.Cell().Text("AV"); });

                                        // OD
                                        t.Cell().Text("OD").Bold();
                                        t.Cell().Text(exam.SphereOD); t.Cell().Text(exam.CylOD); t.Cell().Text(exam.AxisOD); t.Cell().Text(exam.AddOD); t.Cell().Text(exam.AvOD);

                                        // OI
                                        t.Cell().Text("OI").Bold();
                                        t.Cell().Text(exam.SphereOI); t.Cell().Text(exam.CylOI); t.Cell().Text(exam.AxisOI); t.Cell().Text(exam.AddOI); t.Cell().Text(exam.AvOI);
                                    });

                                    if (!string.IsNullOrEmpty(exam.DiagnosticoResumen))
                                        c.Item().PaddingTop(5).Text($"Diagnóstico: {exam.DiagnosticoResumen}");
                                });
                            }
                        }
                        else
                        {
                            col.Item().PaddingTop(10).Text("No hay exámenes registrados.");
                        }

                        col.Item().Height(20);

                        // --- Sección Pedidos ---
                        col.Item().Text("HISTORIAL DE PEDIDOS").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        if (orders != null && orders.Any())
                        {
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(2); c.RelativeColumn(); c.RelativeColumn(); });

                                table.Header(h =>
                                {
                                    h.Cell().Element(CellStyleHeader).Text("FECHA");
                                    h.Cell().Element(CellStyleHeader).Text("DETALLE");
                                    h.Cell().Element(CellStyleHeader).Text("MONTO");
                                    h.Cell().Element(CellStyleHeader).Text("ESTADO");
                                });

                                foreach (var order in orders)
                                {
                                    table.Cell().Element(CellStyle).Text(order.Date.ToString());
                                    table.Cell().Element(CellStyle).Text($"{order.FrameModel} - {order.LensType}");
                                    table.Cell().Element(CellStyle).Text($"{order.TotalAmount:C2}");
                                    table.Cell().Element(CellStyle).Text(order.Status);
                                }
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(10).Text("No hay pedidos registrados.");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        // ==========================================
        // 3. GENERAR REPORTE FINANCIERO (CORREGIDO)
        // ==========================================
        public void GenerateFinancialReport(string filePath, string periodo, DateTime fecha, List<ReportRow> movimientos, decimal totalIngreso, decimal totalGasto, decimal utilidad)
        {
            var settings = SettingsService.Current;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    // 1. ENCABEZADO
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(settings.ClinicName ?? "OPTICA PRO").FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().Text("REPORTE FINANCIERO").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(150).AlignRight().Column(col =>
                        {
                            col.Item().Text(periodo.ToUpper()).FontSize(12).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy}");
                        });
                    });

                    // 2. CONTENIDO
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // --- A. TARJETAS DE RESUMEN (KPIs) ---
                        col.Item().Row(row =>
                        {
                            // AQUI ESTABA EL ERROR: Ahora pasamos objetos Color, no strings
                            row.RelativeItem().Component(new KpiCard("INGRESOS TOTALES", totalIngreso, Colors.Green.Medium));
                            row.ConstantItem(10);
                            row.RelativeItem().Component(new KpiCard("GASTOS TOTALES", totalGasto, Colors.Orange.Medium));
                            row.ConstantItem(10);
                            row.RelativeItem().Component(new KpiCard("UTILIDAD NETA", utilidad, utilidad >= 0 ? Colors.Blue.Medium : Colors.Red.Medium));
                        });

                        col.Item().Height(20);

                        // --- B. GRÁFICO DE BARRAS ---
                        var datosGrafico = movimientos
                            .Where(m => m.Ingreso > 0)
                            .GroupBy(m => m.Fecha)
                            .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Ingreso) })
                            .OrderBy(x => x.Fecha)
                            .Take(15)
                            .ToList();

                        if (datosGrafico.Any())
                        {
                            col.Item().Text("TENDENCIA DE INGRESOS").FontSize(11).Bold();

                            float graphHeight = 120f;

                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Height(graphHeight + 30).Row(chartRow =>
                            {
                                decimal maxVal = datosGrafico.Max(g => g.Total);
                                if (maxVal == 0) maxVal = 1;

                                foreach (var g in datosGrafico)
                                {
                                    float percent = (float)(g.Total / maxVal);
                                    float barHeight = graphHeight * percent;
                                    float emptyHeight = graphHeight - barHeight;

                                    chartRow.RelativeItem().PaddingRight(4).Column(c =>
                                    {
                                        if (emptyHeight > 0)
                                            c.Item().Height(emptyHeight);

                                        c.Item().Height(barHeight)
                                            .Background(Colors.Blue.Lighten2)
                                            .Border(1).BorderColor(Colors.Blue.Medium);

                                        string label = g.Fecha.Length >= 5 ? g.Fecha.Substring(0, 5) : g.Fecha;
                                        c.Item().Height(15).AlignCenter().Text(label).FontSize(7);
                                    });
                                }
                            });
                            col.Item().Height(20);
                        }

                        // --- C. TABLA DETALLADA ---
                        col.Item().Text("DETALLE DE MOVIMIENTOS").FontSize(12).Bold();
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyleHeader).Text("FECHA");
                                header.Cell().Element(CellStyleHeader).Text("CLIENTE / DETALLE");
                                header.Cell().Element(CellStyleHeader).Text("TIPO");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("INGRESO");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("GASTO");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("NETO");
                            });

                            foreach (var mov in movimientos)
                            {
                                table.Cell().Element(CellStyle).Text(mov.Fecha).FontSize(9);
                                table.Cell().Element(CellStyle).Text(mov.Detalle).FontSize(9).SemiBold();
                                table.Cell().Element(CellStyle).Text(mov.Tipo).FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).AlignRight().Text(mov.IngresoDisplay).FontColor(Colors.Green.Darken2).FontSize(9);
                                table.Cell().Element(CellStyle).AlignRight().Text(mov.GastoDisplay).FontColor(Colors.Orange.Darken2).FontSize(9);
                                table.Cell().Element(CellStyle).AlignRight().Text(mov.NetoDisplay).Bold().FontSize(9);
                            }
                        });
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text(x =>
                        {
                            x.Span("Generado por OpticaPro - ");
                            x.Span(DateTime.Now.ToString("g"));
                        });
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                        });
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        // --- CLASE KPICARD CORREGIDA (RAÍZ DEL PROBLEMA DE TIPOS) ---
        private class KpiCard : IComponent
        {
            private string Title;
            private decimal Value;
            private Color ColorInfo; // CORRECCIÓN: Usamos Color (objeto), no string

            public KpiCard(string title, decimal value, Color color)
            {
                Title = title;
                Value = value;
                ColorInfo = color;
            }

            public void Compose(IContainer container)
            {
                container
                    .Border(1)
                    .BorderColor(ColorInfo) // Usamos el objeto Color directamente
                    .Background(Colors.White)
                    .Padding(8)
                    .Column(column =>
                    {
                        column.Item().Text(Title).FontSize(9).FontColor(Colors.Grey.Darken1);
                        column.Item().Text($"{Value:C2}").FontSize(14).Bold().FontColor(ColorInfo);
                    });
            }
        }

        // Estilos Auxiliares
        static IContainer CellStyleHeader(IContainer container) => container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten3)
            .Padding(5)
            .DefaultTextStyle(x => x.Bold());

        static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        static IContainer CellStyleMain(IContainer container) => container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(5).AlignCenter().AlignMiddle();
    }
}