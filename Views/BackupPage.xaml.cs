using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Necesario para .ToList()
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OpticaPro.Views
{
    public class BackupDataModel
    {
        public DateTime CreatedAt { get; set; }
        public string Version { get; set; } = "3.1-SQLite";
        public List<Patient> Patients { get; set; } = new List<Patient>();
        public List<Product> Inventory { get; set; } = new List<Product>();
    }

    public sealed partial class BackupPage : Page
    {
        public BackupPage()
        {
            this.InitializeComponent();
        }

        private void Log(string message)
        {
            TxtLog.Text = $"[{DateTime.Now:HH:mm:ss}] {message}\n" + TxtLog.Text;
        }

        // --- 1. GENERAR RESPALDO ---
        private async void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnBackup.IsEnabled = false;
                Log("Recopilando datos...");

                var patients = PatientRepository.GetAllPatients();
                var products = InventoryRepository.GetAllProducts();

                Log($"Datos encontrados: {patients.Count} pacientes, {products.Count} productos.");

                var backup = new BackupDataModel
                {
                    CreatedAt = DateTime.Now,
                    Patients = patients,
                    Inventory = products
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(backup, options);

                if (ChkEncrypt.IsChecked == true)
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                    jsonContent = Convert.ToBase64String(bytes);
                }

                var savePicker = new FileSavePicker();
                InitializePicker(savePicker);
                savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                savePicker.FileTypeChoices.Add("Archivo JSON", new List<string>() { ".json" });
                savePicker.SuggestedFileName = $"Respaldo_Optica_{DateTime.Now:yyyy-MM-dd}";

                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    // Guardado directo y seguro
                    await FileIO.WriteTextAsync(file, jsonContent);
                    Log($"¡Respaldo guardado en: {file.Name}!");
                    ShowMessage("Éxito", "Copia de seguridad creada correctamente.");
                }
                else
                {
                    Log("Cancelado por el usuario.");
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                if (ex.Message.Contains("SolidColorBrush"))
                    ShowMessage("Error de Formato", "Falta [JsonIgnore] en Models/Patient.cs");
                else
                    ShowMessage("Error", ex.Message);
            }
            finally
            {
                BtnBackup.IsEnabled = true;
            }
        }

        // --- 2. RESTAURAR (CORREGIDO PARA TU INVENTORY REPOSITORY) ---
        private async void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openPicker = new FileOpenPicker();
                InitializePicker(openPicker);
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add(".json");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "⚠️ Peligro",
                        Content = "Se borrarán los datos actuales y se reemplazarán con el respaldo. ¿Confirmar?",
                        PrimaryButtonText = "Sí, Restaurar",
                        CloseButtonText = "Cancelar",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };

                    if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

                    BtnRestore.IsEnabled = false;
                    Log("Leyendo archivo...");

                    string fileContent = await FileIO.ReadTextAsync(file);

                    // Descifrar si es necesario
                    if (!fileContent.TrimStart().StartsWith("{"))
                    {
                        try
                        {
                            var bytes = Convert.FromBase64String(fileContent);
                            fileContent = System.Text.Encoding.UTF8.GetString(bytes);
                            Log("Descifrado correctamente.");
                        }
                        catch { }
                    }

                    var backup = JsonSerializer.Deserialize<BackupDataModel>(fileContent);

                    if (backup != null)
                    {
                        // A. RESTAURAR PACIENTES (SQLite)
                        var db = DatabaseService.GetConnection();
                        db.DeleteAll<Patient>();
                        db.InsertAll(backup.Patients);
                        Log($"Restaurados {backup.Patients.Count} pacientes.");

                        // B. RESTAURAR INVENTARIO (SOLUCIÓN AL ERROR CS0117)
                        // Como InventoryRepository no tiene SaveProducts, lo hacemos manualmente:

                        // 1. Borrar actuales (Hacemos una copia de la lista para poder iterar y borrar)
                        var actuales = InventoryRepository.GetAllProducts().ToList();
                        foreach (var p in actuales)
                        {
                            InventoryRepository.DeleteProduct(p);
                        }

                        // 2. Agregar los del respaldo uno por uno
                        foreach (var p in backup.Inventory)
                        {
                            InventoryRepository.AddProduct(p);
                        }

                        Log($"Restaurados {backup.Inventory.Count} productos.");
                        ShowMessage("Restauración Completa", "El sistema ha sido actualizado.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR RESTAURANDO: {ex.Message}");
                ShowMessage("Error", ex.Message);
            }
            finally
            {
                BtnRestore.IsEnabled = true;
            }
        }

        private void InitializePicker(object picker)
        {
            var window = MainWindow.Current;
            var hWnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hWnd);
        }

        private async void ShowMessage(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Ok",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}