using colizeumUpdateManager.Data;
using colizeumUpdateManager.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace colizeumUpdateManager.ViewModels
{
    public class TimersViewModel : ViewModelBase
    {
        private readonly TimerSlotsRepository _repo = new();

        public TimerSlotViewModel Slot1 { get; } = new(1);
        public TimerSlotViewModel Slot2 { get; } = new(2);
        public TimerSlotViewModel Slot3 { get; } = new(3);
        public TimerSlotViewModel Slot4 { get; } = new(4);
        public TimerSlotViewModel Slot5 { get; } = new(5);
        public TimerSlotViewModel Slot6 { get; } = new(6);

        private DispatcherTimer? _uiTick;
        private DispatcherTimer? _flushTick;

        public async Task Load()
        {
            await _repo.EnsureSlotsExist();

            // правило: после перезапуска не продолжаем
            await _repo.ResetRunningFlags();

            var rows = await _repo.GetAll();

            // safety: если таблица пустая/битая — всё равно не падаем
            foreach (var row in rows)
            {
                var slot = GetSlot(row.SlotId);
                slot?.LoadFromDb(row.Note, row.GoalText, row.ElapsedMs, row.IsRunning);
            }

            StartTimers();
        }

        private TimerSlotViewModel? GetSlot(int slotId) => slotId switch
        {
            1 => Slot1,
            2 => Slot2,
            3 => Slot3,
            4 => Slot4,
            5 => Slot5,
            6 => Slot6,
            _ => null
        };

        private TimerSlotViewModel[] AllSlots() => new[] { Slot1, Slot2, Slot3, Slot4, Slot5, Slot6 };

        private void StartTimers()
        {
            _uiTick ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uiTick.Tick += (_, __) =>
            {
                foreach (var s in AllSlots())
                    s.TickUi();
            };
            _uiTick.Start();

            _flushTick ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _flushTick.Tick += async (_, __) => await FlushDirty();
            _flushTick.Start();
        }

        public async Task FlushDirty()
        {
            // перед записью "поджимаем" бегущие, чтобы в БД было актуально
            foreach (var s in AllSlots())
                s.FlushIntoStoredElapsed();

            var dirty = AllSlots().Where(s => s.IsDirty).ToList();
            if (dirty.Count == 0) return;

            foreach (var s in dirty)
            {
                var row = new TimerSlotRow
                {
                    SlotId = s.SlotId,
                    Note = s.Note,
                    GoalText = s.GoalText,
                    ElapsedMs = s.ElapsedMs,
                    IsRunning = s.IsRunning // будет false на рестарте, но во время работы — true/false
                };

                await _repo.Update(row);
                s.ClearDirty();
            }
        }

        public async Task OnAppClosing()
        {
            // Финальный flush
            await FlushDirty();
        }
    }
}
