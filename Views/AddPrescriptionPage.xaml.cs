using Microsoft.UI; // Para la clase Colors
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media; // Para SolidColorBrush
using System;

namespace OpticaPro.Views
{
    public sealed partial class AddPrescriptionPage : Page
    {
        public AddPrescriptionPage()
        {
            this.InitializeComponent();
        }

        // --- BOTÓN: GENERAR CERTIFICADO ---
        private async void PrintCertificate_Click(object sender, RoutedEventArgs e)
        {
            // 1. Recopilar datos del formulario
            string paciente = TxtPatientName.Text;
            if (string.IsNullOrEmpty(paciente)) paciente = "Paciente Sin Nombre";

            string fecha = DateTime.Now.ToString("dd/MM/yyyy");

            // 2. Construir el diseño visual del certificado
            StackPanel certPanel = new StackPanel { Spacing = 12, Width = 450, Padding = new Thickness(10) };

            // > Encabezado con Logo
            StackPanel header = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
            header.Children.Add(new FontIcon { Glyph = "\uE719", FontSize = 32, Foreground = new SolidColorBrush(Colors.DarkBlue) });
            header.Children.Add(new TextBlock { Text = "OPTICA PRO", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            header.Children.Add(new TextBlock { Text = "Certificado Visual", FontSize = 16, Foreground = new SolidColorBrush(Colors.Gray), HorizontalAlignment = HorizontalAlignment.Center });
            certPanel.Children.Add(header);

            certPanel.Children.Add(new MenuFlyoutSeparator { Margin = new Thickness(0, 10, 0, 10) });

            // > Datos Paciente
            certPanel.Children.Add(new TextBlock { Text = $"PACIENTE: {paciente.ToUpper()}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            certPanel.Children.Add(new TextBlock { Text = $"FECHA: {fecha}" });

            // > Tabla de Medidas (Grid)
            Grid grid = new Grid { ColumnSpacing = 10, RowSpacing = 8, Margin = new Thickness(0, 15, 0, 15), BorderBrush = new SolidColorBrush(Colors.LightGray), BorderThickness = new Thickness(1), Padding = new Thickness(10), CornerRadius = new CornerRadius(4) };

            for (int i = 0; i < 5; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition()); // Header
            grid.RowDefinitions.Add(new RowDefinition()); // OD
            grid.RowDefinitions.Add(new RowDefinition()); // OI

            // Llenar tabla
            AddCell(grid, "OJO", 0, 0, true);
            AddCell(grid, "SPH", 0, 1, true);
            AddCell(grid, "CYL", 0, 2, true);
            AddCell(grid, "EJE", 0, 3, true);
            AddCell(grid, "A.V.", 0, 4, true);

            // OD
            AddCell(grid, "OD", 1, 0, true, Colors.DarkBlue);
            AddCell(grid, TxtOdSph.Text, 1, 1);
            AddCell(grid, TxtOdCyl.Text, 1, 2);
            AddCell(grid, TxtOdAxis.Text, 1, 3);
            AddCell(grid, TxtOdAv.Text, 1, 4);

            // OI
            AddCell(grid, "OI", 2, 0, true, Colors.DarkBlue);
            AddCell(grid, TxtOiSph.Text, 2, 1);
            AddCell(grid, TxtOiCyl.Text, 2, 2);
            AddCell(grid, TxtOiAxis.Text, 2, 3);
            AddCell(grid, TxtOiAv.Text, 2, 4);

            certPanel.Children.Add(grid);

            // > Notas y DIP
            if (!string.IsNullOrEmpty(TxtDip.Text))
                certPanel.Children.Add(new TextBlock { Text = $"DIP: {TxtDip.Text} mm | ADD: {TxtAdd.Text}" });

            if (!string.IsNullOrEmpty(TxtNotes.Text))
            {
                // CORRECCIÓN AQUÍ: Usamos Windows.UI.Text.FontStyle.Italic
                certPanel.Children.Add(new TextBlock
                {
                    Text = $"Observaciones: {TxtNotes.Text}",
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                    Foreground = new SolidColorBrush(Colors.Gray)
                });
            }

            // > Firma
            certPanel.Children.Add(new TextBlock { Text = "_______________________", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0) });
            certPanel.Children.Add(new TextBlock { Text = "Dr. Quiber - Optometrista", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 11 });


            // 3. Mostrar Diálogo
            ContentDialog dialog = new ContentDialog
            {
                Title = "Vista Previa",
                Content = new ScrollViewer { Content = certPanel },
                PrimaryButtonText = "Imprimir",
                CloseButtonText = "Cerrar",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void AddCell(Grid g, string text, int row, int col, bool bold = false, Windows.UI.Color? color = null)
        {
            TextBlock tb = new TextBlock
            {
                Text = string.IsNullOrEmpty(text) ? "-" : text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (bold) tb.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
            if (color.HasValue) tb.Foreground = new SolidColorBrush(color.Value);

            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await new ContentDialog { Title = "Guardado", Content = "Receta guardada en historial.", CloseButtonText = "Ok", XamlRoot = this.XamlRoot }.ShowAsync();
            if (Frame.CanGoBack) Frame.GoBack();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack) Frame.GoBack();
        }
    }
}