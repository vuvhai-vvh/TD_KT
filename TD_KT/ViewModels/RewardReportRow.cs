namespace TD_KT.ViewModels
{
    /// <summary>
    /// Dòng hiển thị cho Tab 2 - Báo cáo khen thưởng (ReportStatisticView).
    /// Chỉ là model hiển thị (checkbox chọn dòng + các cột báo cáo), không chứa logic SQL.
    /// </summary>
    public class RewardReportRow : BaseViewModel
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int No { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string RewardContentName { get; set; } = string.Empty;
        public string RewardFormName { get; set; } = string.Empty;
        public string IssuingLevel { get; set; } = string.Empty;
        public string DecisionNumber { get; set; } = string.Empty;
        public string SignedDateText { get; set; } = string.Empty;
        public string Signer { get; set; } = string.Empty;
    }
}
