using System.Windows.Controls;
using colizeumUpdateManager.Models;
using colizeumUpdateManager.ViewModels;

namespace colizeumUpdateManager.Views
{
    public partial class GamesView : UserControl
    {
        public GamesView()
        {
            InitializeComponent();
        }

        private async void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (DataContext is not GamesViewModel vm)
                return;

            if (e.Row.Item is PcGame game)
                await vm.SaveGameStatus(game);
        }
    }
}
