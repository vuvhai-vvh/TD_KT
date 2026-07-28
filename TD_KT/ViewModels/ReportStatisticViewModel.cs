using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Data;
using TD_KT.Data;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    /// <summary>
    /// ViewModel cho ReportStatisticView (Tab 1-4).
    /// - Không để logic DB/Export trong View.
    /// - Tất cả truy vấn DB gọi qua lớp Data.
    /// - Xuất Word gọi qua WordDocxExportService.
    /// </summary>
    public class ReportStatisticViewModel : BaseViewModel
    {
        private readonly UnitScoreSummaryData _unitScoreSummaryData;
        private readonly RewardReportData _rewardReportData;
        private readonly TitleProposalData _titleProposalData;
        private readonly MedalRecordData _medalRecordData;
        private readonly OrgUnitData _orgUnitData;
        private readonly IMessageService _messageService;
        private readonly WordDocxExportService _exportService;

        private bool _isResettingRewardReportFilters;
        private bool _isResettingSeniorityFilters;
        private bool _isUpdatingScoreFilters;

        #region TAB 1 - Thống kê điểm thi đua

        public ObservableCollection<ReportFilterOption> ScoreYearOptions { get; } = new ObservableCollection<ReportFilterOption>();
        public ObservableCollection<ReportFilterOption> ScoreMonthOptions { get; } = new ObservableCollection<ReportFilterOption>();

        private ReportFilterOption _selectedScoreYear;
        public ReportFilterOption SelectedScoreYear
        {
            get => _selectedScoreYear;
            set
            {
                if (!SetProperty(ref _selectedScoreYear, value)) return;
                EnsureScoreMonths();
            }
        }

        private ReportFilterOption _selectedScoreMonth;
        public ReportFilterOption SelectedScoreMonth
        {
            get => _selectedScoreMonth;
            set
            {
                if (SetProperty(ref _selectedScoreMonth, value) && !_isUpdatingScoreFilters)
                    RefreshScoreSummary();
            }
        }

        private string _monthHeaderText = "Thi đua Tháng";
        public string MonthHeaderText
        {
            get => _monthHeaderText;
            set => SetProperty(ref _monthHeaderText, value);
        }

        private string _yearHeaderText = "Thi đua Năm";
        public string YearHeaderText
        {
            get => _yearHeaderText;
            set => SetProperty(ref _yearHeaderText, value);
        }

        public ObservableCollection<UnitScoreAverageDisplayModel> ScoreMonthRows { get; } = new ObservableCollection<UnitScoreAverageDisplayModel>();
        public ObservableCollection<UnitScoreAverageDisplayModel> ScoreYearRows { get; } = new ObservableCollection<UnitScoreAverageDisplayModel>();

        public RelayCommand RefreshScoreSummaryCommand { get; }

        // 14 đơn vị cố định cho thống kê thi đua (giữ đúng thứ tự hiển thị giống bảng chấm điểm tuần)
        private static readonly string[] FIXED_SCORE_UNITS = new[]
        {
            
            // Nhóm 1
            "Cụm 1", "Cụm 2", "Cụm 3",
            // Nhóm 2
            "Phòng 6", "Phòng 7", "Phòng 8",
            // Nhóm 3
            "Phòng Chính trị", "Phòng Tham mưu", "Phòng Hậu cần - Kỹ thuật", 
            // Nhóm 4
            "Ban Tài chính","Trạm 15", "Trạm 16", "Trạm 17", "Trạm 24",

        };

        private static readonly HashSet<string> SCORE_GROUP_1 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cụm 1", "Cụm 2", "Cụm 3"
        };
        private static readonly HashSet<string> SCORE_GROUP_2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Phòng 6", "Phòng 7", "Phòng 8"
        };
        private static readonly HashSet<string> SCORE_GROUP_3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Phòng Chính trị", "Phòng Tham mưu", "Phòng Hậu cần - Kỹ thuật"
        };
        private static readonly HashSet<string> SCORE_GROUP_4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
             "Ban Tài chính","Trạm 15","Trạm 16", "Trạm 17", "Trạm 24"
        };

        private static int GetScoreGroupId(string orgUnitName)
        {
            if (string.IsNullOrWhiteSpace(orgUnitName)) return 0;
            if (SCORE_GROUP_1.Contains(orgUnitName)) return 1;
            if (SCORE_GROUP_2.Contains(orgUnitName)) return 2;
            if (SCORE_GROUP_3.Contains(orgUnitName)) return 3;
            if (SCORE_GROUP_4.Contains(orgUnitName)) return 4;
            return 0;
        }

        #endregion

        #region TAB 2 - Báo cáo khen thưởng

        public ObservableCollection<ReportFilterOption> YearOptions { get; } = new ObservableCollection<ReportFilterOption>();
        public ObservableCollection<ReportFilterOption> IssuingLevelOptions { get; } = new ObservableCollection<ReportFilterOption>();
        public ObservableCollection<ReportFilterOption> RewardFormOptions { get; } = new ObservableCollection<ReportFilterOption>();


        private ReportFilterOption _selectedYear;
        public ReportFilterOption SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value) && !_isResettingRewardReportFilters)
                    LoadRewardReportData();
            }
        }

        private ReportFilterOption _selectedIssuingLevel;
        public ReportFilterOption SelectedIssuingLevel
        {
            get => _selectedIssuingLevel;
            set
            {
                if (SetProperty(ref _selectedIssuingLevel, value) && !_isResettingRewardReportFilters)
                    LoadRewardReportData();
            }
        }

        private ReportFilterOption _selectedRewardForm;
        public ReportFilterOption SelectedRewardForm
        {
            get => _selectedRewardForm;
            set
            {
                if (SetProperty(ref _selectedRewardForm, value) && !_isResettingRewardReportFilters)
                    LoadRewardReportData();
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value) && !_isResettingRewardReportFilters)
                    LoadRewardReportData();
            }
        }

        public ObservableCollection<RewardReportRowDisplayModel> RewardReportRows { get; } = new ObservableCollection<RewardReportRowDisplayModel>();

        public RelayCommand RefreshRewardReportCommand { get; }
        public RelayCommand ExportRewardReportCommand { get; }

        #endregion

        #region TAB 3 - Thống kê khen thưởng qua các năm

        public ObservableCollection<ReportFilterOption> RewardYearsYearOptions { get; } = new ObservableCollection<ReportFilterOption>();

        private bool _isLoadingRewardYearsData;

        private ReportFilterOption _selectedRewardYearsYear;
        public ReportFilterOption SelectedRewardYearsYear
        {
            get => _selectedRewardYearsYear;
            set => SetProperty(ref _selectedRewardYearsYear, value);
        }

        private string _rewardYearsSearchText;
        public string RewardYearsSearchText
        {
            get => _rewardYearsSearchText;
            set
            {
                if (SetProperty(ref _rewardYearsSearchText, value) && !_isResettingRewardReportFilters)
                    LoadRewardYearsData();
            }
        }

        public ObservableCollection<RewardYearsRowDisplayModel> RewardYearsRows { get; } = new ObservableCollection<RewardYearsRowDisplayModel>();

        public RelayCommand RefreshRewardYearsCommand { get; }
        public RelayCommand ExportRewardYearsCommand { get; }

        #endregion

        #region TAB 4 - Đề xuất danh hiệu theo thâm niên

        public ObservableCollection<string> SeniorityStatusOptions { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> SeniorityOrgUnitOptions { get; } = new ObservableCollection<string>();

        private bool _isRefreshingSeniorityAwards;
        private bool _isRefreshingSeniorityOrgUnitOptions;

        private string _selectedSeniorityStatus;
        public string SelectedSeniorityStatus
        {
            get => _selectedSeniorityStatus;
            set
            {
                if (SetProperty(ref _selectedSeniorityStatus, value) && !_isRefreshingSeniorityAwards && !_isResettingRewardReportFilters)
                    RefreshSeniorityAwards();
            }
        }

        private string _seniorityOrgUnitSearchText;
        public string SeniorityOrgUnitSearchText
        {
            get => _seniorityOrgUnitSearchText;
            set
            {
                if (SetProperty(ref _seniorityOrgUnitSearchText, value) && !_isResettingRewardReportFilters)
                    RefreshSeniorityAwards();
            }
        }

        private string _selectedSeniorityOrgUnit;
        public string SelectedSeniorityOrgUnit
        {
            get => _selectedSeniorityOrgUnit;
            set
            {
                if (SetProperty(ref _selectedSeniorityOrgUnit, value)
                    && !_isRefreshingSeniorityAwards
                    && !_isRefreshingSeniorityOrgUnitOptions
                    && !_isResettingSeniorityFilters)
                    RefreshSeniorityAwards();
            }
        }

        public ObservableCollection<SeniorityAwardDisplayModel> SeniorityAwardRows { get; } = new ObservableCollection<SeniorityAwardDisplayModel>();

        public RelayCommand RefreshSeniorityAwardsCommand { get; }
        public RelayCommand MarkSeniorityAsAwardedCommand { get; }

        #endregion

        #region TAB 5 - Báo cáo danh hiệu về hưu

        public ObservableCollection<MedalRecordRow> MedalRecordRows { get; } = new ObservableCollection<MedalRecordRow>();

        private MedalRecordRow _selectedMedalRecord;
        public MedalRecordRow SelectedMedalRecord
        {
            get => _selectedMedalRecord;
            set
            {
                if (SetProperty(ref _selectedMedalRecord, value))
                {
                    LoadMedalRecordToForm(_selectedMedalRecord);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _medalRecordFullName;
        public string MedalRecordFullName
        {
            get => _medalRecordFullName;
            set
            {
                if (SetProperty(ref _medalRecordFullName, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _medalRecordRank;
        public string MedalRecordRank
        {
            get => _medalRecordRank;
            set => SetProperty(ref _medalRecordRank, value);
        }

        private string _medalRecordPosition;
        public string MedalRecordPosition
        {
            get => _medalRecordPosition;
            set => SetProperty(ref _medalRecordPosition, value);
        }

        private string _medalRecordOrgUnit;
        public string MedalRecordOrgUnit
        {
            get => _medalRecordOrgUnit;
            set => SetProperty(ref _medalRecordOrgUnit, value);
        }

        private string _medalRecordTitleName;
        public string MedalRecordTitleName
        {
            get => _medalRecordTitleName;
            set => SetProperty(ref _medalRecordTitleName, value);
        }

        private string _medalRecordDecisionNumber;
        public string MedalRecordDecisionNumber
        {
            get => _medalRecordDecisionNumber;
            set => SetProperty(ref _medalRecordDecisionNumber, value);
        }

        public RelayCommand AddMedalRecordCommand { get; }
        public RelayCommand EditMedalRecordCommand { get; }
        public RelayCommand DeleteMedalRecordCommand { get; }
        public RelayCommand ClearMedalRecordFormCommand { get; }

        private int _medalRecordIndex = 1;

        #endregion

        public ReportStatisticViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService(), new WordDocxExportService())
        {
        }

        public ReportStatisticViewModel(IConnectionStringProvider csProvider, IMessageService messageService, WordDocxExportService exportService)
        {
            _unitScoreSummaryData = new UnitScoreSummaryData(csProvider);
            _rewardReportData = new RewardReportData(csProvider);
            _titleProposalData = new TitleProposalData(csProvider);
            _medalRecordData = new MedalRecordData(csProvider);
            _orgUnitData = new OrgUnitData(csProvider);

            _messageService = messageService;
            _exportService = exportService;

            RefreshScoreSummaryCommand = new RelayCommand(_ => ResetTab1AndRefresh());
            RefreshRewardReportCommand = new RelayCommand(_ => ResetTab2AndRefresh());
            ExportRewardReportCommand = new RelayCommand(_ => ExportRewardReport());
            RefreshRewardYearsCommand = new RelayCommand(_ => ResetTab3AndRefresh());
            ExportRewardYearsCommand = new RelayCommand(_ => ExportRewardYears());
            RefreshSeniorityAwardsCommand = new RelayCommand(_ => ResetTab4AndRefresh());
            MarkSeniorityAsAwardedCommand = new RelayCommand(_ => MarkSelectedSeniorityAsAwarded());
            AddMedalRecordCommand = new RelayCommand(_ => AddMedalRecord(), _ => CanAddMedalRecord());
            EditMedalRecordCommand = new RelayCommand(_ => EditMedalRecord(), _ => CanEditMedalRecord());
            DeleteMedalRecordCommand = new RelayCommand(_ => DeleteMedalRecord(), _ => CanDeleteMedalRecord());
            ClearMedalRecordFormCommand = new RelayCommand(_ => ResetTab5AndRefresh());

            LoadTab1Filters();
            LoadRewardReportFilters();
            LoadTab3Filters();
            LoadTab4Filters();

            // Load lần đầu (để mở form là có dữ liệu ngay)
            RefreshScoreSummary();
            LoadRewardReportData();
            LoadRewardYearsData();
            RefreshSeniorityAwards(forceSync: true);
            LoadMedalRecords();
        }

        private void ResetTab1AndRefresh()
        {
            ClearCollectionSortState(ScoreMonthRows);
            ClearCollectionSortState(ScoreYearRows);
            LoadTab1Filters();
            RefreshScoreSummary();
        }

        private void ResetTab2AndRefresh()
        {
            ClearCollectionSortState(RewardReportRows);
            _isResettingRewardReportFilters = true;
            try
            {
                LoadRewardReportFilters();
                SearchText = string.Empty;
            }
            finally
            {
                _isResettingRewardReportFilters = false;
            }

            LoadRewardReportData();
        }

        private void ResetTab3AndRefresh()
        {
            ClearCollectionSortState(RewardYearsRows);
            LoadTab3Filters();

            if (!string.IsNullOrEmpty(_rewardYearsSearchText))
            {
                _rewardYearsSearchText = string.Empty;
                OnPropertyChanged(nameof(RewardYearsSearchText));
            }

            LoadRewardYearsData();
        }

        private void ResetTab4AndRefresh()
        {
            ClearCollectionSortState(SeniorityAwardRows);
            _isResettingSeniorityFilters = true;
            try
            {
                LoadTab4Filters();
                SeniorityOrgUnitSearchText = string.Empty;
            }
            finally
            {
                _isResettingSeniorityFilters = false;
            }

            RefreshSeniorityAwards(forceSync: true);
        }

        private void ResetTab5AndRefresh()
        {
            ClearCollectionSortState(MedalRecordRows);
            LoadMedalRecords();
            ClearMedalRecordForm();
        }

        private static void ClearCollectionSortState(object source)
        {
            var view = CollectionViewSource.GetDefaultView(source);
            if (view == null || view.SortDescriptions.Count == 0)
                return;

            view.SortDescriptions.Clear();
            view.Refresh();
        }

        #region TAB 5

        private bool CanAddMedalRecord()
        {
            return !string.IsNullOrWhiteSpace(MedalRecordFullName);
        }

        private bool CanEditMedalRecord()
        {
            return SelectedMedalRecord != null;
        }

        private bool CanDeleteMedalRecord()
        {
            return SelectedMedalRecord != null;
        }

        private void AddMedalRecord()
        {
            try
            {
                var record = new MedalRecordDataRow
                {
                    FullName = MedalRecordFullName?.Trim() ?? string.Empty,
                    Rank = MedalRecordRank?.Trim() ?? string.Empty,
                    Position = MedalRecordPosition?.Trim() ?? string.Empty,
                    OrgUnit = MedalRecordOrgUnit?.Trim() ?? string.Empty,
                    TitleName = MedalRecordTitleName?.Trim() ?? string.Empty,
                    DecisionNumber = MedalRecordDecisionNumber?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.Now
                };

                var newId = _medalRecordData.Insert(record);
                MedalRecordRows.Add(new MedalRecordRow
                {
                    Id = newId,
                    No = _medalRecordIndex++,
                    FullName = record.FullName,
                    Rank = record.Rank,
                    Position = record.Position,
                    OrgUnit = record.OrgUnit,
                    TitleName = record.TitleName,
                    DecisionNumber = record.DecisionNumber
                });

                ClearMedalRecordForm();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không lưu được danh hiệu về hưu.\n" + ex.Message);
            }
        }

        private void EditMedalRecord()
        {
            if (SelectedMedalRecord == null)
            {
                _messageService.Warning("Vui lòng chọn một dòng để sửa.");
                return;
            }

            try
            {
                var record = new MedalRecordDataRow
                {
                    Id = SelectedMedalRecord.Id,
                    FullName = MedalRecordFullName?.Trim() ?? string.Empty,
                    Rank = MedalRecordRank?.Trim() ?? string.Empty,
                    Position = MedalRecordPosition?.Trim() ?? string.Empty,
                    OrgUnit = MedalRecordOrgUnit?.Trim() ?? string.Empty,
                    TitleName = MedalRecordTitleName?.Trim() ?? string.Empty,
                    DecisionNumber = MedalRecordDecisionNumber?.Trim() ?? string.Empty
                };

                _medalRecordData.Update(record);
                LoadMedalRecords();
                ClearMedalRecordForm();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không cập nhật được danh hiệu về hưu.\n" + ex.Message);
            }
        }

        private void DeleteMedalRecord()
        {
            if (SelectedMedalRecord == null)
            {
                _messageService.Warning("Vui lòng chọn một dòng để xóa.");
                return;
            }

            try
            {
                _medalRecordData.Delete(SelectedMedalRecord.Id);
                MedalRecordRows.Remove(SelectedMedalRecord);
                ReindexMedalRecords();
                ClearMedalRecordForm();
                SelectedMedalRecord = null;
            }
            catch (Exception ex)
            {
                _messageService.Error("Không xóa được danh hiệu về hưu.\n" + ex.Message);
            }
        }

        private void ClearMedalRecordForm()
        {
            MedalRecordFullName = string.Empty;
            MedalRecordRank = string.Empty;
            MedalRecordPosition = string.Empty;
            MedalRecordOrgUnit = string.Empty;
            MedalRecordTitleName = string.Empty;
            MedalRecordDecisionNumber = string.Empty;
            SelectedMedalRecord = null;
        }

        private void LoadMedalRecords()
        {
            MedalRecordRows.Clear();

            try
            {
                var rows = _medalRecordData.GetList();
                var no = 1;
                foreach (var row in rows)
                {
                    MedalRecordRows.Add(new MedalRecordRow
                    {
                        Id = row.Id,
                        No = no++,
                        FullName = row.FullName ?? string.Empty,
                        Rank = row.Rank ?? string.Empty,
                        Position = row.Position ?? string.Empty,
                        OrgUnit = row.OrgUnit ?? string.Empty,
                        TitleName = row.TitleName ?? string.Empty,
                        DecisionNumber = row.DecisionNumber ?? string.Empty
                    });
                }

                _medalRecordIndex = no;
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được danh hiệu về hưu.\n" + ex.Message);
            }
        }

        private void LoadMedalRecordToForm(MedalRecordRow row)
        {
            if (row == null) return;

            MedalRecordFullName = row.FullName ?? string.Empty;
            MedalRecordRank = row.Rank ?? string.Empty;
            MedalRecordPosition = row.Position ?? string.Empty;
            MedalRecordOrgUnit = row.OrgUnit ?? string.Empty;
            MedalRecordTitleName = row.TitleName ?? string.Empty;
            MedalRecordDecisionNumber = row.DecisionNumber ?? string.Empty;
        }

        private void ReindexMedalRecords()
        {
            var no = 1;
            foreach (var row in MedalRecordRows)
                row.No = no++;

            _medalRecordIndex = no;
        }

        #endregion

        #region TAB 1

        private void LoadTab1Filters()
        {
            ScoreYearOptions.Clear();

            try
            {
                var years = MergeYearOptions(_unitScoreSummaryData.GetAvailableYears(), ensureCurrentYear: true);
                foreach (var y in years)
                    ScoreYearOptions.Add(new ReportFilterOption { Id = y, Name = y.ToString() });

                SelectedScoreYear = ScoreYearOptions.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được bộ lọc Năm (thi đua).\n" + ex.Message);
            }
        }

        private void EnsureScoreMonths()
        {
            _isUpdatingScoreFilters = true;
            ScoreMonthOptions.Clear();

            try
            {
                var year = SelectedScoreYear?.Id;
                if (year == null)
                {
                    SelectedScoreMonth = null;
                    return;
                }

                var months = _unitScoreSummaryData.GetAvailableMonths(year.Value);
                foreach (var m in months)
                    ScoreMonthOptions.Add(new ReportFilterOption { Id = m, Name = m.ToString() });

                SelectedScoreMonth = ScoreMonthOptions.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được bộ lọc Tháng (thi đua).\n" + ex.Message);
            }
            finally
            {
                _isUpdatingScoreFilters = false;
            }

            RefreshScoreSummary();
        }

        private void RefreshScoreSummary()
        {
            try
            {
                var year = SelectedScoreYear?.Id;
                var month = SelectedScoreMonth?.Id;

                if (year == null)
                {
                    _messageService.Warning("Vui lòng chọn Năm.");
                    return;
                }

                // Ensure tháng (phòng trường hợp load lần đầu)
                if (ScoreMonthOptions.Count == 0)
                    EnsureScoreMonths();

                if (month == null)
                {
                    _messageService.Warning("Không có dữ liệu Tháng cho năm đã chọn.");
                    ScoreMonthRows.Clear();
                    ScoreYearRows.Clear();
                    MonthHeaderText = $"Thi đua Tháng";
                    YearHeaderText = $"Thi đua Năm {year}";
                    return;
                }

                MonthHeaderText = $"Thi đua Tháng {month:00}/{year}";
                YearHeaderText = $"Thi đua Năm {year}";

                // ===== Tháng: hiển thị theo 14 đơn vị cố định + xếp hạng theo từng nhóm =====
                var monthRaw = _unitScoreSummaryData.GetMonthlyAverageScores(year.Value, month.Value);
                var monthByName = monthRaw
                    .Where(x => !string.IsNullOrWhiteSpace(x.OrgUnitName))
                    .GroupBy(x => x.OrgUnitName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var monthDisplay = new List<UnitScoreAverageDisplayModel>();
                var no = 1;
                foreach (var name in FIXED_SCORE_UNITS)
                {
                    monthByName.TryGetValue(name, out var r);
                    monthDisplay.Add(new UnitScoreAverageDisplayModel
                    {
                        No = no++,
                        GroupId = GetScoreGroupId(name),
                        OrgUnitName = name,
                        AverageScore = r?.AverageScore,
                        Ranking = 0
                    });
                }

                foreach (var grp in monthDisplay.GroupBy(x => x.GroupId).OrderBy(g => g.Key))
                {
                    var ordered = grp
                        .OrderByDescending(x => x.AverageScore ?? decimal.MinValue)
                        .ThenBy(x => x.OrgUnitName)
                        .ToList();
                    int rank = 1;
                    foreach (var item in ordered)
                        item.Ranking = rank++;
                }

                ScoreMonthRows.Clear();
                foreach (var item in monthDisplay)
                    ScoreMonthRows.Add(item);

                // ===== Năm: hiển thị theo 14 đơn vị cố định + xếp hạng theo từng nhóm =====
                var yearRaw = _unitScoreSummaryData.GetYearlyAverageScores(year.Value);
                var yearByName = yearRaw
                    .Where(x => !string.IsNullOrWhiteSpace(x.OrgUnitName))
                    .GroupBy(x => x.OrgUnitName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var yearDisplay = new List<UnitScoreAverageDisplayModel>();
                no = 1;
                foreach (var name in FIXED_SCORE_UNITS)
                {
                    yearByName.TryGetValue(name, out var r);
                    yearDisplay.Add(new UnitScoreAverageDisplayModel
                    {
                        No = no++,
                        GroupId = GetScoreGroupId(name),
                        OrgUnitName = name,
                        AverageScore = r?.AverageScore,
                        Ranking = 0
                    });
                }

                foreach (var grp in yearDisplay.GroupBy(x => x.GroupId).OrderBy(g => g.Key))
                {
                    var ordered = grp
                        .OrderByDescending(x => x.AverageScore ?? decimal.MinValue)
                        .ThenBy(x => x.OrgUnitName)
                        .ToList();
                    int rank = 1;
                    foreach (var item in ordered)
                        item.Ranking = rank++;
                }

                ScoreYearRows.Clear();
                foreach (var item in yearDisplay)
                    ScoreYearRows.Add(item);
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được thống kê điểm thi đua.\n" + ex.Message);
            }
        }

        #endregion

        #region TAB 2

        private void LoadRewardReportFilters()
        {
            YearOptions.Clear();
            YearOptions.Add(new ReportFilterOption { Id = null, Name = "Tất cả" });

            try
            {
                _rewardReportData.SyncRewardReportSnapshotsFromDecisions();
                foreach (var y in MergeYearOptions(_rewardReportData.GetAvailableReportYears(), ensureCurrentYear: true))
                {
                    YearOptions.Add(new ReportFilterOption { Id = y, Name = y.ToString() });
                }
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được bộ lọc Năm (khen thưởng).\n" + ex.Message);
            }

            IssuingLevelOptions.Clear();
            IssuingLevelOptions.Add(new ReportFilterOption { Id = null, Name = "Tất cả" });
            try
            {
                foreach (var il in _rewardReportData.GetIssuingLevels())
                    IssuingLevelOptions.Add(new ReportFilterOption { Id = il.Id, Name = il.Name });
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được bộ lọc Cấp ban hành.\n" + ex.Message);
            }

            RewardFormOptions.Clear();
            RewardFormOptions.Add(new ReportFilterOption { Id = null, Name = "Tất cả" });
            try
            {
                foreach (var rf in _rewardReportData.GetRewardForms())
                    RewardFormOptions.Add(new ReportFilterOption { Id = rf.Id, Name = rf.Name });
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được bộ lọc Hình thức khen thưởng.\n" + ex.Message);
            }

            SelectedYear = YearOptions.FirstOrDefault();
            SelectedIssuingLevel = IssuingLevelOptions.FirstOrDefault();
            SelectedRewardForm = RewardFormOptions.FirstOrDefault();
        }

        private void LoadRewardReportData()
        {
            try
            {
                RewardReportRows.Clear();

                _rewardReportData.SyncRewardReportSnapshotsFromDecisions();

                int? year = SelectedYear?.Id;
                int? issuingLevelId = SelectedIssuingLevel?.Id;
                int? rewardFormId = SelectedRewardForm?.Id;


                var rows = _rewardReportData.GetRewardReportRows(year, issuingLevelId, rewardFormId, SearchText);
                var no = 1;
                foreach (var r in rows)
                {
                    RewardReportRows.Add(new RewardReportRowDisplayModel
                    {
                        IsSelected = false,
                        No = no++,
                        SubjectName = r.SubjectName,
                        RewardContentName = r.RewardContentName,
                        RewardFormName = r.RewardFormName,
                        DecisionNumber = r.DecisionNumber,
                        IssuingLevel = r.IssuingLevelName,
                        SignedDate = r.SignedOrCreatedDate,
                        Signer = r.Signer
                    });
                }
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được báo cáo khen thưởng.\n" + ex.Message);
            }
        }

        private void ExportRewardReport()
        {
            try
            {
                var selected = RewardReportRows.Where(x => x.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    _messageService.Warning("Vui lòng tick chọn ít nhất 1 dòng để xuất!");
                    return;
                }

                var sfd = new SaveFileDialog
                {
                    Title = "Xuất báo cáo khen thưởng",
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"BaoCaoKhenThuong_{DateTime.Now:yyyyMMdd_HHmm}.docx"
                };
                if (sfd.ShowDialog() != true) return;

                var title = "BÁO CÁO KHEN THƯỞNG";
                var filters = new List<string>
                {
                    $"Năm: {(SelectedYear?.Id == null ? "Tất cả" : SelectedYear.Name)}",
                    $"Cấp ban hành: {(SelectedIssuingLevel?.Id == null ? "Tất cả" : SelectedIssuingLevel.Name)}",
                    $"Hình thức khen thưởng: {(SelectedRewardForm?.Id == null ? "Tất cả" : SelectedRewardForm.Name)}",
                    string.IsNullOrWhiteSpace(SearchText) ? null : $"Tìm kiếm: {SearchText.Trim()}",
                    $"Số dòng xuất: {selected.Count}"
                };

                var table = new List<IList<string>>
                {
                    new List<string> { "STT", "Cá nhân/Tập thể", "ND khen thưởng", "Hình thức khen thưởng", "Số QĐ", "Cấp ban hành", "Ngày ký", "Người ký" }
                };

                foreach (var r in selected)
                {
                    table.Add(new List<string>
                    {
                        r.No.ToString(),
                        r.SubjectName,
                        r.RewardContentName,
                        r.RewardFormName,
                        r.DecisionNumber,
                        r.IssuingLevel,
                        r.SignedDateText,
                        r.Signer
                    });
                }

                _exportService.ExportTableReport(sfd.FileName, title, filters, table);
                _messageService.Info("Xuất Word (.docx) thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thể xuất Word (.docx).\n" + ex.Message);
            }
        }

        #endregion

        #region TAB 3

        private void LoadTab3Filters()
        {
            RewardYearsYearOptions.Clear();
            RewardYearsYearOptions.Add(new ReportFilterOption { Id = null, Name = "Tất cả" });

            try
            {
                _rewardReportData.SyncRewardReportSnapshotsFromDecisions();
                _rewardReportData.SyncRewardHistoryFromReportSnapshots();
                foreach (var y in MergeYearOptions(_rewardReportData.GetAvailableRewardHistoryYears(), ensureCurrentYear: true))
                    RewardYearsYearOptions.Add(new ReportFilterOption { Id = y, Name = y.ToString() });
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được bộ lọc Năm (thống kê khen thưởng).\n" + ex.Message);
            }

            SelectedRewardYearsYear = RewardYearsYearOptions.FirstOrDefault();
        }

        private void LoadRewardYearsData()
        {
            if (_isLoadingRewardYearsData)
                return;

            _isLoadingRewardYearsData = true;
            try
            {
                RewardYearsRows.Clear();

                _rewardReportData.SyncRewardReportSnapshotsFromDecisions();
                _rewardReportData.SyncRewardHistoryFromReportSnapshots();

                var endYear = SelectedRewardYearsYear?.Id;
                var raw = _rewardReportData.GetRewardYearRawRowsFromSnapshots(endYear, RewardYearsSearchText);

                // Group theo (Cá nhân/Tập thể, HT khen thưởng)
                var groups = raw
                    .GroupBy(x => new { x.SubjectName, x.RewardFormName })
                    .ToList();

                var result = new List<RewardYearsRowDisplayModel>();

                foreach (var g in groups)
                {
                    var years = g.Select(x => x.Year).Where(y => y > 0).Distinct().OrderBy(y => y).ToList();
                    if (years.Count < 2) continue;

                    var runs = FindConsecutiveRuns(years);
                    foreach (var run in runs)
                    {
                        if (run.Length < 2) continue;
                        if (endYear != null && run.End != endYear.Value) continue;

                        var rep = g
                            .Where(x => x.Year == run.End)
                            .OrderByDescending(x => x.SignedOrCreatedDate ?? DateTime.MinValue)
                            .FirstOrDefault();

                        result.Add(new RewardYearsRowDisplayModel
                        {
                            IsSelected = false,
                            SubjectName = g.Key.SubjectName,
                            RewardFormName = g.Key.RewardFormName,
                            IssuingLevel = rep?.IssuingLevelName ?? string.Empty,
                            ConsecutiveText = $"{run.Start}-{run.End} ({run.Length} năm)",
                            ConsecutiveLength = run.Length,
                            EndYear = run.End
                        });
                    }
                }

                // Sort: run dài trước, năm kết thúc mới trước
                var ordered = result
                    .OrderByDescending(x => x.ConsecutiveLength)
                    .ThenByDescending(x => x.EndYear)
                    .ThenBy(x => x.SubjectName)
                    .ThenBy(x => x.RewardFormName)
                    .ToList();

                var no = 1;
                foreach (var row in ordered)
                {
                    row.No = no++;
                    RewardYearsRows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được thống kê khen thưởng qua các năm.\n" + ex.Message);
            }
            finally
            {
                _isLoadingRewardYearsData = false;
            }
        }

        private void ExportRewardYears()
        {
            try
            {
                var selected = RewardYearsRows.Where(x => x.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    _messageService.Warning("Vui lòng tick chọn ít nhất 1 dòng để xuất!");
                    return;
                }

                var sfd = new SaveFileDialog
                {
                    Title = "Xuất thống kê khen thưởng qua các năm",
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"ThongKeKhenThuong_{DateTime.Now:yyyyMMdd_HHmm}.docx"
                };
                if (sfd.ShowDialog() != true) return;

                var title = "THỐNG KÊ KHEN THƯỞNG QUA CÁC NĂM";
                var filters = new List<string>
                {
                    $"Năm (kết thúc chuỗi): {(SelectedRewardYearsYear?.Id == null ? "Tất cả" : SelectedRewardYearsYear.Name)}",
                    string.IsNullOrWhiteSpace(RewardYearsSearchText) ? null : $"Tìm kiếm: {RewardYearsSearchText.Trim()}",
                    $"Số dòng xuất: {selected.Count}"
                };

                var table = new List<IList<string>>
                {
                    new List<string> { "STT", "Cá nhân/Tập thể", "HT khen thưởng", "Cấp ban hành", "Liên tiếp" }
                };

                foreach (var r in selected)
                {
                    table.Add(new List<string>
                    {
                        r.No.ToString(),
                        r.SubjectName,
                        r.RewardFormName,
                        r.IssuingLevel,
                        r.ConsecutiveText
                    });
                }

                _exportService.ExportTableReport(sfd.FileName, title, filters, table);
                _messageService.Info("Xuất Word (.docx) thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thể xuất Word (.docx).\n" + ex.Message);
            }
        }

        private static List<RunInfo> FindConsecutiveRuns(IList<int> yearsSortedAsc)
        {
            var runs = new List<RunInfo>();
            if (yearsSortedAsc == null || yearsSortedAsc.Count == 0) return runs;

            var start = yearsSortedAsc[0];
            var prev = yearsSortedAsc[0];
            var len = 1;

            for (var i = 1; i < yearsSortedAsc.Count; i++)
            {
                var y = yearsSortedAsc[i];
                if (y == prev + 1)
                {
                    len++;
                    prev = y;
                }
                else
                {
                    runs.Add(new RunInfo { Start = start, End = prev, Length = len });
                    start = y;
                    prev = y;
                    len = 1;
                }
            }

            runs.Add(new RunInfo { Start = start, End = prev, Length = len });
            return runs;
        }

        private class RunInfo
        {
            public int Start { get; set; }
            public int End { get; set; }
            public int Length { get; set; }
        }

        #endregion

        #region TAB 4

        private void LoadTab4Filters()
        {
            SeniorityStatusOptions.Clear();
            SeniorityStatusOptions.Add("Tất cả");
            SeniorityStatusOptions.Add("Chưa trao");
            SeniorityStatusOptions.Add("Đã trao");

            SelectedSeniorityStatus = SeniorityStatusOptions.FirstOrDefault();

            RefreshSeniorityOrgUnitOptions();
            SelectedSeniorityOrgUnit = SeniorityOrgUnitOptions.FirstOrDefault();
        }

        private void RefreshSeniorityOrgUnitOptions()
        {
            var selectedNormalized = NormalizeOrgUnitName(SelectedSeniorityOrgUnit);

            var allOrgUnits = _orgUnitData.GetAll()
                .Where(u => !string.IsNullOrWhiteSpace(u?.Name))
                .Where(u => !OrgUnitData.IsInternalOrgUnit(u))
                .Select(u => NormalizeOrgUnitName(u.Name))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            _isRefreshingSeniorityOrgUnitOptions = true;
            try
            {
                SeniorityOrgUnitOptions.Clear();
                SeniorityOrgUnitOptions.Add("Tất cả");
                foreach (var orgUnit in allOrgUnits)
                    SeniorityOrgUnitOptions.Add(orgUnit);

                if (string.IsNullOrWhiteSpace(selectedNormalized)
                    || !SeniorityOrgUnitOptions.Any(x => string.Equals(NormalizeOrgUnitName(x), selectedNormalized, StringComparison.OrdinalIgnoreCase)))
                {
                    SelectedSeniorityOrgUnit = "Tất cả";
                }
                else
                {
                    var matched = SeniorityOrgUnitOptions.FirstOrDefault(x =>
                        string.Equals(NormalizeOrgUnitName(x), selectedNormalized, StringComparison.OrdinalIgnoreCase));
                    if (!string.Equals(SelectedSeniorityOrgUnit, matched, StringComparison.Ordinal))
                        SelectedSeniorityOrgUnit = matched;
                }
            }
            finally
            {
                _isRefreshingSeniorityOrgUnitOptions = false;
            }
        }

        private static List<int> MergeYearOptions(IEnumerable<int> years, bool ensureCurrentYear)
        {
            var merged = years?.Where(y => y > 0).Distinct().ToList() ?? new List<int>();

            if (ensureCurrentYear)
            {
                var currentYear = DateTime.Now.Year;
                merged.Add(currentYear);
            }

            if (merged.Count == 0)
            {
                merged.Add(DateTime.Now.Year);
            }

            return merged
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
        }

        private void RefreshSeniorityAwards(bool forceSync = false)
        {
            if (_isRefreshingSeniorityAwards)
                return;

            _isRefreshingSeniorityAwards = true;
            try
            {
                var yearToGenerate = DateTime.Now.Year;

                var asOfDate = DateTime.Today;

                if (forceSync)
                    _titleProposalData.GenerateOrUpdateForYear(yearToGenerate, asOfDate);

                var rows = _titleProposalData.GetList(null, SelectedSeniorityStatus, asOfDate);

                RefreshSeniorityOrgUnitOptions();

                if (!string.Equals(SelectedSeniorityOrgUnit, "Tất cả", StringComparison.OrdinalIgnoreCase))
                {
                    var selectedOrgUnit = NormalizeOrgUnitName(SelectedSeniorityOrgUnit);
                    rows = rows
                        .Where(r => string.Equals(NormalizeOrgUnitName(r.OrgUnit), selectedOrgUnit, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                var searchText = SeniorityOrgUnitSearchText?.Trim();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    rows = rows
                        .Where(r =>
                            (!string.IsNullOrWhiteSpace(r.FullName) && r.FullName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(r.Rank) && r.Rank.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(r.Position) && r.Position.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(r.OrgUnit) && r.OrgUnit.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(r.ProposedTitle) && r.ProposedTitle.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0))
                        .ToList();
                }

                SeniorityAwardRows.Clear();
                var no = 1;
                foreach (var r in rows)
                {
                    SeniorityAwardRows.Add(new SeniorityAwardDisplayModel
                    {
                        IsSelected = false,
                        No = no++,
                        Id = r.Id,
                        Year = r.ProposalYear,
                        SoldierId = r.SoldierId,
                        FullName = r.FullName,
                        Rank = r.Rank,
                        Position = r.Position,
                        OrgUnit = NormalizeOrgUnitName(r.OrgUnit),
                        EnlistmentDate = r.EnlistmentDate,
                        YearsOfService = r.YearsOfService,
                        YearsOfServiceText = r.YearsOfServiceText,
                        SuggestedReward = r.ProposedTitle,
                        Status = r.Status,
                        Note = r.Note
                    });
                }
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được đề xuất thâm niên.\n" + ex.Message);
            }
            finally
            {
                _isRefreshingSeniorityAwards = false;
            }
        }

        private static string NormalizeOrgUnitName(string orgUnit)
        {
            var normalized = (orgUnit ?? string.Empty)
                .Normalize(NormalizationForm.FormKC)
                .Replace("\u200B", string.Empty)
                .Replace("\uFEFF", string.Empty)
                .Trim();

            if (normalized.Length == 0)
                return string.Empty;

            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized.Normalize(NormalizationForm.FormKC);
        }

        private void MarkSelectedSeniorityAsAwarded()
        {
            try
            {
                var selectedIds = SeniorityAwardRows.Where(x => x.IsSelected).Select(x => x.Id).ToList();
                if (selectedIds.Count == 0)
                {
                    _messageService.Warning("Vui lòng tick chọn ít nhất 1 dòng để đánh dấu!");
                    return;
                }

                if (!_messageService.Confirm("Bạn có chắc chắn muốn đánh dấu các dòng đã chọn là 'Đã trao' ?"))
                    return;

                _titleProposalData.MarkAsAwarded(selectedIds);
                _messageService.Info("Cập nhật trạng thái thành công!");

                RefreshSeniorityAwards(forceSync: true);
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thể cập nhật trạng thái.\n" + ex.Message);
            }
        }

        #endregion
    }

    public class ReportFilterOption
    {
        public int? Id { get; set; }
        public string Name { get; set; }
    }

    public class RewardReportRowDisplayModel : BaseViewModel
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int No { get; set; }
        public string SubjectName { get; set; }
        public string RewardContentName { get; set; }
        public string RewardFormName { get; set; }
        public string DecisionNumber { get; set; } = string.Empty;
        public string IssuingLevel { get; set; }
        public DateTime? SignedDate { get; set; }
        public string Signer { get; set; }

        public string SignedDateText => SignedDate?.ToString("dd/MM/yyyy") ?? string.Empty;
    }

    public class UnitScoreAverageDisplayModel
    {
        public int No { get; set; }
        public int GroupId { get; set; }
        public string OrgUnitName { get; set; }
        public decimal? AverageScore { get; set; }
        public int Ranking { get; set; }

        public string AverageScoreText => AverageScore == null ? string.Empty : Math.Round(AverageScore.Value, 2).ToString("0.##");
    }

    public class RewardYearsRowDisplayModel : BaseViewModel
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int No { get; set; }
        public string SubjectName { get; set; }
        public string RewardFormName { get; set; }
        public string DecisionNumber { get; set; }
        public string IssuingLevel { get; set; }
        public string ConsecutiveText { get; set; }

        // for sorting
        public int ConsecutiveLength { get; set; }
        public int EndYear { get; set; }
    }

    public class SeniorityAwardDisplayModel : BaseViewModel
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int Id { get; set; }
        public int No { get; set; }
        public int Year { get; set; }
        public int SoldierId { get; set; }
        public string FullName { get; set; }
        public string Rank { get; set; }
        public string Position { get; set; }
        public string OrgUnit { get; set; }
        public DateTime? EnlistmentDate { get; set; }
        public int YearsOfService { get; set; }
        public string YearsOfServiceText { get; set; }
        public string SuggestedReward { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }

        public string EnlistmentDateText => EnlistmentDate?.ToString("dd/MM/yyyy") ?? "";

        public string PositionOrgUnit
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Position)) return OrgUnit ?? string.Empty;
                if (string.IsNullOrWhiteSpace(OrgUnit)) return Position ?? string.Empty;
                return $"{Position} / {OrgUnit}";
            }
        }
    }
}
