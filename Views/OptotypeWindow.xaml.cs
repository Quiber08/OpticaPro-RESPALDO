using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;

namespace OpticaPro.Views
{
    public class OptotypeChar
    {
        public string Character { get; set; }
        public int Angle { get; set; }
    }

    public sealed partial class OptotypeWindow : Window
    {
        private readonly List<string> _acuities = new List<string>
        {
            "20/200", "20/100", "20/70", "20/50", "20/40", "20/30", "20/25", "20/20", "20/15", "20/10"
        };

        // 6 METROS
        private readonly double[] _sizes6m = { 600, 300, 210, 150, 120, 90, 75, 60, 45, 30 };
        // 3 METROS
        private readonly double[] _sizes3m = { 300, 150, 105, 75, 60, 45, 37.5, 30, 22.5, 15 };

        private int _currentIndex = 0;
        private readonly Random _random = new Random();
        private string _mode = "Letras";

        public OptotypeWindow()
        {
            this.InitializeComponent();

            if (this.Content != null)
            {
                this.Content.KeyDown += OnKeyDown;
            }

            // --- CAMBIO: MAXIMIZAR AUTOMÁTICAMENTE AL INICIAR ---
            if (this.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (LblAcuity == null || MainViewbox == null || CmbDistance == null) return;

            LblAcuity.Text = _acuities[_currentIndex];

            double[] currentSizes = (CmbDistance.SelectedIndex == 0) ? _sizes6m : _sizes3m;

            if (_currentIndex < currentSizes.Length)
            {
                MainViewbox.Height = currentSizes[_currentIndex];
                MainViewbox.Margin = new Thickness(0);
            }

            GenerateRandomContent();
        }

        private void GenerateRandomContent()
        {
            if (OptotypeList == null) return;

            int count = 1;
            if (_currentIndex > 2) count = 2;
            if (_currentIndex > 4) count = 3;
            if (_currentIndex > 6) count = 4;
            if (_currentIndex > 8) count = 5;

            var items = new List<OptotypeChar>();

            if (_mode == "E" || _mode == "E Direccional")
            {
                int[] angles = { 0, 90, 180, 270 };
                for (int i = 0; i < count; i++)
                {
                    items.Add(new OptotypeChar
                    {
                        Character = "E",
                        Angle = angles[_random.Next(angles.Length)]
                    });
                }
            }
            else if (_mode == "Numeros" || _mode == "Números")
            {
                string nums = "35689247";
                for (int i = 0; i < count; i++)
                {
                    items.Add(new OptotypeChar
                    {
                        Character = nums[_random.Next(nums.Length)].ToString(),
                        Angle = 0
                    });
                }
            }
            // --- NUEVO MODO: FIGURAS ---
            else if (_mode == "Figuras")
            {
                // Usamos símbolos geométricos sólidos y claros
                // Círculo, Cuadrado, Estrella, Corazón, Triángulo
                string[] shapes = { "●", "■", "★", "♥", "▲" };

                for (int i = 0; i < count; i++)
                {
                    items.Add(new OptotypeChar
                    {
                        Character = shapes[_random.Next(shapes.Length)],
                        Angle = 0
                    });
                }
            }
            else // Default: Letras
            {
                string chars = "CDEFLOPTZHVNRKS";
                for (int i = 0; i < count; i++)
                {
                    items.Add(new OptotypeChar
                    {
                        Character = chars[_random.Next(chars.Length)].ToString(),
                        Angle = 0
                    });
                }
            }

            OptotypeList.ItemsSource = items;
        }

        private void CmbDistance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _acuities.Count - 1)
            {
                _currentIndex++;
                UpdateDisplay();
            }
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                UpdateDisplay();
            }
        }

        private void BtnRandom_Click(object sender, RoutedEventArgs e) => GenerateRandomContent();

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbType.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                // Toma la primera palabra: "Figuras", "Letras", "Números"
                _mode = item.Content.ToString().Split(' ')[0];
                GenerateRandomContent();
            }
        }

        private void TglDuochrome_Toggled(object sender, RoutedEventArgs e)
        {
            if (DuochromeLayer == null) return;
            var toggleBtn = sender as Microsoft.UI.Xaml.Controls.Primitives.ToggleButton;
            DuochromeLayer.Visibility = (toggleBtn != null && toggleBtn.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (this.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State == OverlappedPresenterState.Maximized)
                    presenter.Restore();
                else
                    presenter.Maximize();
            }
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Up:
                    BtnZoomIn_Click(null, null);
                    break;
                case Windows.System.VirtualKey.Down:
                    BtnZoomOut_Click(null, null);
                    break;
                case Windows.System.VirtualKey.Space:
                    GenerateRandomContent();
                    break;
                case Windows.System.VirtualKey.F11:
                    BtnFullscreen_Click(null, null);
                    break;
            }
        }
    }
}