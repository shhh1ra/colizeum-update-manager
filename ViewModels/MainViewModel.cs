using colizeumUpdateManager.Data;
using colizeumUpdateManager.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace colizeumUpdateManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly GameRepository _repo = new();

        public ObservableCollection<Pc> Pcs { get; } = new();
        public ObservableCollection<PcGame> Games { get; } = new();

        private Pc _selectedPc;
        public Pc SelectedPc
        {
            get => _selectedPc;
            set
            {
                _selectedPc = value;
                OnPropertyChanged();
                LoadGamesForDate(SelectedDate);
            }
        }

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value.Date;
                OnPropertyChanged();
                LoadGamesForDate(_selectedDate);
            }
        }

        public ICommand SaveCommand { get; }

        public MainViewModel()
        {
            SaveCommand = new RelayCommand(async () => await SaveAllGames());
        }

        public async void Load()
        {
            var pcs = await _repo.GetPcs();
            Pcs.Clear();
            foreach (var pc in pcs)
                Pcs.Add(pc);

            if (Pcs.Any())
                SelectedPc = Pcs.First();
        }

        private async void LoadGamesForDate(DateTime date)
        {
            Games.Clear();
            if (SelectedPc == null) return;

            await EnsureStatusesForDate(SelectedPc.Id, date);

            var games = await _repo.GetGamesForPc(SelectedPc.Id, date);
            foreach (var g in games)
                Games.Add(g);
        }

        // Вставляем нулевые записи на дату, если для этой даты у ПК вообще нет статусов
        private async Task EnsureStatusesForDate(int pcId, DateTime date)
        {
            var hasAny = await _repo.HasAnyStatusForDate(pcId, date);
            if (hasAny) return;

            var requiredGameIds = await _repo.GetRequiredGameIdsForPc(pcId);
            foreach (var gameId in requiredGameIds)
            {
                await _repo.SaveStatus(pcId, gameId, UpdateStatus.NotUpdated, date);
            }
        }

        private async Task SaveAllGames()
        {
            if (SelectedPc == null) return;

            foreach (var game in Games)
            {
                await _repo.SaveStatus(game.PcId, game.GameId, game.Status, SelectedDate);
            }
        }

        public async Task SaveGameStatus(PcGame game)
        {
            await _repo.SaveStatus(game.PcId, game.GameId, game.Status, SelectedDate);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public async void Execute(object parameter) => await _execute();

        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
