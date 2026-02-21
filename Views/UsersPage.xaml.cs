using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpticaPro.Models;
using OpticaPro.Services;
using Supabase.Gotrue;
using System;
using System.Linq;

namespace OpticaPro.Views
{
    public sealed partial class UsersPage : Page
    {
        public UsersPage()
        {
            this.InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            var users = UserRepository.GetAllUsers();
            UsersList.ItemsSource = users;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack) Frame.GoBack();
        }

        // --- AGREGAR USUARIO ---
        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            // Creamos los inputs
            var nameInput = new TextBox { Header = "Nombre Completo", PlaceholderText = "Ej. Ana López" };
            var userInput = new TextBox { Header = "Usuario", PlaceholderText = "Ej. alopez" };
            var passInput = new PasswordBox { Header = "Contraseña" };

            var roleCombo = new ComboBox { Header = "Rol", HorizontalAlignment = HorizontalAlignment.Stretch };
            roleCombo.Items.Add("Vendedor");
            roleCombo.Items.Add("Optometrista");
            roleCombo.Items.Add("Admin");
            roleCombo.SelectedIndex = 0;

            var stack = new StackPanel { Spacing = 12 };
            stack.Children.Add(nameInput);
            stack.Children.Add(userInput);
            stack.Children.Add(passInput);
            stack.Children.Add(roleCombo);

            var dialog = new ContentDialog
            {
                Title = "Nuevo Usuario",
                Content = stack,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(userInput.Text) || string.IsNullOrWhiteSpace(passInput.Password))
                {
                    ShowMessage("Error", "Usuario y contraseña son obligatorios.");
                    return;
                }

                var newUser = new AppUser
                {
                    FullName = nameInput.Text,
                    Username = userInput.Text,
                    Password = passInput.Password,
                    Role = roleCombo.SelectedItem?.ToString() ?? "Vendedor"
                };

                UserRepository.AddUser(newUser);
                LoadUsers(); // Recargar lista
            }
        }

        // --- EDITAR USUARIO ---
        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var user = btn.Tag as AppUser;
            if (user == null) return;

            var nameInput = new TextBox { Header = "Nombre Completo", Text = user.FullName };
            var userInput = new TextBox { Header = "Usuario", Text = user.Username, IsReadOnly = true }; // Usuario no editable
            var passInput = new PasswordBox { Header = "Nueva Contraseña (Dejar vacío para no cambiar)" };

            var roleCombo = new ComboBox { Header = "Rol", HorizontalAlignment = HorizontalAlignment.Stretch };
            roleCombo.Items.Add("Vendedor");
            roleCombo.Items.Add("Optometrista");
            roleCombo.Items.Add("Admin");
            roleCombo.SelectedItem = user.Role;

            var stack = new StackPanel { Spacing = 12 };
            stack.Children.Add(nameInput);
            stack.Children.Add(userInput);
            stack.Children.Add(passInput);
            stack.Children.Add(roleCombo);

            var dialog = new ContentDialog
            {
                Title = "Editar Usuario",
                Content = stack,
                PrimaryButtonText = "Actualizar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                user.FullName = nameInput.Text;
                user.Role = roleCombo.SelectedItem?.ToString();

                if (!string.IsNullOrEmpty(passInput.Password))
                {
                    user.Password = passInput.Password;
                }

                UserRepository.UpdateUser(user);
                LoadUsers();
            }
        }

        // --- ELIMINAR USUARIO ---
        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var user = btn.Tag as AppUser;
            if (user == null) return;

            var dialog = new ContentDialog
            {
                Title = "Eliminar Usuario",
                Content = $"¿Estás seguro de que quieres eliminar a {user.FullName}?\nEsta acción es irreversible.",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close, // El botón por defecto es cancelar para evitar accidentes
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                UserRepository.DeleteUser(user.Id);
                LoadUsers();
            }
        }

        private async void ShowMessage(string title, string content)
        {
            await new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Ok",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
    }
}