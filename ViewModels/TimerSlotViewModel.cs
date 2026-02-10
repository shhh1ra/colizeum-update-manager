using colizeumUpdateManager.Infrastructure;
using System;
using System.Windows.Input;

namespace colizeumUpdateManager.ViewModels
{
    public class TimerSlotViewModel : ViewModelBase
    {
        public int SlotId { get; }

        private string _note = "";
        public string Note
        {
            get => _note;
            set { if (_note == value) return; _note = value; OnPropertyChanged(); MarkDirty(); }
        }

        private string _goalText = "";
        public string GoalText
        {
            get => _goalText;
            set { if (_goalText == value) return; _goalText = value; OnPropertyChanged(); MarkDirty(); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayPauseText));
                MarkDirty();
            }
        }

        // "сохранённое" значение
        private long _elapsedMs;
        public long ElapsedMs
        {
            get => _elapsedMs;
            private set
            {
                if (_elapsedMs == value) return;
                _elapsedMs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ElapsedText));
                MarkDirty();
            }
        }

        // время старта текущего пробега (только в памяти)
        private DateTime _runStartedAt;

        public string ElapsedText
        {
            get
            {
                var ms = GetCurrentElapsedMs();
                var ts = TimeSpan.FromMilliseconds(ms);
                // h:mm:ss (без дней)
                var hours = (int)ts.TotalHours;
                return $"{hours}:{ts.Minutes:00}:{ts.Seconds:00}";
            }
        }

        public string PlayPauseText => IsRunning ? "⏸" : "▶";

        public ICommand PlayPauseCommand { get; }
        public ICommand ResetCommand { get; }

        // для flush10s: писать в бд только если изменилось
        public bool IsDirty { get; private set; }

        public TimerSlotViewModel(int slotId)
        {
            SlotId = slotId;

            PlayPauseCommand = new RelayCommand(_ => TogglePlayPause());
            ResetCommand = new RelayCommand(_ => Reset());
        }

        public void LoadFromDb(string note, string goalText, long elapsedMs, bool isRunningFromDb)
        {
            // по твоему правилу: после перезапуска не продолжаем
            _note = note ?? "";
            _goalText = goalText ?? "";
            _elapsedMs = elapsedMs;
            _isRunning = false;

            OnPropertyChanged(nameof(Note));
            OnPropertyChanged(nameof(GoalText));
            OnPropertyChanged(nameof(ElapsedMs));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(ElapsedText));
            OnPropertyChanged(nameof(PlayPauseText));

            IsDirty = false;
        }

        public void TickUi()
        {
            if (!IsRunning) return;
            // просто обновляем текст времени
            OnPropertyChanged(nameof(ElapsedText));
        }

        public long GetCurrentElapsedMs()
        {
            if (!IsRunning) return ElapsedMs;
            var delta = DateTime.UtcNow - _runStartedAt;
            return ElapsedMs + (long)delta.TotalMilliseconds;
        }

        private void TogglePlayPause()
        {
            if (!IsRunning)
            {
                // старт
                _runStartedAt = DateTime.UtcNow;
                IsRunning = true;
                return;
            }

            // пауза: фиксируем накопленное
            var now = DateTime.UtcNow;
            var delta = now - _runStartedAt;
            ElapsedMs = ElapsedMs + (long)delta.TotalMilliseconds;
            IsRunning = false;

            // обновить время
            OnPropertyChanged(nameof(ElapsedText));
        }

        private void Reset()
        {
            IsRunning = false;
            ElapsedMs = 0;
            OnPropertyChanged(nameof(ElapsedText));
        }

        public void FlushIntoStoredElapsed()
        {
            // перед записью в БД: если running, “поджать” elapsed к текущему
            if (!IsRunning) return;

            var now = DateTime.UtcNow;
            var delta = now - _runStartedAt;
            _elapsedMs = _elapsedMs + (long)delta.TotalMilliseconds; // напрямую, чтобы не дергать MarkDirty дважды
            _runStartedAt = now;

            OnPropertyChanged(nameof(ElapsedMs));
            OnPropertyChanged(nameof(ElapsedText));
            MarkDirty(); // это изменение нужно сохранить
        }

        public void MarkDirty() => IsDirty = true;
        public void ClearDirty() => IsDirty = false;
    }
}
