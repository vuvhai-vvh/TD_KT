using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using TD_KT.Data;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class WeeklyViewModel : BaseViewModel
    {
        private readonly OrgUnitData _orgUnitData;
        private readonly SoldierData _soldierData;
        private readonly WeeklyScoreData _weeklyScoreData;
        private readonly UnitScoreSummaryData _unitScoreSummaryData;
        private readonly WeekLockData _weekLockData;
        private readonly UserData _userData;
        private readonly UserPermissionFlags _permissionFlags;
        private readonly IMessageService _message;

        private List<OrgUnit> _allUnits = new List<OrgUnit>();
        private readonly Dictionary<string, string> _userDisplayNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ====== Lookups ======
        public ObservableCollection<int> Years { get; }
        public ObservableCollection<int> Months { get; }
        public ObservableCollection<FilterOption> ViolationMonths { get; }
        public ObservableCollection<WeekCalculator.WeekOption> ViolationWeeks { get; }

        public ObservableCollection<WeekCalculator.WeekOption> ScoreWeeks { get; }

        // Combobox "Đơn vị" (tab chấm điểm) – luôn hiển thị 14 đơn vị cố định
        public ObservableCollection<OrgUnit> Units { get; }

        // Combobox "Cơ quan/Đơn vị" cho lỗi tập thể
        public ObservableCollection<OrgUnit> CollectiveOrgUnits { get; }

        public ObservableCollection<OrgUnitFilterOption> ViolationOrgUnits { get; }
        public ObservableCollection<FilterOption> ViolationLevels { get; }
        public ObservableCollection<FilterOption> CommendationLevels { get; }

        private OrgUnitFilterOption _selectedViolationOrgUnit;
        public OrgUnitFilterOption SelectedViolationOrgUnit
        {
            get => _selectedViolationOrgUnit;
            set
            {
                if (SetProperty(ref _selectedViolationOrgUnit, value))
                    RefreshViolations();
            }
        }

        private OrgUnit _selectedCollectiveOrgUnit;
        public OrgUnit SelectedCollectiveOrgUnit
        {
            get => _selectedCollectiveOrgUnit;
            set => SetProperty(ref _selectedCollectiveOrgUnit, value);
        }
        // Combobox "Quân nhân" (tab vi phạm/biểu dương)
        public ObservableCollection<SoldierDisplayModel> Soldiers { get; }

        // ====== Tab: Danh sách vi phạm/biểu dương ======
        public ObservableCollection<WeeklyScoreDisplayModel> Violations { get; }
        private WeeklyScoreDisplayModel _selectedViolation;
        public WeeklyScoreDisplayModel SelectedViolation
        {
            get => _selectedViolation;
            set
            {
                if (SetProperty(ref _selectedViolation, value))
                {
                    if (!IsViolationEditing)
                        LoadViolationToForm(value);
                }
            }
        }

        private SoldierDisplayModel _selectedSoldier;
        public SoldierDisplayModel SelectedSoldier
        {
            get => _selectedSoldier;
            set
            {
                if (SetProperty(ref _selectedSoldier, value))
                {
                    // Khi đang nhập/sửa thì đồng bộ các ô hiển thị theo quân nhân
                    if (IsViolationEditing)
                    {
                        // Không cần set gì thêm vì XAML bind RankName/PositionName/OrgUnitName
                        // Nhưng vẫn gọi Notify để UI cập nhật tức thì trong một số trường hợp.
                        OnPropertyChanged(nameof(SelectedSoldier));
                    }
                }
            }
        }

        // ====== Form nhập liệu vi phạm/biểu dương ======
        private bool _isViolationEditing;
        public bool IsViolationEditing
        {
            get => _isViolationEditing;
            private set => SetProperty(ref _isViolationEditing, value);
        }

        private bool _isCollectiveViolation;
        public bool IsCollectiveViolation
        {
            get => _isCollectiveViolation;
            set => SetProperty(ref _isCollectiveViolation, value);
        }

        private string _violationContentInput = "";
        public string ViolationContentInput
        {
            get => _violationContentInput;
            set => SetProperty(ref _violationContentInput, value);
        }

        private int _selectedViolationLevel;
        private bool _isUpdatingViolationLevel;
        public int SelectedViolationLevel
        {
            get => _selectedViolationLevel;
            set
            {
                if (!SetProperty(ref _selectedViolationLevel, value))
                    return;

                if (_isUpdatingViolationLevel)
                    return;

                _isUpdatingViolationLevel = true;
                SelectedViolationLevelOption = ViolationLevels.FirstOrDefault(v => v.Value == value);
                _isUpdatingViolationLevel = false;
            }
        }

        private FilterOption _selectedViolationLevelOption;
        public FilterOption SelectedViolationLevelOption
        {
            get => _selectedViolationLevelOption;
            set
            {
                if (!SetProperty(ref _selectedViolationLevelOption, value))
                    return;

                if (_isUpdatingViolationLevel)
                    return;

                _isUpdatingViolationLevel = true;
                SelectedViolationLevel = value?.Value ?? 0;
                _isUpdatingViolationLevel = false;
            }
        }

        private string _commendationContentInput = "";
        public string CommendationContentInput
        {
            get => _commendationContentInput;
            set => SetProperty(ref _commendationContentInput, value);
        }

        private int _selectedCommendationLevel;
        private bool _isUpdatingCommendationLevel;
        public int SelectedCommendationLevel
        {
            get => _selectedCommendationLevel;
            set
            {
                if (!SetProperty(ref _selectedCommendationLevel, value))
                    return;

                if (_isUpdatingCommendationLevel)
                    return;

                _isUpdatingCommendationLevel = true;
                SelectedCommendationLevelOption = CommendationLevels.FirstOrDefault(v => v.Value == value);
                _isUpdatingCommendationLevel = false;
            }
        }

        private FilterOption _selectedCommendationLevelOption;
        public FilterOption SelectedCommendationLevelOption
        {
            get => _selectedCommendationLevelOption;
            set
            {
                if (!SetProperty(ref _selectedCommendationLevelOption, value))
                    return;

                if (_isUpdatingCommendationLevel)
                    return;

                _isUpdatingCommendationLevel = true;
                SelectedCommendationLevel = value?.Value ?? 0;
                _isUpdatingCommendationLevel = false;
            }
        }

        private DateTime? _recordDate;
        public DateTime? RecordDate
        {
            get => _recordDate;
            set
            {
                if (SetProperty(ref _recordDate, value) && !_isAutoUpdatingRecordDateTime)
                    IsRecordDateTimeAuto = false;
            }
        }

        private string _recordTimeText = "";
        public string RecordTimeText
        {
            get => _recordTimeText;
            set
            {
                if (SetProperty(ref _recordTimeText, value) && !_isAutoUpdatingRecordDateTime)
                    IsRecordDateTimeAuto = false;
            }
        }

        private bool _isRecordDateTimeAuto = true;
        public bool IsRecordDateTimeAuto
        {
            get => _isRecordDateTimeAuto;
            set => SetProperty(ref _isRecordDateTimeAuto, value);
        }

        private bool _isAutoUpdatingRecordDateTime;

        private int _selectedYearViolation;
        public int SelectedYearViolation
        {
            get => _selectedYearViolation;
            set
            {
                if (SetProperty(ref _selectedYearViolation, value))
                {
                    BuildViolationWeeks(_selectedYearViolation, _selectedMonthViolation);
                    RefreshViolations();
                }
            }
        }

        private int _selectedMonthViolation;
        public int SelectedMonthViolation
        {
            get => _selectedMonthViolation;
            set
            {
                if (SetProperty(ref _selectedMonthViolation, value))
                {
                    BuildViolationWeeks(_selectedYearViolation, _selectedMonthViolation);
                    RefreshViolations();
                }
            }
        }

        private WeekCalculator.WeekOption _selectedViolationWeek;
        public WeekCalculator.WeekOption SelectedViolationWeek
        {
            get => _selectedViolationWeek;
            set
            {
                if (SetProperty(ref _selectedViolationWeek, value))
                    RefreshViolations();
            }
        }

        private int? _selectedAreaTypeViolation;
        public int? SelectedAreaTypeViolation
        {
            get => _selectedAreaTypeViolation;
            set
            {
                if (SetProperty(ref _selectedAreaTypeViolation, value))
                    RefreshViolations();
            }
        }

        // ====== Tab: Chấm điểm thi đua tuần ======
        public ObservableCollection<WeeklyUnitScoreDisplayModel> UnitScores { get; }
        public ObservableCollection<WeeklyScoreDisplayModel> UnitWeeklyScores { get; }

        private bool _isFinalized;
        public bool IsFinalized
        {
            get => _isFinalized;
            private set
            {
                if (SetProperty(ref _isFinalized, value))
                    OnPropertyChanged(nameof(WeekStatusText));
            }
        }

        private bool _isWeekLocked;
        public bool IsWeekLocked
        {
            get => _isWeekLocked;
            private set
            {
                if (SetProperty(ref _isWeekLocked, value))
                    OnPropertyChanged(nameof(WeekStatusText));
            }
        }

        public string WeekStatusText => IsWeekLocked ? "Đã khóa" : (IsFinalized ? "Đã chốt" : "Chưa chốt");

        // 14 đơn vị cố định theo yêu cầu (thứ tự hiển thị giữ nguyên)
        private static readonly string[] FIXED_UNIT_NAMES = new[]
        {
            // Nhóm 1
            "Cụm 1",
            "Cụm 2",
            "Cụm 3",

            // Nhóm 2
            "Phòng 6",
            "Phòng 7",
            "Phòng 8",

            // Nhóm 3
            "Phòng Chính trị",
            "Phòng Tham mưu",
            "Phòng Hậu cần - Kỹ thuật",

            // Nhóm 4 (các Trạm)
            "Ban Tài chính",
            "Trạm 15",
            "Trạm 16",
            "Trạm 17",
            "Trạm 24",
        };

        private static readonly HashSet<string> GROUP_1 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cụm 1", "Cụm 2", "Cụm 3"
        };

        private static readonly HashSet<string> GROUP_2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Phòng 6", "Phòng 7", "Phòng 8"
        };

        private static readonly HashSet<string> GROUP_3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Phòng Chính trị", "Phòng Tham mưu", "Phòng Hậu cần - Kỹ thuật"
        };

        private static readonly HashSet<string> GROUP_4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Ban Tài chính","Trạm 15", "Trạm 16", "Trạm 17", "Trạm 24"
        };

        private static int GetGroupIdByUnitName(string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName)) return 0;
            if (GROUP_1.Contains(unitName)) return 1;
            if (GROUP_2.Contains(unitName)) return 2;
            if (GROUP_3.Contains(unitName)) return 3;
            if (GROUP_4.Contains(unitName)) return 4;
            return 0;
        }

        public WeeklyViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService())
        {
        }

        public WeeklyViewModel(IConnectionStringProvider csProvider, IMessageService messageService)
        {
            if (csProvider == null) csProvider = new ConnectionStringProvider("Database");
            _message = messageService ?? new MessageService();

            _orgUnitData = new OrgUnitData(csProvider);
            _soldierData = new SoldierData(csProvider);
            _weeklyScoreData = new WeeklyScoreData(csProvider);
            _unitScoreSummaryData = new UnitScoreSummaryData(csProvider);
            _weekLockData = new WeekLockData(csProvider);
            _userData = new UserData(csProvider);
            _permissionFlags = LoadPermissionFlags(csProvider);

            Years = new ObservableCollection<int>();
            Months = new ObservableCollection<int>();
            ViolationMonths = new ObservableCollection<FilterOption>();
            ViolationWeeks = new ObservableCollection<WeekCalculator.WeekOption>();
            ScoreWeeks = new ObservableCollection<WeekCalculator.WeekOption>();
            Units = new ObservableCollection<OrgUnit>();
            CollectiveOrgUnits = new ObservableCollection<OrgUnit>();
            ViolationOrgUnits = new ObservableCollection<OrgUnitFilterOption>();
            ViolationLevels = new ObservableCollection<FilterOption>();
            CommendationLevels = new ObservableCollection<FilterOption>();
            Soldiers = new ObservableCollection<SoldierDisplayModel>();

            Violations = new ObservableCollection<WeeklyScoreDisplayModel>();
            UnitScores = new ObservableCollection<WeeklyUnitScoreDisplayModel>();
            UnitWeeklyScores = new ObservableCollection<WeeklyScoreDisplayModel>();

            // Lookups
            var now = DateTime.Now;
            LoadYearOptions(now.Year);
            for (int m = 1; m <= 12; m++) Months.Add(m);

            ViolationMonths.Add(new FilterOption { Value = 0, DisplayText = "Tất cả" });
            for (int m = 1; m <= 12; m++)
                ViolationMonths.Add(new FilterOption { Value = m, DisplayText = $"Tháng {m}" });

            ViolationLevels.Add(new FilterOption { Value = 1, DisplayText = "vi phạm thông thường (- 0,25)" });
            ViolationLevels.Add(new FilterOption { Value = 2, DisplayText = "vi phạm phải xử lý KL (-0,5)" });
            ViolationLevels.Add(new FilterOption { Value = 3, DisplayText = "vi phạm nghiệm trọng (-1,0)" });

            CommendationLevels.Add(new FilterOption { Value = 1, DisplayText = "Cấp Trung tâm (+0,25)" });
            CommendationLevels.Add(new FilterOption { Value = 2, DisplayText = "Cấp BTM và tương đương (+0,5)" });
            CommendationLevels.Add(new FilterOption { Value = 3, DisplayText = "Cấp Quân chủng (+1,0)" });

            // Default filter for tab vi phạm/biểu dương
            _selectedYearViolation = now.Year;
            _selectedMonthViolation = 0;
            _selectedViolationWeek = null;
            _selectedAreaTypeViolation = null;
            _isUpdatingViolationLevel = true;
            _selectedViolationLevel = 1;
            _selectedViolationLevelOption = ViolationLevels.FirstOrDefault(v => v.Value == 1);
            _isUpdatingViolationLevel = false;
            _isUpdatingCommendationLevel = true;
            _selectedCommendationLevel = 1;
            _selectedCommendationLevelOption = CommendationLevels.FirstOrDefault(v => v.Value == 1);
            _isUpdatingCommendationLevel = false;

            BuildViolationWeeks(_selectedYearViolation, _selectedMonthViolation);
            BuildScoreWeeks(now.Year, now.Month);
            RecordDate = now.Date;
            IsRecordDateTimeAuto = true;
            UpdateRecordDateTimeToNow();

            LoadOrgUnitsAndEnsureFixed();
            LoadSoldiers();
            LoadUserDisplayNames();

            // Load tab 2 list immediately
            RefreshViolations();
        }

        private void LoadYearOptions(int currentYear)
        {
            Years.Clear();

            var yearSet = new HashSet<int>
            {
                2023,
                2024,
                2025
            };

            for (int y = currentYear - 2; y <= currentYear + 1; y++)
                yearSet.Add(y);

            foreach (var year in yearSet.OrderBy(y => y))
                Years.Add(year);
        }
        private void LoadSoldiers()
        {
            Soldiers.Clear();
            foreach (var s in _soldierData.GetAllDisplay())
                Soldiers.Add(s);
        }

        private void LoadOrgUnitsAndEnsureFixed()
        {
            _allUnits = _orgUnitData.GetAll();

            EnsureFixedUnitsExistInDb();

            // reload after insert
            _allUnits = _orgUnitData.GetAll();

            // Units combobox (tab 3): đúng 14 đơn vị cố định theo thứ tự FIXED_UNIT_NAMES
            Units.Clear();
            foreach (var name in FIXED_UNIT_NAMES)
            {
                var unit = _allUnits.FirstOrDefault(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
                if (unit != null) Units.Add(unit);
            }
            LoadCollectiveOrgUnits();
            LoadViolationOrgUnits();
        }

        private void LoadCollectiveOrgUnits()
        {
            CollectiveOrgUnits.Clear();

            foreach (var unit in _allUnits
                .Where(u => !string.IsNullOrWhiteSpace(u.Name))
                .Where(u => !OrgUnitData.IsInternalOrgUnit(u))
                .OrderBy(u => u.Name))
            {
                CollectiveOrgUnits.Add(unit);
            }

            if (SelectedCollectiveOrgUnit == null)
                SelectedCollectiveOrgUnit = CollectiveOrgUnits.FirstOrDefault();
        }

        private void LoadViolationOrgUnits()
        {
            ViolationOrgUnits.Clear();

            ViolationOrgUnits.Add(new OrgUnitFilterOption { Id = null, Name = "Tất cả", IsAll = true });

            foreach (var unit in _allUnits
                .Where(u => !string.IsNullOrWhiteSpace(u.Name))
                .Where(u => !OrgUnitData.IsInternalOrgUnit(u))
                .OrderBy(u => u.Name))
            {
                ViolationOrgUnits.Add(new OrgUnitFilterOption { Id = unit.Id, Name = unit.Name, IsAll = false });
            }

            if (SelectedViolationOrgUnit == null)
                SelectedViolationOrgUnit = ViolationOrgUnits.FirstOrDefault();
        }

        private void EnsureFixedUnitsExistInDb()
        {
            // tạo (nếu thiếu) 1 root để gom các đơn vị "cứng" cho chấm điểm
            const string rootCode = "CDTD";
            const string rootName = "Chấm điểm thi đua";

            var root = _allUnits.FirstOrDefault(u =>
                string.Equals(u.Code, rootCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Name, rootName, StringComparison.OrdinalIgnoreCase));

            if (root == null)
            {
                _orgUnitData.Insert(rootCode, rootName, parentId: null, note: "Tự tạo để chứa các đơn vị chấm điểm (cố định) ");
                _allUnits = _orgUnitData.GetAll();
                root = _allUnits.FirstOrDefault(u => string.Equals(u.Code, rootCode, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var name in FIXED_UNIT_NAMES)
            {
                var exists = _allUnits.Any(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exists) continue;

                // Code đơn giản: bỏ dấu + thay khoảng trắng -> _
                var code = "CD_" + NormalizeCode(name);
                _orgUnitData.Insert(code, name, parentId: root?.Id, note: "Đơn vị chấm điểm (cứng)");
            }
        }

        private static string NormalizeCode(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "UNIT";
            var s = name.Trim().ToUpperInvariant();

            // bỏ ký tự không phù hợp cho Code
            var keep = s.Select(ch =>
            {
                if (char.IsLetterOrDigit(ch)) return ch;
                if (ch == ' ' || ch == '-' || ch == '_') return '_';
                return '_';
            }).ToArray();

            // gộp "__"
            var t = new string(keep);
            while (t.Contains("__")) t = t.Replace("__", "_");
            return t.Trim('_');
        }

        // ====== TAB 2 ======
        public void RefreshViolations()
        {
            if (SelectedYearViolation <= 0 || SelectedViolationWeek == null) return;

            Violations.Clear();

            var orgUnitNames = GetViolationOrgUnitFilterNames();
            var rows = _weeklyScoreData.GetByFilter(
                year: SelectedYearViolation,
                month: SelectedMonthViolation,
                week: SelectedViolationWeek.WeekNoInMonth,
                areaType: SelectedAreaTypeViolation,
                orgUnitNames: orgUnitNames);

            int no = 1;
            foreach (var r in rows)
            {
                Violations.Add(new WeeklyScoreDisplayModel
                {
                    No = no++,
                    Id = r.Id,
                    Year = r.Year,
                    Month = r.Month,
                    Week = r.Week,
                    SoldierId = r.SoldierId,
                    AreaType = r.AreaType,
                    FullName = r.FullName,
                    Rank = r.Rank,
                    Position = r.Position,
                    OrgUnit = r.OrgUnit,
                    ViolationContent = r.ViolationContent,
                    ViolationLevel = r.ViolationLevel,
                    ViolationContentDisplay = BuildViolationDisplay(r.ViolationContent, r.ViolationLevel),
                    CommendationContent = r.CommendationContent,
                    RecordDateTime = r.RecordDateTime,
                    EnteredBy = GetDisplayName(r.EnteredBy)
                });
            }

            // Nếu đang không nhập/sửa: đồng bộ form theo dòng đang chọn (hoặc dòng đầu tiên)
            if (!IsViolationEditing)
            {
                if (SelectedViolation != null)
                    LoadViolationToForm(SelectedViolation);
                else if (Violations.Count > 0)
                    LoadViolationToForm(Violations[0]);
                else
                    ClearViolationForm();
            }
        }

        private List<string> GetViolationOrgUnitFilterNames()
        {
            if (SelectedViolationOrgUnit == null || SelectedViolationOrgUnit.IsAll)
                return null;

            if (SelectedViolationOrgUnit.Id.HasValue)
                return GetDescendantNames(SelectedViolationOrgUnit.Id.Value);

            return new List<string> { SelectedViolationOrgUnit.Name };
        }

        private void BuildViolationWeeks(int year, int month)
        {
            ViolationWeeks.Clear();
            ViolationWeeks.Add(new WeekCalculator.WeekOption
            {
                WeekNoInMonth = 0,
                DisplayText = "Tất cả"
            });

            if (month > 0)
            {
                foreach (var w in WeekCalculator.GetWeeksForMonth(year, month)
                    .Where(x => x != null
                                && x.WeekNoInMonth > 0
                                && !string.Equals(x.DisplayText, "Tất cả", StringComparison.OrdinalIgnoreCase)))
                {
                    ViolationWeeks.Add(w);
                }
            }

            if (ViolationWeeks.Count == 0)
            {
                SelectedViolationWeek = null;
                return;
            }

            SelectedViolationWeek = ViolationWeeks.FirstOrDefault(x => x.WeekNoInMonth == 0) ?? ViolationWeeks.First();
        }

        public void BuildScoreWeeks(int year, int month)
        {
            ScoreWeeks.Clear();
            // Ensure week filter never shows the legacy "Tất cả" option (if it exists in older builds)
            foreach (var w in WeekCalculator.GetWeeksForMonth(year, month)
                .Where(x => x != null
                            && x.WeekNoInMonth > 0
                            && !string.Equals(x.DisplayText, "Tất cả", StringComparison.OrdinalIgnoreCase)))
            {
                ScoreWeeks.Add(w);
            }
        }


        public void BeginAddViolation()
        {
            if (!CanEditViolation())
                return;

            if (!SelectedAreaTypeViolation.HasValue)
            {
                _message.Warning("Vui lòng chọn khu vực (Tham mưu/Chính trị/HC-KT/Nghiệp vụ) trước khi thêm.");
                return;
            }

            IsViolationEditing = true;
            SelectedViolation = null;

            IsCollectiveViolation = false;
            SelectedSoldier = null;
            ViolationContentInput = "";
            SelectedViolationLevel = 0;
            SelectedViolationLevelOption = ViolationLevels.FirstOrDefault(v => v.Value == 0);
            CommendationContentInput = "";

            var now = DateTime.Now;
            RecordDate = now.Date;
            RecordTimeText = now.ToString("HH:mm");
        }

        public void BeginEditViolation()
        {
            if (!CanEditViolation())
                return;

            if (SelectedViolation == null)
            {
                _message.Warning("Vui lòng chọn 1 dòng để sửa.");
                return;
            }

            IsViolationEditing = true;
            LoadViolationToForm(SelectedViolation);
        }

        public void CancelViolationEdit()
        {
            IsViolationEditing = false;
            if (SelectedViolation != null) LoadViolationToForm(SelectedViolation);
            else ClearViolationForm();
        }

        public void DeleteSelectedViolation()
        {
            if (SelectedViolation == null)
            {
                _message.Warning("Vui lòng chọn 1 dòng để xóa.");
                return;
            }

            if (!CanEditViolation())
                return;

            if (!_message.Confirm($"Bạn có chắc muốn xóa dòng '{SelectedViolation.FullName}'?"))
                return;

            try
            {
                _weeklyScoreData.Delete(SelectedViolation.Id);
                SelectedViolation = null;
                RefreshViolations();
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi xóa dữ liệu: {ex.Message}");
            }
        }

        public void SaveViolation()
        {
            if (!CanEditViolation())
                return;

            if (!SelectedAreaTypeViolation.HasValue)
            {
                _message.Warning("Vui lòng chọn khu vực (Tham mưu/Chính trị/HC-KT/Nghiệp vụ) trước khi lưu.");
                return;
            }

            // Đồng bộ từ Quân nhân (bám DB)
            int? soldierId = null;
            string fullName = "";
            string rank = "";
            string position = "";
            string orgUnit = "";

            if (!IsCollectiveViolation)
            {
                if (SelectedSoldier == null)
                {
                    _message.Warning("Vui lòng chọn quân nhân.");
                    return;
                }

                soldierId = SelectedSoldier.Id;
                fullName = SelectedSoldier.FullName ?? "";
                rank = SelectedSoldier.Rank ?? "";
                position = SelectedSoldier.Position ?? "";
                orgUnit = SelectedSoldier.OrgUnit ?? "";
            }
            else
            {
                // Nếu là lỗi tập thể: bắt buộc vẫn có họ tên (theo DB WeeklyScores.FullName NOT NULL).
                // Ở UI hiện tại không có ô nhập họ tên tập thể riêng => dùng Quân nhân nếu có, còn không thì chặn.
                if (SelectedCollectiveOrgUnit == null)
                {
                    _message.Warning("Vui lòng chọn Cơ quan/Đơn vị cho lỗi tập thể.");
                    return;
                }

                soldierId = null;
                fullName = SelectedCollectiveOrgUnit.Name ?? "";
                rank = "";
                position = "";
                orgUnit = SelectedCollectiveOrgUnit.Name ?? "";
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                _message.Warning("Họ tên không hợp lệ.");
                return;
            }

            var trimmedViolationContent = (ViolationContentInput ?? "").Trim();
            var trimmedCommendationContent = (CommendationContentInput ?? "").Trim();

            if (string.IsNullOrWhiteSpace(trimmedViolationContent) && string.IsNullOrWhiteSpace(trimmedCommendationContent))
            {
                _message.Warning("Vui lòng nhập nội dung vi phạm hoặc nội dung biểu dương.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(trimmedViolationContent) && !string.IsNullOrWhiteSpace(trimmedCommendationContent))
            {
                _message.Warning("Mỗi lần nhập chỉ chọn một loại: vi phạm hoặc biểu dương.");
                return;
            }

            var selectedViolationLevel = SelectedViolationLevel;
            if (!string.IsNullOrWhiteSpace(trimmedViolationContent) && selectedViolationLevel <= 0)
            {
                _message.Warning("Vui lòng chọn mức độ vi phạm.");
                return;
            }

            var selectedCommendationLevel = SelectedCommendationLevel;
            if (!string.IsNullOrWhiteSpace(trimmedCommendationContent) && selectedCommendationLevel <= 0)
            {
                _message.Warning("Vui lòng chọn cấp biểu dương/khen thưởng.");
                return;
            }

            var violationLevelToSave = !string.IsNullOrWhiteSpace(trimmedViolationContent)
                ? selectedViolationLevel
                : selectedCommendationLevel;

            // parse datetime
            DateTime recordDateTime;
            if (!RecordDate.HasValue) RecordDate = DateTime.Now.Date;

            var timeText = (RecordTimeText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(timeText)) timeText = "00:00";
            if (!TimeSpan.TryParse(timeText, out var time))
            {
                _message.Warning("Giờ không đúng định dạng HH:mm.");
                return;
            }
            recordDateTime = RecordDate.Value.Date.Add(time);

            // Tính Year/Month/Week theo logic: tuần bắt đầu Thứ 2 và quy tắc >=4 ngày thuộc tháng đó
            var assign = WeekCalculator.GetAssignment(recordDateTime);
            var saveYear = assign.AssignedYear;
            var saveMonth = assign.AssignedMonth;
            var saveWeek = assign.WeekNoInAssignedMonth;

            if (IsViolationWeekLocked(saveYear, saveMonth, saveWeek))
            {
                _message.Warning("Tháng/tuần của dữ liệu này đã khóa, không thể thêm/sửa.");
                return;
            }

            if (IsViolationWeekFinalized(saveYear, saveMonth, saveWeek) && !_permissionFlags.CanLockWeek)
            {
                _message.Warning("Tháng/tuần của dữ liệu này đã chốt điểm, không thể thêm/sửa.");
                return;
            }

            var enteredBy = (AppSession.CurrentUser != null && !string.IsNullOrWhiteSpace(AppSession.CurrentUser.Username))
                            ? AppSession.CurrentUser.Username
                            : "";

            try
            {
                var areaType = SelectedAreaTypeViolation.Value;

                if (SelectedViolation == null)
                {
                    var newId = _weeklyScoreData.Insert(
                        year: saveYear,
                        month: saveMonth,
                        week: saveWeek,
                        areaType: areaType,
                        soldierId: soldierId,
                        fullName: fullName,
                        rank: rank,
                        position: position,
                        orgUnit: orgUnit,
                        violationContent: trimmedViolationContent,
                        violationLevel: violationLevelToSave,
                        commendationContent: trimmedCommendationContent,
                        recordDateTime: recordDateTime,
                        enteredBy: enteredBy);

                    RefreshViolations();
                    SelectedViolation = Violations.FirstOrDefault(x => x.Id == newId);
                }
                else
                {
                    _weeklyScoreData.Update(
                        id: SelectedViolation.Id,
                        areaType: areaType,
                        soldierId: soldierId,
                        fullName: fullName,
                        rank: rank,
                        position: position,
                        orgUnit: orgUnit,
                        violationContent: trimmedViolationContent,
                        violationLevel: violationLevelToSave,
                        commendationContent: trimmedCommendationContent,
                        recordDateTime: recordDateTime,
                        enteredBy: enteredBy);

                    var keepId = SelectedViolation.Id;
                    RefreshViolations();
                    SelectedViolation = Violations.FirstOrDefault(x => x.Id == keepId);
                }

                IsViolationEditing = false;
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi lưu dữ liệu: {ex.Message}");
            }
        }

        private void LoadViolationToForm(WeeklyScoreDisplayModel item)
        {
            if (item == null)
            {
                ClearViolationForm();
                return;
            }

            IsRecordDateTimeAuto = false;

            // map selected soldier by SoldierId (ưu tiên), fallback theo FullName
            SoldierDisplayModel soldier = null;
            if (item.SoldierId.HasValue)
                soldier = Soldiers.FirstOrDefault(s => s.Id == item.SoldierId.Value);
            if (soldier == null && !string.IsNullOrWhiteSpace(item.FullName))
                soldier = Soldiers.FirstOrDefault(s => string.Equals(s.FullName, item.FullName, StringComparison.OrdinalIgnoreCase));

            SelectedSoldier = soldier;
            IsCollectiveViolation = !item.SoldierId.HasValue;
            if (IsCollectiveViolation)
            {
                SelectedCollectiveOrgUnit = CollectiveOrgUnits.FirstOrDefault(u =>
                    string.Equals(u.Name, item.OrgUnit, StringComparison.OrdinalIgnoreCase));
            }
            ViolationContentInput = item.ViolationContent ?? "";
            CommendationContentInput = item.CommendationContent ?? "";

            if (!string.IsNullOrWhiteSpace(item.ViolationContent))
            {
                SelectedViolationLevel = item.ViolationLevel;
                SelectedViolationLevelOption = ViolationLevels.FirstOrDefault(v => v.Value == item.ViolationLevel);
                SelectedCommendationLevel = 0;
                SelectedCommendationLevelOption = CommendationLevels.FirstOrDefault(v => v.Value == 0);
            }
            else
            {
                SelectedViolationLevel = 0;
                SelectedViolationLevelOption = ViolationLevels.FirstOrDefault(v => v.Value == 0);
                SelectedCommendationLevel = item.ViolationLevel;
                SelectedCommendationLevelOption = CommendationLevels.FirstOrDefault(v => v.Value == item.ViolationLevel);
            }

            if (item.RecordDateTime.HasValue)
            {
                RecordDate = item.RecordDateTime.Value.Date;
                RecordTimeText = item.RecordDateTime.Value.ToString("HH:mm");
            }
            else
            {
                UpdateRecordDateTimeToNow();
            }
        }

        private void ClearViolationForm()
        {
            SelectedSoldier = null;
            IsCollectiveViolation = false;
            ViolationContentInput = "";
            SelectedViolationLevel = 0;
            SelectedViolationLevelOption = ViolationLevels.FirstOrDefault(v => v.Value == 0);
            CommendationContentInput = "";
            SelectedCommendationLevel = 0;
            SelectedCommendationLevelOption = CommendationLevels.FirstOrDefault(v => v.Value == 0);
            SelectedCollectiveOrgUnit = CollectiveOrgUnits.FirstOrDefault();

            IsRecordDateTimeAuto = true;
            UpdateRecordDateTimeToNow();
        }

        public void UpdateRecordDateTimeToNow()
        {
            if (!IsViolationEditing || !IsRecordDateTimeAuto)
                return;

            _isAutoUpdatingRecordDateTime = true;
            var now = DateTime.Now;
            RecordDate = now.Date;
            RecordTimeText = now.ToString("HH:mm");
            _isAutoUpdatingRecordDateTime = false;
        }

        // ====== TAB 3 ======
        // Lưu ý: Tab chấm điểm thi đua tuần tính tự động từ bảng WeeklyScores.
        // Nút "Lưu điểm" không dùng nữa (UI đã bỏ), nhưng vẫn giữ hàm SaveUnitScores để tránh lỗi build nếu code-behind còn gọi.

        // Wrapper cũ (giữ lại để không ảnh hưởng chỗ khác): mặc định Week=1
        public void LoadUnitScores(int year, int month) => LoadUnitScores(year, month, 1);

        public void LoadUnitScores(int year, int month, int week)
        {
            ResetUnitScoresSorting();

            UnitScores.Clear();

            if (week <= 0) week = 1; // không hỗ trợ "Tất cả" trong lọc tuần

            var savedRows = SafeGetSavedSummary(year, month, week);
            var isFinalized = savedRows != null
                              && savedRows.Count >= Units.Count
                              && savedRows.All(r => r.Ranking.HasValue);
            IsFinalized = isFinalized;

            IsWeekLocked = _weekLockData.IsLocked(year, month, week);

            if (IsWeekLocked && savedRows != null && savedRows.Count > 0)
            {
                LoadUnitScoresFromSaved(year, month, week, savedRows!);
                return;
            }

            // Chưa chốt: tính tự động theo vi phạm/biểu dương
            ComputeAndFillUnitScores(year, month, week);
        }

        // Wrapper cũ: mặc định Week=1
        public void SaveUnitScores(int year, int month) => SaveUnitScores(year, month, 1);

        public void SaveUnitScores(int year, int month, int week)
        {
            if (week <= 0)
                throw new InvalidOperationException("Vui lòng chọn 1 tuần cụ thể.");

            if (IsFinalized)
                throw new InvalidOperationException("Tuần này đã chốt, không thể lưu/chỉnh sửa.");

            // Vì điểm được tính tự động, Save chỉ có ý nghĩa lưu tạm (Ranking = NULL)
            if (UnitScores.Count == 0)
                ComputeAndFillUnitScores(year, month, week);

            foreach (var item in UnitScores)
            {
                _unitScoreSummaryData.Upsert(
                    year, month, week, item.OrgUnitId,
                    item.ScoreArea1, item.ScoreArea2, item.ScoreArea3, item.ScoreArea4,
                    item.AverageScore, null);
            }

            // vẫn là chưa chốt
            IsFinalized = false;
        }

        // Wrapper cũ: mặc định Week=1
        public void FinalizeScores(int year, int month) => FinalizeScores(year, month, 1);

        public void FinalizeScores(int year, int month, int week)
        {
            if (week <= 0)
                throw new InvalidOperationException("Vui lòng chọn 1 tuần cụ thể.");

            // Nếu đã chốt thì không làm lại
            var savedRows = SafeGetSavedSummary(year, month, week);
            var alreadyFinalized = savedRows != null
                                   && savedRows.Count >= Units.Count
                                   && savedRows.All(r => r.Ranking.HasValue);
            if (alreadyFinalized)
            {
                LoadUnitScoresFromSaved(year, month, week, savedRows!);
                IsFinalized = true;
                return;
            }

            // Tính tự động trước khi chốt
            ComputeAndFillUnitScores(year, month, week);

            // Xếp hạng theo từng Nhóm (desc). Ranking bắt đầu từ 1 trong mỗi nhóm.
            foreach (var grp in UnitScores.GroupBy(x => x.GroupId).OrderBy(g => g.Key))
            {
                var ordered = grp
                    .OrderByDescending(x => x.AverageScore ?? decimal.MinValue)
                    .ThenBy(x => x.OrgUnitName)
                    .ToList();

                int rank = 1;
                foreach (var item in ordered)
                {
                    item.Ranking = rank++;
                    _unitScoreSummaryData.Upsert(
                        year, month, week, item.OrgUnitId,
                        item.ScoreArea1, item.ScoreArea2, item.ScoreArea3, item.ScoreArea4,
                        item.AverageScore, item.Ranking);
                }
            }

            IsFinalized = true;
        }

        public void LockWeek(int year, int month, int week)
        {
            if (week <= 0)
                throw new InvalidOperationException("Vui lòng chọn 1 tuần cụ thể.");

            if (IsWeekLocked)
                throw new InvalidOperationException("Tuần này đã khóa.");

            if (!IsFinalized)
                FinalizeScores(year, month, week);

            var lockedBy = (AppSession.CurrentUser != null && !string.IsNullOrWhiteSpace(AppSession.CurrentUser.Username))
                ? AppSession.CurrentUser.Username
                : "";
            _weekLockData.LockWeek(year, month, week, lockedBy);
            IsWeekLocked = true;
        }

        private List<UnitScoreSummaryRow> SafeGetSavedSummary(int year, int month, int week)
        {
            try
            {
                return _unitScoreSummaryData.GetByYearMonthWeek(year, month, week);
            }
            catch
            {
                // Nếu DB chưa có cột Week thì sẽ lỗi ở đây; để bám sát DB thì DB cần có Week.
                return null;
            }
        }

        private void LoadUnitScoresFromSaved(int year, int month, int week, List<UnitScoreSummaryRow> savedRows)
        {
            UnitScores.Clear();

            foreach (var unit in Units)
            {
                var found = savedRows.FirstOrDefault(r => r.OrgUnitId == unit.Id);

                // tính lại Tổng vi phạm + Điểm cộng từ WeeklyScores để hiển thị đúng (DB summary không lưu 2 cột này)
                var (s1, s2, s3, s4, totalViol, bonus) = ComputeScoresForUnit(year, month, week, unit.Id);

                var vm = new WeeklyUnitScoreDisplayModel(unit.Id, unit.Name, GetGroupIdByUnitName(unit.Name));
                if (found != null)
                {
                    vm.SetScores(found.ScoreArea1, found.ScoreArea2, found.ScoreArea3, found.ScoreArea4,
                        found.AverageScore, found.Ranking,
                        totalViolations: totalViol,
                        bonusPoints: bonus);
                }
                else
                {
                    // nếu DB thiếu dòng thì vẫn hiển thị theo tính tự động
                    vm.SetComputedScores(s1, s2, s3, s4, totalViol, bonus, ranking: null);
                }

                UnitScores.Add(vm);
            }
        }

        private void ComputeAndFillUnitScores(int year, int month, int week)
        {
            UnitScores.Clear();

            foreach (var unit in Units)
            {
                var (s1, s2, s3, s4, totalViol, bonus) = ComputeScoresForUnit(year, month, week, unit.Id);

                var vm = new WeeklyUnitScoreDisplayModel(unit.Id, unit.Name, GetGroupIdByUnitName(unit.Name));
                vm.SetComputedScores(s1, s2, s3, s4, totalViol, bonus, ranking: null);
                UnitScores.Add(vm);
            }

            // Hiển thị Ranking tạm (chưa chốt) theo từng Nhóm
            foreach (var grp in UnitScores.GroupBy(x => x.GroupId).OrderBy(g => g.Key))
            {
                var ordered = grp
                    .OrderByDescending(x => x.AverageScore ?? decimal.MinValue)
                    .ThenBy(x => x.OrgUnitName)
                    .ToList();

                int rank = 1;
                foreach (var item in ordered)
                    item.Ranking = rank++;
            }
        }

        private (decimal s1, decimal s2, decimal s3, decimal s4, int totalViolations, decimal bonusPoints)
            ComputeScoresForUnit(int year, int month, int week, int unitId)
        {
            var names = GetDescendantNames(unitId);
            var rows = _weeklyScoreData.GetByFilter(year, month, week, areaType: null, orgUnitNames: names);

            int v1Count = rows.Count(r => r.AreaType == 1 && !string.IsNullOrWhiteSpace(r.ViolationContent));
            int v2Count = rows.Count(r => r.AreaType == 2 && !string.IsNullOrWhiteSpace(r.ViolationContent));
            int v3Count = rows.Count(r => r.AreaType == 3 && !string.IsNullOrWhiteSpace(r.ViolationContent));
            int v4Count = rows.Count(r => r.AreaType == 4 && !string.IsNullOrWhiteSpace(r.ViolationContent));

            var v1Deduction = rows.Where(r => r.AreaType == 1).Sum(GetViolationPenalty);
            var v2Deduction = rows.Where(r => r.AreaType == 2).Sum(GetViolationPenalty);
            var v3Deduction = rows.Where(r => r.AreaType == 3).Sum(GetViolationPenalty);
            var v4Deduction = rows.Where(r => r.AreaType == 4).Sum(GetViolationPenalty);

            var commendationRows = rows.Where(r => !string.IsNullOrWhiteSpace(r.CommendationContent)).ToList();

            int totalViol = v1Count + v2Count + v3Count + v4Count;
            var bonus = commendationRows.Sum(GetCommendationBonus);

            var s1 = CalcScore(v1Deduction);
            var s2 = CalcScore(v2Deduction);
            var s3 = CalcScore(v3Deduction);
            var s4 = CalcScore(v4Deduction);

            return (s1, s2, s3, s4, totalViol, bonus);
        }

        private static decimal CalcScore(decimal violationDeduction)
        {
            var score = 10m - violationDeduction;
            if (score < 0m) score = 0m;
            return score;
        }

        private static decimal GetViolationPenalty(WeeklyScoreRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ViolationContent)) return 0m;

            switch (row.ViolationLevel)
            {
                case 1:
                    return 0.25m;
                case 2:
                    return 0.5m;
                case 3:
                    return 1m;
                default:
                    return 0m;
            }
        }

        private static decimal GetCommendationBonus(WeeklyScoreRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.CommendationContent)) return 0m;

            switch (row.ViolationLevel)
            {
                case 1:
                    return 0.25m;
                case 2:
                    return 0.5m;
                case 3:
                    return 1m;
                default:
                    // dữ liệu cũ chưa có cấp thì giữ mặc định như trước
                    return 0.5m;
            }
        }
        private static string GetViolationLevelText(int level)
        {
            switch (level)
            {
                case 1:
                    return "vi phạm thông thường";
                case 2:
                    return "vi phạm phải xử lý KL";
                case 3:
                    return "vi phạm nghiêm trọng";
                default:
                    return "";
            }
        }

        private static string BuildViolationDisplay(string violationContent, int violationLevel)
        {
            var content = (violationContent ?? "").Trim();
            if (string.IsNullOrWhiteSpace(content)) return "";

            var levelText = GetViolationLevelText(violationLevel);
            return string.IsNullOrWhiteSpace(levelText) ? content : $"{content} - {levelText}";
        }

        public void LoadUnitWeeklyScores(int year, int month, int week, int selectedOrgUnitId)
        {
            UnitWeeklyScores.Clear();

            var names = GetDescendantNames(selectedOrgUnitId);
            var rows = _weeklyScoreData.GetByFilter(year, month, week, areaType: null, orgUnitNames: names);

            int no = 1;
            foreach (var r in rows)
            {
                UnitWeeklyScores.Add(new WeeklyScoreDisplayModel
                {
                    No = no++,
                    Id = r.Id,
                    AreaType = r.AreaType,
                    FullName = r.FullName,
                    Rank = r.Rank,
                    Position = r.Position,
                    OrgUnit = r.OrgUnit,
                    ViolationContent = r.ViolationContent,
                    ViolationLevel = r.ViolationLevel,
                    ViolationContentDisplay = BuildViolationDisplay(r.ViolationContent, r.ViolationLevel),
                    CommendationContent = r.CommendationContent,
                    RecordDateTime = r.RecordDateTime,
                    EnteredBy = GetDisplayName(r.EnteredBy)
                });
            }
        }

        private void LoadUserDisplayNames()
        {
            _userDisplayNameMap.Clear();
            foreach (var user in _userData.GetAllDisplay())
            {
                if (string.IsNullOrWhiteSpace(user.Username)) continue;
                _userDisplayNameMap[user.Username] = string.IsNullOrWhiteSpace(user.DisplayName)
                    ? user.Username
                    : user.DisplayName;
            }
        }

        private string GetDisplayName(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return string.Empty;
            return _userDisplayNameMap.TryGetValue(username, out var display) ? display : username;
        }

        private static UserPermissionFlags LoadPermissionFlags(IConnectionStringProvider csProvider)
        {
            if (AppSession.CurrentUser == null) return new UserPermissionFlags();
            var permissionData = new UserPermissionData(csProvider);
            return permissionData.GetUserPermissionFlags(AppSession.CurrentUser.Id);
        }

        private bool CanEditViolation()
        {
            var targetWeek = GetViolationTargetWeek();
            if (targetWeek.HasValue && IsViolationWeekLocked(targetWeek.Value.year, targetWeek.Value.month, targetWeek.Value.week))
            {
                _message.Warning("Tuần này đã khóa, không thể thêm/sửa vi phạm hoặc biểu dương.");
                return false;
            }

            if (targetWeek.HasValue
                && IsViolationWeekFinalized(targetWeek.Value.year, targetWeek.Value.month, targetWeek.Value.week)
                && !_permissionFlags.CanLockWeek)
            {
                _message.Warning("Tuần này đã chốt, không thể thêm/sửa vi phạm hoặc biểu dương.");
                return false;
            }

            if (AppSession.IsAdmin)
                return true;

            if (!SelectedAreaTypeViolation.HasValue)
                return true;

            switch (SelectedAreaTypeViolation.Value)
            {
                case 1:
                    if (_permissionFlags.EditTM) return true;
                    _message.Warning("Bạn không có quyền sửa khu vực Công tác tham mưu.");
                    return false;
                case 2:
                    if (_permissionFlags.EditCT) return true;
                    _message.Warning("Bạn không có quyền sửa khu vực Công tác chính trị.");
                    return false;
                case 3:
                    if (_permissionFlags.EditHCKT) return true;
                    _message.Warning("Bạn không có quyền sửa khu vực Công tác HC-KT.");
                    return false;
                case 4:
                    if (_permissionFlags.EditNV) return true;
                    _message.Warning("Bạn không có quyền sửa khu vực Công tác nghiệp vụ.");
                    return false;
                default:
                    return true;
            }
        }

        private (int year, int month, int week)? GetViolationTargetWeek()
        {
            if (SelectedViolation != null)
            {
                if (SelectedViolation.Year > 0 && SelectedViolation.Month > 0 && SelectedViolation.Week > 0)
                    return (SelectedViolation.Year, SelectedViolation.Month, SelectedViolation.Week);

                if (SelectedViolation.RecordDateTime.HasValue)
                {
                    var assign = WeekCalculator.GetAssignment(SelectedViolation.RecordDateTime.Value);
                    return (assign.AssignedYear, assign.AssignedMonth, assign.WeekNoInAssignedMonth);
                }
            }

            if (SelectedYearViolation > 0 && SelectedMonthViolation > 0 && SelectedViolationWeek != null && SelectedViolationWeek.WeekNoInMonth > 0)
                return (SelectedYearViolation, SelectedMonthViolation, SelectedViolationWeek.WeekNoInMonth);

            if (RecordDate.HasValue)
            {
                var timeText = (RecordTimeText ?? "").Trim();
                if (string.IsNullOrWhiteSpace(timeText)) timeText = "00:00";
                if (!TimeSpan.TryParse(timeText, out var time))
                    time = TimeSpan.Zero;

                var assign = WeekCalculator.GetAssignment(RecordDate.Value.Date.Add(time));
                return (assign.AssignedYear, assign.AssignedMonth, assign.WeekNoInAssignedMonth);
            }

            if (SelectedYearViolation <= 0 || SelectedViolationWeek == null)
                return null;

            return (SelectedYearViolation, SelectedMonthViolation, SelectedViolationWeek.WeekNoInMonth);
        }

        private bool IsViolationWeekFinalized(int year, int month, int week)
        {
            if (year <= 0 || month <= 0 || week <= 0)
                return false;

            var savedRows = SafeGetSavedSummary(year, month, week);
            return savedRows != null
                   && savedRows.Count >= Units.Count
                   && savedRows.All(r => r.Ranking.HasValue);
        }

        private bool IsViolationWeekLocked(int year, int month, int week)
        {
            if (year <= 0 || month <= 0 || week <= 0)
                return false;

            return _weekLockData.IsLocked(year, month, week);
        }

        private List<string> GetDescendantNames(int rootId)
        {
            // Fix lỗi key null: chỉ group các node có ParentId != null
            var map = _allUnits
                .Where(x => x.ParentId.HasValue)
                .GroupBy(x => x.ParentId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultIds = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(rootId);

            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (resultIds.Contains(id)) continue;

                resultIds.Add(id);

                if (map.TryGetValue(id, out var children))
                {
                    foreach (var c in children)
                        stack.Push(c.Id);
                }
            }

            var unitById = _allUnits.ToDictionary(u => u.Id, u => u);

            return _allUnits
                .Where(u => resultIds.Contains(u.Id))
                .SelectMany(u =>
                {
                    var names = new List<string> { u.Name };
                    if (u.ParentId.HasValue
                        && unitById.TryGetValue(u.ParentId.Value, out var parent)
                        && !OrgUnitData.IsInternalOrgUnit(parent))
                    {
                        names.Add($"{u.Name} - {parent.Name}");
                    }

                    return names;
                })
                .Distinct()
                .ToList();
        }
        private void ResetUnitScoresSorting()
        {
            var view = CollectionViewSource.GetDefaultView(UnitScores);
            if (view == null) return;

            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();      // xoá sort user bấm trên DataGrid
                view.GroupDescriptions.Clear();     // phòng trường hợp có grouping
            }
        }

    }
}