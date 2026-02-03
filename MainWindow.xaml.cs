using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using colizeumUpdateManager.Models;
using colizeumUpdateManager.ViewModels;

namespace colizeumUpdateManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;
            Loaded += (_, __) => vm.Load();
        }

        // Drag window
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e)
            => ToggleMaximize();

        private void Close_Click(object sender, RoutedEventArgs e)
            => Close();

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private async void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (DataContext is MainViewModel vm && e.Row.Item is PcGame game)
            {
                await vm.SaveGameStatus(game);
            }
        }
    }
}
