using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using TD_KT.Data;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class DocumentProposalViewModel : BaseViewModel
    {
        private readonly DocumentProposalData _data;
        private readonly IMessageService _message;
        private readonly IFileDialogService _fileDialog;
        private readonly IFileStorageService _fileStorage;
        private readonly UserData _userData;
        private readonly IssuingLevelData _issuingLevelData;
        private readonly Dictionary<string, string> _userDisplayNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private bool _suppressAutoRefresh;

        // Option hiển thị tất cả trong bộ lọc (chỉ dùng cho Filter, KHÔNG dùng cho form nhập).
        private const string AllIssuingLevelOption = "Tất cả";
        private const int AllMonthsOptionValue = 0;

        public ObservableCollection<DocumentProposalDisplayModel> Documents { get; }
        public ObservableCollection<int> Years { get; }
        public ObservableCollection<KeyValuePair<int, string>> Months { get; }

        /// <summary>
        /// Danh sách cho bộ lọc "Cấp ban hành" (có thêm option "Tất cả").
        /// </summary>
        public ObservableCollection<string> ProposingUnitOptions { get; }

        /// <summary>
        /// Danh sách cho form nhập/sửa (KHÔNG có "Tất cả").
        /// </summary>
        public ObservableCollection<string> ProposingUnitFormOptions { get; }

        private DocumentProposalDisplayModel _selectedDocument;
        public DocumentProposalDisplayModel SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                if (SetProperty(ref _selectedDocument, value))
                {
                    if (!_isFormEnabled)
                    {
                        LoadSelectedToForm(value);
                    }
                }
            }
        }

        // ===== Filter =====
        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    if (!_suppressAutoRefresh)
                        Refresh();
                }
            }
        }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (SetProperty(ref _selectedMonth, value))
                {
                    if (!_suppressAutoRefresh)
                        Refresh();
                }
            }
        }

        private string _selectedProposingUnitFilter;
        public string SelectedProposingUnitFilter
        {
            get => _selectedProposingUnitFilter;
            set
            {
                if (SetProperty(ref _selectedProposingUnitFilter, value))
                {
                    if (!_suppressAutoRefresh)
                        Refresh();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        // ===== Form state =====
        private bool _isFormEnabled;
        public bool IsFormEnabled
        {
            get => _isFormEnabled;
            set => SetProperty(ref _isFormEnabled, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _proposingUnit;
        public string ProposingUnit
        {
            get => _proposingUnit;
            set => SetProperty(ref _proposingUnit, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private string _fileType;
        public string FileType
        {
            get => _fileType;
            set => SetProperty(ref _fileType, value);
        }

        private string _fileSize;
        public string FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        private string _uploadDateText;
        public string UploadDateText
        {
            get => _uploadDateText;
            set => SetProperty(ref _uploadDateText, value);
        }

        private string _uploadedBy;
        public string UploadedBy
        {
            get => _uploadedBy;
            set => SetProperty(ref _uploadedBy, value);
        }

        private string _storedFilePath;
        public string StoredFilePath
        {
            get => _storedFilePath;
            set => SetProperty(ref _storedFilePath, value);
        }

        private string _localPickedFilePath;

        // ===== Commands =====
        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ChooseFileCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand DownloadFileCommand { get; }

        public DocumentProposalViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService(), new FileDialogService(), new FileStorageService())
        {
        }

        public DocumentProposalViewModel(IConnectionStringProvider csProvider, IMessageService messageService, IFileDialogService fileDialog, IFileStorageService fileStorage)
        {
            _data = new DocumentProposalData(csProvider ?? new ConnectionStringProvider("Database"));
            _message = messageService ?? new MessageService();
            _fileDialog = fileDialog ?? new FileDialogService();
            _fileStorage = fileStorage ?? new FileStorageService();
            _userData = new UserData(csProvider ?? new ConnectionStringProvider("Database"));
            _issuingLevelData = new IssuingLevelData(csProvider ?? new ConnectionStringProvider("Database"));

            Documents = new ObservableCollection<DocumentProposalDisplayModel>();
            Years = new ObservableCollection<int>();
            Months = new ObservableCollection<KeyValuePair<int, string>>();
            ProposingUnitOptions = new ObservableCollection<string>();
            ProposingUnitFormOptions = new ObservableCollection<string>();

            RefreshCommand = new RelayCommand(_ => Refresh());
            AddCommand = new RelayCommand(_ => BeginAdd());
            EditCommand = new RelayCommand(_ => BeginEdit());
            DeleteCommand = new RelayCommand(_ => DeleteSelected());
            SaveCommand = new RelayCommand(_ => Save(), _ => IsFormEnabled);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => IsFormEnabled);
            ChooseFileCommand = new RelayCommand(_ => ChooseFile(), _ => IsFormEnabled);
            OpenFileCommand = new RelayCommand(_ => OpenFile(), _ => SelectedDocument != null && (SelectedDocument.HasFileData || !string.IsNullOrWhiteSpace(SelectedDocument.FilePath)));
            DownloadFileCommand = new RelayCommand(_ => DownloadFile(), _ => SelectedDocument != null && (SelectedDocument.HasFileData || !string.IsNullOrWhiteSpace(SelectedDocument.FilePath)));

            InitLookups();
            LoadUserDisplayNames();
            MigrateLegacyFilesToDatabase();
            Refresh();
            IsFormEnabled = false;
        }

        private void InitLookups()
        {
            Years.Clear();
            var nowYear = DateTime.Now.Year;
            var yearSet = new HashSet<int> { 2023, 2024, 2025, nowYear - 1, nowYear, nowYear + 1 };

            // Nếu DB đã có năm thì ưu tiên thêm vào
            try
            {
                foreach (var y in _data.GetYears())
                {
                    yearSet.Add(y);
                }
            }
            catch
            {
                // ignore
            }

            foreach (var year in yearSet.OrderBy(y => y))
                Years.Add(year);

            // months filter: thêm "Tất cả" + 1-12
            Months.Clear();
            Months.Add(new KeyValuePair<int, string>(AllMonthsOptionValue, "Tất cả"));
            for (int m = 1; m <= 12; m++)
                Months.Add(new KeyValuePair<int, string>(m, m.ToString(CultureInfo.InvariantCulture)));

            _suppressAutoRefresh = true;
            SelectedYear = nowYear;
            SelectedMonth = AllMonthsOptionValue;
            LoadProposingUnitOptions();
            _suppressAutoRefresh = false;
        }

        private void LoadProposingUnitOptions()
        {
            ProposingUnitOptions.Clear();

            // Filter có thêm "Tất cả"
            ProposingUnitOptions.Add(AllIssuingLevelOption);
            foreach (var u in LoadIssuingLevelNames())
                ProposingUnitOptions.Add(u);

            // Form nhập/sửa: chỉ 4 mục cố định
            ProposingUnitFormOptions.Clear();
            foreach (var u in LoadIssuingLevelNames())
                ProposingUnitFormOptions.Add(u);

            // Mặc định filter = "Tất cả" để hiển thị toàn bộ
            if (string.IsNullOrWhiteSpace(SelectedProposingUnitFilter))
                SelectedProposingUnitFilter = AllIssuingLevelOption;
        }

        private List<string> LoadIssuingLevelNames()
        {
            try
            {
                return _issuingLevelData.GetAllDisplay()
                    .Select(x => x.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private string GetDefaultIssuingLevelName()
        {
            return ProposingUnitFormOptions.FirstOrDefault() ?? string.Empty;
        }


        public void Refresh()
        {
            try
            {
                Documents.Clear();
                var unitFilter = SelectedProposingUnitFilter;
                if (string.Equals(unitFilter, AllIssuingLevelOption, StringComparison.OrdinalIgnoreCase))
                    unitFilter = null; // null => DocumentProposalData sẽ bỏ qua điều kiện ProposingUnit

                int? monthFilter = SelectedMonth == AllMonthsOptionValue ? (int?)null : SelectedMonth;
                var list = _data.GetAll(SelectedYear, monthFilter, unitFilter, SearchText);

                int stt = 1;
                foreach (var d in list)
                {
                    Documents.Add(new DocumentProposalDisplayModel
                    {
                        Stt = stt++,
                        Id = d.Id,
                        Title = d.Title,
                        ProposingUnit = d.ProposingUnit,
                        FileName = d.FileName,
                        FileType = d.FileType,
                        FileSize = d.FileSize,
                        FilePath = d.FilePath,
                        HasFileData = d.HasFileData,
                        UploadDate = d.UploadDate,
                        UploadedBy = GetDisplayName(d.UploadedBy),
                        Year = d.Year,
                        Month = d.Month
                    });
                }
                if (SelectedDocument == null && Documents.Count > 0)
                    SelectedDocument = Documents[0];
            }
            catch (Exception ex)
            {
                _message.Error("Lỗi tải danh sách văn bản: " + ex.Message);
            }
        }

        private void BeginAdd()
        {
            IsEditMode = false;
            IsFormEnabled = true;
            _localPickedFilePath = null;

            Title = "";
            // Nếu filter đang là "Tất cả" thì không gán vào form, tránh lưu sai dữ liệu.
            if (!string.IsNullOrWhiteSpace(SelectedProposingUnitFilter)
                && !string.Equals(SelectedProposingUnitFilter, AllIssuingLevelOption, StringComparison.OrdinalIgnoreCase))
            {
                ProposingUnit = SelectedProposingUnitFilter;
            }
            else
            {
                ProposingUnit = GetDefaultIssuingLevelName();
            }
            FileName = "";
            FileType = "";
            FileSize = "";
            StoredFilePath = "";
            UploadDateText = "";
            UploadedBy = GetDisplayName(AppSession.CurrentUser?.Username);

            SelectedDocument = null;
        }

        private void BeginEdit()
        {
            if (SelectedDocument == null)
            {
                _message.Warning("Vui lòng chọn văn bản cần sửa.");
                return;
            }

            IsEditMode = true;
            IsFormEnabled = true;
            _localPickedFilePath = null;

            LoadSelectedToForm(SelectedDocument);
        }

        private void LoadSelectedToForm(DocumentProposalDisplayModel d)
        {
            if (d == null)
            {
                Title = "";
                if (!string.IsNullOrWhiteSpace(SelectedProposingUnitFilter)
                    && !string.Equals(SelectedProposingUnitFilter, AllIssuingLevelOption, StringComparison.OrdinalIgnoreCase))
                {
                    ProposingUnit = SelectedProposingUnitFilter;
                }
                else
                {
                    ProposingUnit = GetDefaultIssuingLevelName();
                }
                FileName = "";
                FileType = "";
                FileSize = "";
                StoredFilePath = "";
                UploadDateText = "";
                UploadedBy = "";
                return;
            }

            Title = d.Title;
            ProposingUnit = d.ProposingUnit;
            FileName = d.FileName;
            FileType = d.FileType;
            FileSize = d.FileSize;
            StoredFilePath = d.FilePath;
            UploadDateText = d.UploadDateText;
            UploadedBy = GetDisplayName(d.UploadedBy);
        }

        private void Cancel()
        {
            IsFormEnabled = false;
            IsEditMode = false;
            _localPickedFilePath = null;

            // Trả form về item đang chọn để xem
            LoadSelectedToForm(SelectedDocument);
        }

        private void MigrateLegacyFilesToDatabase()
        {
            try
            {
                foreach (var legacy in _data.GetLegacyFilesWithoutData())
                {
                    var fullPath = _fileStorage.ResolvePath(legacy.FilePath);
                    if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
                        continue;

                    _data.StoreFileData(legacy.Id, File.ReadAllBytes(fullPath));
                }
            }
            catch
            {
                // Không chặn chương trình nếu file cũ đang nằm trên máy khác.
                // Hồ sơ cũ vẫn tiếp tục được mở theo FilePath khi file còn tồn tại.
            }
        }

        private void ChooseFile()
        {
            try
            {
                if (_fileDialog.TryPickFile(out var path) && !string.IsNullOrWhiteSpace(path))
                {
                    _localPickedFilePath = path;
                    FileName = Path.GetFileName(path);
                    FileType = Path.GetExtension(path)?.Trim('.').ToLowerInvariant();
                    FileSize = FormatFileSize(new FileInfo(path).Length);
                }
            }
            catch (Exception ex)
            {
                _message.Error("Lỗi chọn file: " + ex.Message);
            }
        }

        private void Save()
        {
            if (!IsFormEnabled) return;

            // validate
            if (string.IsNullOrWhiteSpace(Title))
            {
                _message.Warning("Vui lòng nhập 'Tên văn bản'.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ProposingUnit))
            {
                _message.Warning("Vui lòng nhập/chọn 'Cấp ban hành'.");
                return;
            }
            if (!IsEditMode && string.IsNullOrWhiteSpace(_localPickedFilePath))
            {
                _message.Warning("Vui lòng chọn file để tải lên.");
                return;
            }

            try
            {
                if (!IsEditMode)
                {
                    // insert
                    var fileData = File.ReadAllBytes(_localPickedFilePath);

                    var item = new DocumentProposal
                    {
                        Title = Title.Trim(),
                        ProposingUnit = ProposingUnit.Trim(),
                        FileName = FileName ?? "",
                        FileType = FileType,
                        FileSize = FileSize,
                        FilePath = "",
                        FileData = fileData,
                        UploadDate = DateTime.Now,
                        UploadedBy = AppSession.CurrentUser != null ? (AppSession.CurrentUser.Username ?? "") : null,
                        Year = SelectedYear,
                        Month = GetSelectedMonthForSave()
                    };

                    _data.Insert(item);

                    IsFormEnabled = false;
                    Refresh();
                    _message.Info("Đã tải lên văn bản.");
                }
                else
                {
                    // update
                    var id = SelectedDocument.Id;

                    var filePath = StoredFilePath;
                    byte[] fileData = null;
                    var fileName = FileName;
                    var fileType = FileType;
                    var fileSize = FileSize;

                    if (!string.IsNullOrWhiteSpace(_localPickedFilePath))
                    {
                        fileData = File.ReadAllBytes(_localPickedFilePath);
                        filePath = "";
                        fileName = Path.GetFileName(_localPickedFilePath);
                        fileType = Path.GetExtension(_localPickedFilePath)?.Trim('.').ToLowerInvariant();
                        fileSize = FormatFileSize(new FileInfo(_localPickedFilePath).Length);
                    }

                    var item = new DocumentProposal
                    {
                        Id = id,
                        Title = Title.Trim(),
                        ProposingUnit = ProposingUnit.Trim(),
                        FileName = fileName ?? "",
                        FileType = fileType,
                        FileSize = fileSize,
                        FilePath = filePath ?? "",
                        FileData = fileData,
                        UploadedBy = AppSession.CurrentUser != null ? (AppSession.CurrentUser.Username ?? "") : null,
                        Year = SelectedYear,
                        Month = GetSelectedMonthForSave()
                    };

                _data.Update(item);

                    IsFormEnabled = false;
                    IsEditMode = false;
                    Refresh();
                    _message.Info("Đã lưu thay đổi.");
                }
            }
            catch (Exception ex)
            {
                _message.Error("Lỗi lưu văn bản: " + ex.Message);
            }
        }

        private int GetSelectedMonthForSave()
        {
            return SelectedMonth == AllMonthsOptionValue ? DateTime.Now.Month : SelectedMonth;
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

        private void DeleteSelected()
        {
            if (SelectedDocument == null)
            {
                _message.Warning("Vui lòng chọn văn bản cần xóa.");
                return;
            }

            if (!_message.Confirm("Bạn có chắc muốn xóa văn bản đang chọn?"))
                return;

            try
            {
                _data.Delete(SelectedDocument.Id);
                Refresh();
                _message.Info("Đã xóa văn bản.");
            }
            catch (Exception ex)
            {
                _message.Error("Lỗi xóa văn bản: " + ex.Message);
            }
        }

        private void OpenFile()
        {
            if (SelectedDocument == null) return;

            try
            {
                var fileData = _data.GetFileData(SelectedDocument.Id);
                if (fileData != null && fileData.Length > 0)
                {
                    _fileStorage.OpenBytesWithDefaultApp(fileData, SelectedDocument.FileName);
                    return;
                }

                // Tương thích với hồ sơ cũ chưa chuyển vào CSDL.
                _fileStorage.OpenWithDefaultApp(SelectedDocument.FilePath);
            }
            catch (Exception ex)
            {
                _message.Error("Không mở được file: " + ex.Message);
            }
        }

        private void DownloadFile()
        {
            if (SelectedDocument == null) return;

            try
            {
                if (_fileDialog.TryPickSavePath(SelectedDocument.FileName, out var savePath) && !string.IsNullOrWhiteSpace(savePath))
                {
                    var fileData = _data.GetFileData(SelectedDocument.Id);
                    if (fileData != null && fileData.Length > 0)
                        _fileStorage.SaveBytesTo(fileData, savePath);
                    else
                        _fileStorage.CopyTo(SelectedDocument.FilePath, savePath);

                    _message.Info("Đã tải file về máy.");
                }
            }
            catch (Exception ex)
            {
                _message.Error("Lỗi tải file: " + ex.Message);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int idx = 0;

            while (size >= 1024 && idx < units.Length - 1)
            {
                size /= 1024;
                idx++;
            }

            return idx == 0
                ? size.ToString(CultureInfo.InvariantCulture) + " " + units[idx]
                : size.ToString("0.##", CultureInfo.InvariantCulture) + " " + units[idx];
        }
    }
}
