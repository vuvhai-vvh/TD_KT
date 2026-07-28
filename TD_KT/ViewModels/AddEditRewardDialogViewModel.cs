using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TD_KT.Data;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class AddEditRewardDialogViewModel : BaseViewModel
    {
        private readonly IConnectionStringProvider _cs;
        private readonly IMessageService _msg;
        private readonly IHometownPickerService _hometownPicker;

        private bool _isCollectiveReward;
        private string _title;

        private ObservableCollection<SoldierDisplayModel> _soldiers;
        private ObservableCollection<RewardForm> _rewardForms;
        private ObservableCollection<OrgUnit> _orgUnits;

        private SoldierDisplayModel _selectedSoldier;
        private RewardForm _selectedRewardForm;
        private OrgUnit _selectedCollectiveOrgUnit;

        private string _fullName;
        private string _rank;
        private string _birthYear;
        private string _positionUnit;
        private DateTime? _enlistmentDate;
        private string _hometown;
        private string _rewardFormText;
        private string _circumstance;
        private string _note;

        private bool _suppressSync;
        private Window _ownerWindow;

        public event Action<bool?> RequestClose;

        public AddEditRewardDialogViewModel(DecisionDetailDisplay editItem = null)
        {
            _cs = new ConnectionStringProvider("Database");
            _msg = new MessageService();
            _hometownPicker = new HometownPickerService();

            Title = editItem == null ? "THÊM NỘI DUNG KHEN THƯỞNG" : "SỬA NỘI DUNG KHEN THƯỞNG";

            LoadLookupData();

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
            PickHometownCommand = new RelayCommand(_ => PickHometown(), _ => !IsCollectiveReward);

            if (editItem != null)
                LoadFromExisting(editItem);
        }

        public void SetOwner(Window owner)
        {
            _ownerWindow = owner;
        }

        private void LoadLookupData()
        {
            try
            {
                var soldierData = new SoldierData(_cs);
                Soldiers = new ObservableCollection<SoldierDisplayModel>(soldierData.GetAllDisplay());

                var rewardData = new RewardFormData(_cs);
                RewardForms = new ObservableCollection<RewardForm>(rewardData.GetAll());

                var orgData = new OrgUnitData(_cs);
                OrgUnits = new ObservableCollection<OrgUnit>(orgData.GetAll().Where(o => !OrgUnitData.IsInternalOrgUnit(o)));
            }
            catch (Exception ex)
            {
                _msg.Error($"Lỗi tải danh sách dữ liệu: {ex.Message}");
            }
        }

        public void LoadFromExisting(DecisionDetailDisplay item)
        {
            if (item == null) return;

            var isCollective = string.Equals((item.HoTen ?? "").Trim(), "(Tập thể)", StringComparison.OrdinalIgnoreCase);
            IsCollectiveReward = isCollective;

            if (isCollective)
            {
                SelectedCollectiveOrgUnit = OrgUnits?.FirstOrDefault(x => string.Equals(x.Name, item.ChucVuDonVi ?? "", StringComparison.OrdinalIgnoreCase))
                                           ?? OrgUnits?.FirstOrDefault(x => (item.ChucVuDonVi ?? "").Contains(x.Name));

                RewardFormText = item.HinhThucKT ?? "";
                SelectedRewardForm = RewardForms?.FirstOrDefault(x => string.Equals(x.Name, RewardFormText, StringComparison.OrdinalIgnoreCase));

                Note = item.GhiChu ?? "";
                // các trường khác không dùng trong tập thể
            }
            else
            {
                FullName = item.HoTen ?? "";
                Rank = item.CapBac ?? "";
                BirthYear = item.NamSinh?.ToString() ?? "";
                PositionUnit = item.ChucVuDonVi ?? "";
                Hometown = item.QueQuan ?? "";
                RewardFormText = item.HinhThucKT ?? "";
                SelectedRewardForm = RewardForms?.FirstOrDefault(x => string.Equals(x.Name, RewardFormText, StringComparison.OrdinalIgnoreCase));

                Circumstance = item.HoanCanh ?? "";
                Note = item.GhiChu ?? "";

                if (DateTime.TryParseExact(item.NhapNgu ?? "", "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    EnlistmentDate = d;

                // cố gắng match soldier để autofill nếu có
                var match = (item.SoldierId.HasValue ? Soldiers?.FirstOrDefault(s => s.Id == item.SoldierId.Value) : null)
                           ?? Soldiers?.FirstOrDefault(s => string.Equals(s.FullName, FullName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    SelectedSoldier = match;
            }
        }

        private void PickHometown()
        {
            var picked = _hometownPicker.PickHometown(_ownerWindow);
            if (!string.IsNullOrWhiteSpace(picked))
                Hometown = picked;
        }

        private void Save()
        {
            if (IsCollectiveReward)
            {
                if (SelectedCollectiveOrgUnit == null)
                {
                    _msg.Warning("Vui lòng chọn Cơ quan/Đơn vị!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(RewardFormText))
                {
                    _msg.Warning("Vui lòng chọn hình thức khen thưởng!");
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    _msg.Warning("Vui lòng chọn hoặc nhập họ tên!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(RewardFormText))
                {
                    _msg.Warning("Vui lòng chọn hình thức khen thưởng!");
                    return;
                }
            }

            RequestClose?.Invoke(true);
        }

        private void ApplySoldierToFields(SoldierDisplayModel soldier)
        {
            if (soldier == null) return;

            _suppressSync = true;
            FullName = soldier.FullName ?? "";
            Rank = soldier.Rank ?? "";
            BirthYear = soldier.BirthYear.HasValue ? soldier.BirthYear.Value.ToString() : BirthYear;
            PositionUnit = $"{soldier.Position ?? ""} - {soldier.OrgUnit ?? ""}".Trim();
            EnlistmentDate = soldier.EnlistmentDate;
            if (!string.IsNullOrWhiteSpace(soldier.Hometown))
                Hometown = soldier.Hometown;
            _suppressSync = false;
        }

        private void ApplyCollectiveDefaults()
        {
            // set các trường tập thể theo yêu cầu: chỉ dùng Cơ quan/Đơn vị + Hình thức KT + Ghi chú
            _suppressSync = true;
            SelectedSoldier = null;
            FullName = "(Tập thể)";
            Rank = "";
            BirthYear = "";
            PositionUnit = "";
            EnlistmentDate = null;
            Hometown = "";
            Circumstance = "";
            _suppressSync = false;
        }

        // =================== Binding properties ===================
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsCollectiveReward
        {
            get => _isCollectiveReward;
            set
            {
                if (!SetProperty(ref _isCollectiveReward, value)) return;

                if (value)
                    ApplyCollectiveDefaults();
                else
                {
                    if (string.Equals(FullName, "(Tập thể)", StringComparison.OrdinalIgnoreCase))
                        FullName = "";
                }

                // update CanExecute for PickHometown
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<SoldierDisplayModel> Soldiers
        {
            get => _soldiers;
            private set => SetProperty(ref _soldiers, value);
        }

        public ObservableCollection<RewardForm> RewardForms
        {
            get => _rewardForms;
            private set => SetProperty(ref _rewardForms, value);
        }

        public ObservableCollection<OrgUnit> OrgUnits
        {
            get => _orgUnits;
            private set => SetProperty(ref _orgUnits, value);
        }

        public SoldierDisplayModel SelectedSoldier
        {
            get => _selectedSoldier;
            set
            {
                if (!SetProperty(ref _selectedSoldier, value)) return;
                if (IsCollectiveReward) return;
                if (value != null) ApplySoldierToFields(value);
            }
        }

        public RewardForm SelectedRewardForm
        {
            get => _selectedRewardForm;
            set
            {
                if (!SetProperty(ref _selectedRewardForm, value)) return;
                if (value != null)
                {
                    _suppressSync = true;
                    RewardFormText = value.Name ?? "";
                    _suppressSync = false;
                }
            }
        }

        public OrgUnit SelectedCollectiveOrgUnit
        {
            get => _selectedCollectiveOrgUnit;
            set => SetProperty(ref _selectedCollectiveOrgUnit, value);
        }

        public string FullName
        {
            get => _fullName;
            set
            {
                if (!SetProperty(ref _fullName, value)) return;
                if (_suppressSync) return;
                if (IsCollectiveReward) return;

                // nếu user gõ khác soldier -> bỏ selected
                if (SelectedSoldier != null && !string.Equals(SelectedSoldier.FullName, value ?? "", StringComparison.OrdinalIgnoreCase))
                    SelectedSoldier = null;
            }
        }

        public string Rank
        {
            get => _rank;
            set => SetProperty(ref _rank, value);
        }

        public string BirthYear
        {
            get => _birthYear;
            set => SetProperty(ref _birthYear, value);
        }

        public string PositionUnit
        {
            get => _positionUnit;
            set => SetProperty(ref _positionUnit, value);
        }

        public DateTime? EnlistmentDate
        {
            get => _enlistmentDate;
            set => SetProperty(ref _enlistmentDate, value);
        }

        public string Hometown
        {
            get => _hometown;
            set => SetProperty(ref _hometown, value);
        }

        public string RewardFormText
        {
            get => _rewardFormText;
            set
            {
                if (!SetProperty(ref _rewardFormText, value)) return;
                if (_suppressSync) return;

                if (!string.IsNullOrWhiteSpace(value) && RewardForms != null)
                {
                    var match = RewardForms.FirstOrDefault(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));
                    if (match != null && match != SelectedRewardForm)
                        SelectedRewardForm = match;
                }
            }
        }

        public string Circumstance
        {
            get => _circumstance;
            set => SetProperty(ref _circumstance, value);
        }

        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        // =================== Commands ===================
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand PickHometownCommand { get; }

        // =================== Output mapping for DecisionView ===================
        public string OutputHoTen => IsCollectiveReward ? "(Tập thể)" : (FullName ?? "");
        public string OutputCapBac => IsCollectiveReward ? "" : (Rank ?? "");
        public string OutputNamSinh => IsCollectiveReward ? "" : (BirthYear ?? "");

        public string OutputChucVuDonVi
        {
            get
            {
                if (IsCollectiveReward)
                    return SelectedCollectiveOrgUnit?.Name ?? "";
                return PositionUnit ?? "";
            }
        }

        public string OutputNhapNgu => IsCollectiveReward ? "" : (EnlistmentDate?.ToString("dd/MM/yyyy") ?? "");
        public string OutputQueQuan => IsCollectiveReward ? "" : (Hometown ?? "");
        public string OutputHinhThucKT => RewardFormText ?? "";
        public string OutputHoanCanh => IsCollectiveReward ? "" : (Circumstance ?? "");
        public string OutputGhiChu => Note ?? "";

        // Id phục vụ JOIN đồng bộ
        public int? OutputSoldierId => IsCollectiveReward ? (int?)null : SelectedSoldier?.Id;
        public int? OutputRewardFormId => SelectedRewardForm?.Id;
    }
}
