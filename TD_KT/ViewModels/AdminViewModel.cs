using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TD_KT.Data;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class AdminViewModel : BaseViewModel
    {
        private readonly UserData _userData;
        private readonly SoldierData _soldierData;
        private readonly OrgUnitData _orgUnitData;
        private readonly PositionData _positionData;
        private readonly RankData _rankData;
        private readonly IMessageService _messageService;
        private readonly IHometownPickerService _hometownPicker;
        // ====== OPTIONS cho ComboBox (View đang binding) ======

        // NOTE: Bám đúng dữ liệu DB (dbo.Users.Role)
        // - Filter: có "Tất cả"
        // - Form nhập liệu: chỉ cho chọn 2 vai trò hợp lệ
        // NOTE: Vai trò hiển thị theo yêu cầu: dùng "User" thay vì "Người dùng".
        // DB không cần đổi schema. Nếu DB còn dữ liệu cũ (Role = N'Người dùng'), phần mềm sẽ tự map sang "User" để hiển thị.
        public ObservableCollection<string> RoleOptions { get; } =
            new ObservableCollection<string> { "Tất cả", "Admin", "User" };

        public ObservableCollection<string> RoleOptionsForm { get; } =
            new ObservableCollection<string> { "Admin", "User" };

        public ObservableCollection<string> SubjectGroupOptions { get; } =
            new ObservableCollection<string> { "Tất cả", "Sĩ quan", "QNCN" };

        public ObservableCollection<string> SubjectGroupOptionsForm { get; } =
            new ObservableCollection<string> { "Sĩ quan", "QNCN" };

        private string _selectedSubjectGroupFilter;
        public string SelectedSubjectGroupFilter
        {
            get => _selectedSubjectGroupFilter;
            set
            {
                if (SetProperty(ref _selectedSubjectGroupFilter, value))
                {
                    // nếu bạn đang dùng CollectionView để filter danh sách quân nhân
                    SoldiersView?.Refresh();
                }
            }
        }

        public PermissionViewModel Permissions { get; }
        public CatalogViewModel Catalog { get; }

        #region === TAB 1: TÀI KHOẢN ===

        private ObservableCollection<UserDisplayModel> _accounts;
        public ObservableCollection<UserDisplayModel> Accounts
        {
            get => _accounts;
            set => SetProperty(ref _accounts, value);
        }

        public ICollectionView AccountsView { get; private set; }

        private UserDisplayModel _selectedAccount;
        public UserDisplayModel SelectedAccount
        {
            get => _selectedAccount;
            set => SetProperty(ref _selectedAccount, value);
        }

        // Bộ lọc
        private string _roleFilter = "Tất cả";
        public string RoleFilter
        {
            get => _roleFilter;
            set { SetProperty(ref _roleFilter, value); AccountsView?.Refresh(); }
        }

        private string _accountSearchText;
        public string AccountSearchText
        {
            get => _accountSearchText;
            set { SetProperty(ref _accountSearchText, value); AccountsView?.Refresh(); }
        }

        #endregion

        #region === TAB 3: HỒ SƠ QUÂN NHÂN ===

        private ObservableCollection<SoldierDisplayModel> _soldiers;
        public ObservableCollection<SoldierDisplayModel> Soldiers
        {
            get => _soldiers;
            set => SetProperty(ref _soldiers, value);
        }

        public ICollectionView SoldiersView { get; private set; }

        private SoldierDisplayModel _selectedSoldier;
        public SoldierDisplayModel SelectedSoldier
        {
            get => _selectedSoldier;
            set
            {
                if (SetProperty(ref _selectedSoldier, value))
                {
                    FillSoldierFormFromSelected();
                }
            }
        }

        private string _soldierSearchText;
        public string SoldierSearchText
        {
            get => _soldierSearchText;
            set { SetProperty(ref _soldierSearchText, value); SoldiersView?.Refresh(); }
        }

        #endregion

        #region === DANH SÁCH DÙNG CHUNG ===

        public ObservableCollection<OrgUnit> OrgUnitsForFilter { get; private set; }
        public ObservableCollection<Position> PositionsForFilter { get; private set; }
        public ObservableCollection<Rank> RanksForFilter { get; private set; }

        public ObservableCollection<OrgUnit> UnitFilterOptions { get; private set; }
        public ObservableCollection<OrgUnit> SoldierUnitOptions { get; private set; }

        private OrgUnit _selectedGroupFilter;
        public OrgUnit SelectedGroupFilter
        {
            get => _selectedGroupFilter;
            set
            {
                if (SetProperty(ref _selectedGroupFilter, value))
                {
                    LoadUnitFilterOptions();
                    SoldiersView?.Refresh();
                }
            }
        }

        private OrgUnit _selectedUnitFilter;
        public OrgUnit SelectedUnitFilter
        {
            get => _selectedUnitFilter;
            set { SetProperty(ref _selectedUnitFilter, value); SoldiersView?.Refresh(); }
        }

        private Rank _selectedRankFilter;
        public Rank SelectedRankFilter
        {
            get => _selectedRankFilter;
            set { SetProperty(ref _selectedRankFilter, value); SoldiersView?.Refresh(); }
        }

        private Position _selectedPositionFilter;
        public Position SelectedPositionFilter
        {
            get => _selectedPositionFilter;
            set { SetProperty(ref _selectedPositionFilter, value); SoldiersView?.Refresh(); }
        }

        #endregion

        #region === SOLDIER FORM (TAB 3) ===

        private string _soldierFullName;
        public string SoldierFullName
        {
            get => _soldierFullName;
            set => SetProperty(ref _soldierFullName, value);
        }

        private string _soldierBirthYear;
        public string SoldierBirthYear
        {
            get => _soldierBirthYear;
            set => SetProperty(ref _soldierBirthYear, value);
        }

        private string _soldierHometown;
        public string SoldierHometown
        {
            get => _soldierHometown;
            set => SetProperty(ref _soldierHometown, value);
        }

        private string _soldierCitizenId;
        public string SoldierCitizenId
        {
            get => _soldierCitizenId;
            set => SetProperty(ref _soldierCitizenId, value);
        }


        private string _soldierSubjectGroup;
        public string SoldierSubjectGroup
        {
            get => _soldierSubjectGroup;
            set => SetProperty(ref _soldierSubjectGroup, value);
        }

        private Rank _soldierRank;
        public Rank SoldierRank
        {
            get => _soldierRank;
            set => SetProperty(ref _soldierRank, value);
        }

        private Position _soldierPosition;
        public Position SoldierPosition
        {
            get => _soldierPosition;
            set => SetProperty(ref _soldierPosition, value);
        }

        private OrgUnit _soldierGroup;
        public OrgUnit SoldierGroup
        {
            get => _soldierGroup;
            set
            {
                if (SetProperty(ref _soldierGroup, value))
                {
                    LoadSoldierUnitOptions();
                }
            }
        }

        private OrgUnit _soldierUnit;
        public OrgUnit SoldierUnit
        {
            get => _soldierUnit;
            set => SetProperty(ref _soldierUnit, value);
        }

        private DateTime? _soldierEnlistmentDate;
        public DateTime? SoldierEnlistmentDate
        {
            get => _soldierEnlistmentDate;
            set => SetProperty(ref _soldierEnlistmentDate, value);
        }

        #endregion

        #region === COMMANDS (TAB 3) ===

        public RelayCommand ReloadSoldiersCommand { get; }
        public RelayCommand AddSoldierCommand { get; }
        public RelayCommand EditSoldierCommand { get; }
        public RelayCommand DeleteSoldierCommand { get; }
        public RelayCommand PickSoldierHometownCommand { get; }

        #endregion

        #region === CONSTRUCTOR ===

        public AdminViewModel()
            : this(new ConnectionStringProvider("Database"), new DialogService(), new MessageService())
        {
        }

        public AdminViewModel(IConnectionStringProvider csProvider, IDialogService dialogService, IMessageService messageService)
        {
            _messageService = messageService;

            _userData = new UserData(csProvider);
            _soldierData = new SoldierData(csProvider);
            _orgUnitData = new OrgUnitData(csProvider);
            _positionData = new PositionData(csProvider);
            _rankData = new RankData(csProvider);
            _hometownPicker = new HometownPickerService();

            // Sub ViewModels
            Permissions = new PermissionViewModel(csProvider, messageService);
            Catalog = new CatalogViewModel(dialogService);

            // Khởi tạo collections
            Accounts = new ObservableCollection<UserDisplayModel>();
            Soldiers = new ObservableCollection<SoldierDisplayModel>();
            OrgUnitsForFilter = new ObservableCollection<OrgUnit>();
            PositionsForFilter = new ObservableCollection<Position>();
            RanksForFilter = new ObservableCollection<Rank>();
            UnitFilterOptions = new ObservableCollection<OrgUnit>();
            SoldierUnitOptions = new ObservableCollection<OrgUnit>();

            LoadFilterData();

            SelectedGroupFilter = OrgUnitsForFilter.FirstOrDefault();
            SelectedRankFilter = RanksForFilter.FirstOrDefault();
            SelectedPositionFilter = PositionsForFilter.FirstOrDefault();
            SelectedSubjectGroupFilter = SubjectGroupOptions.FirstOrDefault();

            LoadAccounts();
            LoadSoldiers();

            AccountsView = CollectionViewSource.GetDefaultView(Accounts);
            AccountsView.Filter = FilterAccounts;

            SoldiersView = CollectionViewSource.GetDefaultView(Soldiers);
            SoldiersView.Filter = FilterSoldiers;

            ReloadSoldiersCommand = new RelayCommand(_ => LoadSoldiers());
            AddSoldierCommand = new RelayCommand(_ => AddSoldierFromForm());
            EditSoldierCommand = new RelayCommand(_ => EditSoldierFromForm(), _ => SelectedSoldier != null);
            DeleteSoldierCommand = new RelayCommand(_ => DeleteSelectedSoldier(), _ => SelectedSoldier != null);
            PickSoldierHometownCommand = new RelayCommand(_ => PickSoldierHometown());
        }

        #endregion

        #region === LOAD DATA ===

        public void LoadAccounts()
        {
            try
            {
                Accounts.Clear();
                foreach (var u in _userData.GetAllDisplay())
                    Accounts.Add(u);

                AccountsView?.Refresh();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được danh sách tài khoản.\n" + ex.Message);
            }
        }

        public void LoadSoldiers()
        {
            try
            {
                Soldiers.Clear();
                foreach (var s in _soldierData.GetAllDisplay())
                    Soldiers.Add(s);

                SoldiersView?.Refresh();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được danh sách quân nhân.\n" + ex.Message);
            }
        }

        private void LoadFilterData()
        {
            try
            {
                OrgUnitsForFilter.Clear();
                OrgUnitsForFilter.Add(new OrgUnit { Id = 0, Name = "Tất cả" });
                foreach (var ou in _orgUnitData.GetAll().Where(o => !OrgUnitData.IsInternalOrgUnit(o)))
                    OrgUnitsForFilter.Add(ou);

                PositionsForFilter.Clear();
                PositionsForFilter.Add(new Position { Id = 0, Name = "Tất cả" });
                foreach (var p in _positionData.GetAll())
                    PositionsForFilter.Add(p);

                RanksForFilter.Clear();
                RanksForFilter.Add(new Rank { Id = 0, Name = "Tất cả" });
                foreach (var r in _rankData.GetAll())
                    RanksForFilter.Add(r);

                LoadUnitFilterOptions();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được dữ liệu danh mục lọc.\n" + ex.Message);
            }
        }

        private void LoadUnitFilterOptions()
        {
            UnitFilterOptions.Clear();
            UnitFilterOptions.Add(new OrgUnit { Id = 0, Name = "Tất cả" });

            if (SelectedGroupFilter != null && SelectedGroupFilter.Id != 0)
            {
                var children = OrgUnitsForFilter
                    .Where(o => o.ParentId == SelectedGroupFilter.Id)
                    .ToList();

                foreach (var item in children)
                    UnitFilterOptions.Add(item);
            }
            else
            {
                foreach (var item in OrgUnitsForFilter.Where(o => o.Id != 0))
                    UnitFilterOptions.Add(item);
            }

            if (SelectedUnitFilter == null || UnitFilterOptions.All(o => o.Id != SelectedUnitFilter.Id))
                SelectedUnitFilter = UnitFilterOptions.FirstOrDefault();
        }

        private void LoadSoldierUnitOptions()
        {
            SoldierUnitOptions.Clear();

            if (SoldierGroup == null || SoldierGroup.Id == 0)
                return;

            var children = OrgUnitsForFilter
                .Where(o => o.ParentId == SoldierGroup.Id)
                .ToList();

            foreach (var item in children)
                SoldierUnitOptions.Add(item);

            if (SoldierUnit != null && SoldierUnitOptions.All(u => u.Id != SoldierUnit.Id))
                SoldierUnit = null;
        }

        #endregion

        #region === FILTER ===

        private bool FilterAccounts(object obj)
        {
            if (obj is UserDisplayModel user)
            {
                if (RoleFilter != "Tất cả" && user.Role != RoleFilter)
                    return false;

                if (!string.IsNullOrWhiteSpace(AccountSearchText))
                {
                    var search = AccountSearchText.ToLower();
                    if (!user.Username.ToLower().Contains(search) &&
                        !(user.DisplayName ?? "").ToLower().Contains(search))
                        return false;
                }

                return true;
            }
            return false;
        }

        private bool FilterSoldiers(object obj)
        {
            if (obj is SoldierDisplayModel soldier)
            {
                if (SelectedRankFilter != null && SelectedRankFilter.Id != 0)
                {
                    if (soldier.RankId != SelectedRankFilter.Id)
                        return false;
                }

                if (SelectedPositionFilter != null && SelectedPositionFilter.Id != 0)
                {
                    if (soldier.PositionId != SelectedPositionFilter.Id)
                        return false;
                }

                if (SelectedUnitFilter != null && SelectedUnitFilter.Id != 0)
                {
                    if (soldier.OrgUnitId != SelectedUnitFilter.Id)
                        return false;
                }
                else if (SelectedGroupFilter != null && SelectedGroupFilter.Id != 0)
                {
                    if (soldier.OrgUnitId != SelectedGroupFilter.Id && soldier.OrgUnitParentId != SelectedGroupFilter.Id)
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(SelectedSubjectGroupFilter) && SelectedSubjectGroupFilter != "Tất cả")
                {
                    if (!string.Equals(soldier.SubjectGroup, SelectedSubjectGroupFilter, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(SoldierSearchText))
                {
                    var search = SoldierSearchText.ToLower();
                    if (!soldier.FullName.ToLower().Contains(search))
                        return false;
                }

                return true;
            }
            return false;
        }

        #endregion

        #region === SOLDIER CRUD (MVVM - TAB 3) ===

        private void FillSoldierFormFromSelected()
        {
            if (SelectedSoldier == null)
                return;

            SoldierFullName = SelectedSoldier.FullName;
            SoldierBirthYear = SelectedSoldier.BirthYear?.ToString() ?? string.Empty;
            SoldierHometown = SelectedSoldier.Hometown ?? string.Empty;
            SoldierCitizenId = SelectedSoldier.CitizenId ?? string.Empty;
            SoldierSubjectGroup = SelectedSoldier.SubjectGroup;

            SoldierRank = RanksForFilter.FirstOrDefault(r => r.Id == (SelectedSoldier.RankId ?? 0));
            SoldierPosition = PositionsForFilter.FirstOrDefault(p => p.Id == (SelectedSoldier.PositionId ?? 0));

            // Nhóm = parent của đơn vị hoặc chính đơn vị nếu không có parent
            var unit = OrgUnitsForFilter.FirstOrDefault(o => o.Id == (SelectedSoldier.OrgUnitId ?? 0));
            if (unit != null)
            {
                var group = unit.ParentId.HasValue
                    ? OrgUnitsForFilter.FirstOrDefault(o => o.Id == unit.ParentId.Value)
                    : unit;

                SoldierGroup = group;
                SoldierUnit = unit.ParentId.HasValue ? unit : null;
            }
            else
            {
                SoldierGroup = null;
                SoldierUnit = null;
            }

            SoldierEnlistmentDate = SelectedSoldier.EnlistmentDate;
        }

        private int? ParseBirthYear()
        {
            if (int.TryParse(SoldierBirthYear, out var year))
                return year;

            return null;
        }

        private void PickSoldierHometown()
        {
            var picked = _hometownPicker.PickHometown(Application.Current?.MainWindow);
            if (!string.IsNullOrWhiteSpace(picked))
                SoldierHometown = picked;
        }

        private void AddSoldierFromForm()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SoldierFullName))
                {
                    _messageService.Warning("Vui lòng nhập họ tên!");
                    return;
                }

                var orgUnitId = (SoldierUnit != null ? (int?)SoldierUnit.Id : (SoldierGroup != null && SoldierGroup.Id != 0 ? (int?)SoldierGroup.Id : null));

                _soldierData.Insert(
                    SoldierFullName.Trim(),
                    SoldierSubjectGroup ?? string.Empty,
                    SoldierRank?.Id == 0 ? (int?)null : SoldierRank?.Id,
                    SoldierPosition?.Id == 0 ? (int?)null : SoldierPosition?.Id,
                    orgUnitId,
                    ParseBirthYear(),
                    SoldierEnlistmentDate,
                    SoldierHometown ?? string.Empty,
                    SoldierCitizenId ?? string.Empty
                );

                LoadSoldiers();
                _messageService.Info("Thêm quân nhân thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thêm được quân nhân.\n" + ex.Message);
            }
        }

        private void EditSoldierFromForm()
        {
            try
            {
                if (SelectedSoldier == null)
                {
                    _messageService.Warning("Vui lòng chọn quân nhân cần sửa!");
                    return;
                }

                var orgUnitId = (SoldierUnit != null ? (int?)SoldierUnit.Id : (SoldierGroup != null && SoldierGroup.Id != 0 ? (int?)SoldierGroup.Id : null));

                _soldierData.Update(
                    SelectedSoldier.Id,
                    SoldierFullName?.Trim() ?? string.Empty,
                    SoldierSubjectGroup ?? string.Empty,
                    SoldierRank?.Id == 0 ? (int?)null : SoldierRank?.Id,
                    SoldierPosition?.Id == 0 ? (int?)null : SoldierPosition?.Id,
                    orgUnitId,
                    ParseBirthYear(),
                    SoldierEnlistmentDate,
                    SoldierHometown ?? string.Empty,
                    SoldierCitizenId ?? string.Empty
                );

                LoadSoldiers();
                _messageService.Info("Cập nhật quân nhân thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không cập nhật được quân nhân.\n" + ex.Message);
            }
        }

        private void DeleteSelectedSoldier()
        {
            try
            {
                if (SelectedSoldier == null)
                {
                    _messageService.Warning("Vui lòng chọn quân nhân cần xóa!");
                    return;
                }

                if (!_messageService.Confirm($"Bạn có chắc muốn xóa '{SelectedSoldier.FullName}'?"))
                    return;

                _soldierData.Delete(SelectedSoldier.Id);
                LoadSoldiers();
                _messageService.Info("Đã xóa quân nhân!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không xóa được quân nhân.\n" + ex.Message);
            }
        }

        #endregion

        #region === ACCOUNT CRUD ===

        public void AddAccount(string username, string password, string displayName, int orgUnitId, int positionId, string role, bool isActive)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    _messageService.Warning("Vui lòng nhập Username!");
                    return;
                }


                if (string.IsNullOrWhiteSpace(password))
                {
                    _messageService.Warning("Vui lòng nhập Mật khẩu!");
                    return;
                }

                // View không có ô DisplayName => nếu rỗng thì dùng username
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = username;

                var hash = PasswordHasherService.HashPassword(password);

                _userData.Insert(
                    username.Trim(),
                    hash.HashBase64,
                    hash.SaltBase64,
                    hash.Algorithm,
                    hash.Iterations,
                    displayName.Trim(),
                    orgUnitId,
                    positionId,
                    role ?? "User",
                    isActive
                );
                LoadAccounts();
                _messageService.Info("Thêm tài khoản thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thêm được tài khoản.\n" + ex.Message);
            }
        }

        public void EditAccount(int id, string username, string password, string displayName, int orgUnitId, int positionId, string role, bool isActive)
        {
            try
            {
                if (id <= 0)
                {
                    _messageService.Warning("Tài khoản không hợp lệ!");
                    return;
                }

                // Khi sửa: password có thể để trống (giữ nguyên mật khẩu hiện tại)
                if (string.IsNullOrWhiteSpace(username))
                {
                    _messageService.Warning("Vui lòng nhập Username!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = username;

                string passwordHash = null;
                string passwordSalt = null;
                string passwordAlgo = null;
                int passwordIterations = 0;

                if (!string.IsNullOrWhiteSpace(password))
                {
                    var hash = PasswordHasherService.HashPassword(password);
                    passwordHash = hash.HashBase64;
                    passwordSalt = hash.SaltBase64;
                    passwordAlgo = hash.Algorithm;
                    passwordIterations = hash.Iterations;
                }

                _userData.Update(
                    id,
                    username.Trim(),
                    passwordHash,
                    passwordSalt,
                    passwordAlgo,
                    passwordIterations,
                    displayName.Trim(),
                    orgUnitId,
                    positionId,
                    role ?? "User",
                    isActive
                );
                LoadAccounts();
                _messageService.Info("Cập nhật tài khoản thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không cập nhật được tài khoản.\n" + ex.Message);
            }
        }

        public void DeleteAccount(int id, string usernameForMessage)
        {
            try
            {
                if (id <= 0)
                {
                    _messageService.Warning("Tài khoản không hợp lệ!");
                    return;
                }

                if (!_messageService.Confirm($"Bạn có chắc muốn xóa tài khoản '{usernameForMessage}'?"))
                    return;

                _userData.Delete(id);
                LoadAccounts();
                _messageService.Info("Đã xóa tài khoản!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không xóa được tài khoản.\n" + ex.Message);
            }
        }

        #endregion
    }
}
