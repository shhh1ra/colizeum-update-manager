namespace colizeumUpdateManager.Models
{
    public enum UpdateStatus
    {
        NotUpdated = 0,
        Updated = 1,
    }

    public class PcGame
    {
        public int PcId { get; set; }
        public int GameId { get; set; }
        public string GameName { get; set; }

        // статус за выбранную дату
        public UpdateStatus Status { get; set; }

        // статус за вчера (относительно выбранной даты)
        public UpdateStatus YesterdayStatus { get; set; }

        // Чекбокс за выбранную дату (редактируемый)
        public bool IsUpdated
        {
            get => Status == UpdateStatus.Updated;
            set => Status = value ? UpdateStatus.Updated : UpdateStatus.NotUpdated;
        }

        // Чекбокс за вчера (readonly)
        public bool IsUpdatedYesterday => YesterdayStatus == UpdateStatus.Updated;
    }
}
