using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using colizeumUpdateManager.ViewModels;

namespace colizeumUpdateManager
{
    public partial class MainWindow : Window
    {
        private bool _isExiting;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isExiting) return;

            // Отменяем закрытие, чтобы успеть сохранить
            e.Cancel = true;
            _isExiting = true;

            try
            {
                if (DataContext is MainViewModel vm)
                    await vm.FlushOnExit();
            }
            finally
            {
                // Закрываем окно повторно уже “по-настоящему”
                Closing -= MainWindow_Closing;
                Close();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
                return;

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
    }
}
