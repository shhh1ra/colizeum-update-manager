using System.Threading.Tasks;
using System.Windows.Input;
using colizeumUpdateManager.Infrastructure;

namespace colizeumUpdateManager.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object? _currentViewModel;
        public object? CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        private AppSection _currentSection;
        public AppSection CurrentSection
        {
            get => _currentSection;
            set
            {
                _currentSection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGamesActive));
                OnPropertyChanged(nameof(IsTimersActive));
            }
        }

        public bool IsGamesActive => CurrentSection == AppSection.Games;
        public bool IsTimersActive => CurrentSection == AppSection.Timers;

        public ICommand ShowGamesCommand { get; }
        public ICommand ShowTimersCommand { get; }

        // держим инстанс таймеров, чтобы сохранить на выходе даже если пользователь не на этом экране
        public TimersViewModel TimersVm { get; } = new TimersViewModel();
        private bool _timersLoaded;

        public MainViewModel()
        {
            ShowGamesCommand = new RelayCommand(async _ => await ShowGames());
            ShowTimersCommand = new RelayCommand(async _ => await ShowTimers());

            _ = ShowGames();
        }

        private async Task ShowGames()
        {
            CurrentSection = AppSection.Games;

            var vm = new GamesViewModel();
            CurrentViewModel = vm;
            await vm.Load();
        }

        private async Task ShowTimers()
        {
            CurrentSection = AppSection.Timers;

            if (!_timersLoaded)
            {
                await TimersVm.Load();
                _timersLoaded = true;
            }

            CurrentViewModel = TimersVm;
        }

        // вызывать при закрытии приложения
        public async Task FlushOnExit()
        {
            if (_timersLoaded)
                await TimersVm.OnAppClosing();
        }
    }
}
