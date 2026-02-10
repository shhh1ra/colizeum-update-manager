using colizeumUpdateManager.Data;
using colizeumUpdateManager.Infrastructure;
using colizeumUpdateManager.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace colizeumUpdateManager.ViewModels
{
    public class GamesViewModel : ViewModelBase
    {
        private readonly GameRepository _repo = new();

        public ObservableCollection<Pc> Pcs { get; } = new();
        public ObservableCollection<PcGame> Games { get; } = new();

        private Pc? _selectedPc;
        public Pc? SelectedPc
        {
            get => _selectedPc;
            set
            {
                if (_selectedPc == value) return;
                _selectedPc = value;
                OnPropertyChanged();

                // При смене ПК — перезагружаем игры
                _ = ReloadGames();
            }
        }

        private DateTime? _selectedDate = DateTime.Today;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate == value) return;
                _selectedDate = value;
                OnPropertyChanged();

                // При смене даты — перезагружаем игры
                _ = ReloadGames();
            }
        }

        public ICommand SaveCommand { get; }

        public GamesViewModel()
        {
            SaveCommand = new RelayCommand(async _ => await Save());
        }

        public async Task Load()
        {
            Pcs.Clear();
            var pcs = await _repo.GetPcs();

            foreach (var pc in pcs)
                Pcs.Add(pc);

            // авто-выбор
            if (SelectedPc == null && Pcs.Count > 0)
                SelectedPc = Pcs[0];
            else
                await ReloadGames();
        }

        private async Task ReloadGames()
        {
            Games.Clear();

            if (SelectedPc == null)
                return;

            var games = await _repo.GetGamesForPc(SelectedPc.Id, SelectedDate);

            foreach (var g in games)
                Games.Add(g);
        }

        private async Task Save()
        {
            // “Сохранить” обычно значит: сохранить текущие статусы из грида
            // Если нужно другое поведение — скажешь, подстроим.
            if (SelectedPc == null) return;

            foreach (var game in Games.ToList())
                await _repo.SaveStatus(SelectedPc.Id, game.GameId, game.Status, SelectedDate);

            // после сохранения можно обновить “вчера/сегодня” если надо
            await ReloadGames();
        }

        public async Task SaveGameStatus(PcGame game)
        {
            if (SelectedPc == null) return;
            await _repo.SaveStatus(SelectedPc.Id, game.GameId, game.Status, SelectedDate);
        }
    }
}
