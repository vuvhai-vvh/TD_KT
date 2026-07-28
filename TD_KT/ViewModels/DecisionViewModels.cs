using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using TD_KT.Data;
using TD_KT.Services;
using TD_KT.Views;

namespace TD_KT.ViewModels
{
    public class DecisionViewModel : BaseViewModel
    {
        private readonly DecisionData _decisionData;
        private readonly DecisionAttachmentData _attachmentData;
        private readonly IssuingLevelData _issuingLevelData;
        private readonly IMessageService _messageService;
        private readonly IFileStorageService _attachmentFileStorage;

        private bool _isLoadingDecisionList;
        private bool _suppressDecisionNumberFilter;

        public ObservableCollection<DecisionHeaderRow> Decisions { get; } = new ObservableCollection<DecisionHeaderRow>();
        public ObservableCollection<DecisionDetailDisplay> DecisionDetails { get; } = new ObservableCollection<DecisionDetailDisplay>();
        public ObservableCollection<DecisionAttachmentDisplay> DecisionAttachments { get; } = new ObservableCollection<DecisionAttachmentDisplay>();
        public ObservableCollection<IssuingLevelDisplayModel> IssuingLevels { get; } = new ObservableCollection<IssuingLevelDisplayModel>();
        public ObservableCollection<DecisionFilterOption> YearOptions { get; } = new ObservableCollection<DecisionFilterOption>();
        public ObservableCollection<DecisionFilterOption> MonthOptions { get; } = new ObservableCollection<DecisionFilterOption>();

        private DecisionFilterOption _selectedYearOption;
        public DecisionFilterOption SelectedYearOption
        {
            get => _selectedYearOption;
            set
            {
                if (SetProperty(ref _selectedYearOption, value))
                {
                    if (!_isLoadingDecisionList)
                        ReloadDecisionList(DecisionNumberText, keepCurrentSelection: false);
                }
            }
        }

        private DecisionFilterOption _selectedMonthOption;
        public DecisionFilterOption SelectedMonthOption
        {
            get => _selectedMonthOption;
            set
            {
                if (SetProperty(ref _selectedMonthOption, value))
                {
                    if (!_isLoadingDecisionList)
                        ReloadDecisionList(DecisionNumberText, keepCurrentSelection: false);
                }
            }
        }

        private DecisionHeaderRow _selectedDecision;
        public DecisionHeaderRow SelectedDecision
        {
            get => _selectedDecision;
            set
            {
                if (SetProperty(ref _selectedDecision, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                    if (_isLoadingDecisionList) return;
                    LoadDecisionToForm(value);
                }
            }
        }

        private DecisionDetailDisplay _selectedDetail;
        public DecisionDetailDisplay SelectedDetail
        {
            get => _selectedDetail;
            set
            {
                if (SetProperty(ref _selectedDetail, value))
                    LoadAttachments();
            }
        }

        private DecisionAttachmentDisplay _selectedAttachment;
        public DecisionAttachmentDisplay SelectedAttachment
        {
            get => _selectedAttachment;
            set => SetProperty(ref _selectedAttachment, value);
        }

        private string _decisionNumberText;
        public string DecisionNumberText
        {
            get => _decisionNumberText;
            set
            {
                if (SetProperty(ref _decisionNumberText, value))
                {
                    if (_suppressDecisionNumberFilter || _isLoadingDecisionList) return;

                    // ComboBox editable sẽ đẩy Text khi user chọn item, thường là DisplayText (vd: "123 (ID:10)").
                    // Đây không phải thao tác lọc nên tránh reload để không tự clear SelectedDecision.
                    if (SelectedDecision != null)
                    {
                        var selectedNumber = SelectedDecision.DecisionNumber?.Trim() ?? string.Empty;
                        var selectedDisplay = SelectedDecision.DisplayText?.Trim() ?? string.Empty;
                        var incoming = value?.Trim() ?? string.Empty;
                        if (string.Equals(incoming, selectedNumber, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(incoming, selectedDisplay, StringComparison.OrdinalIgnoreCase))
                            return;
                    }

                    ReloadDecisionList(NormalizeKeyword(value), keepCurrentSelection: true, autoSelectFirstIfNone: false);
                }
            }
        }

        private IssuingLevelDisplayModel _selectedIssuingLevel;
        public IssuingLevelDisplayModel SelectedIssuingLevel
        {
            get => _selectedIssuingLevel;
            set => SetProperty(ref _selectedIssuingLevel, value);
        }

        private string _signer;
        public string Signer
        {
            get => _signer;
            set => SetProperty(ref _signer, value);
        }

        private DateTime? _signedDate;
        public DateTime? SignedDate
        {
            get => _signedDate;
            set => SetProperty(ref _signedDate, value);
        }

        private string _note;
        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        public RelayCommand AddDecisionCommand { get; }
        public RelayCommand EditDecisionCommand { get; }
        public RelayCommand RefreshDecisionStateCommand { get; }

        public RelayCommand AddDetailCommand { get; }
        public RelayCommand EditDetailCommand { get; }
        public RelayCommand DeleteDetailCommand { get; }

        public RelayCommand AddAttachmentCommand { get; }
        public RelayCommand DeleteAttachmentCommand { get; }
        public RelayCommand OpenAttachmentCommand { get; }
        public RelayCommand DownloadAttachmentCommand { get; }

        public DecisionViewModel()
        {
            _decisionData = new DecisionData("Database");
            _attachmentData = new DecisionAttachmentData("Database");
            _issuingLevelData = new IssuingLevelData(new ConnectionStringProvider("Database"));
            _messageService = new MessageService();
            _attachmentFileStorage = new FileStorageService("DecisionAttachments");

            AddDecisionCommand = new RelayCommand(_ => OpenDecisionDialog(isEditMode: false));
            EditDecisionCommand = new RelayCommand(_ => OpenDecisionDialog(isEditMode: true), _ => SelectedDecision != null);
            RefreshDecisionStateCommand = new RelayCommand(_ => ResetState());
            AddDetailCommand = new RelayCommand(_ => AddDetail());
            EditDetailCommand = new RelayCommand(_ => EditDetail());
            DeleteDetailCommand = new RelayCommand(_ => DeleteDetail());
            AddAttachmentCommand = new RelayCommand(_ => AddAttachment());
            DeleteAttachmentCommand = new RelayCommand(_ => DeleteAttachment());
            OpenAttachmentCommand = new RelayCommand(_ => OpenAttachment());
            DownloadAttachmentCommand = new RelayCommand(_ => DownloadAttachment());

            // Chuyển các file quyết định cũ vào FileData khi file vật lý vẫn còn trên máy này.
            // Việc chuyển thất bại không chặn phần mềm vì file có thể đang nằm trên máy khác.
            MigrateLegacyAttachmentsToDatabase();

            LoadIssuingLevels();
            LoadYearMonthFilters();
            ReloadDecisionList();
        }

        private void ResetState()
        {
            LoadYearMonthFilters();

            _suppressDecisionNumberFilter = true;
            DecisionNumberText = string.Empty;
            _suppressDecisionNumberFilter = false;

            ReloadDecisionList(keyword: null, keepCurrentSelection: false, autoSelectFirstIfNone: true);
        }

        private void LoadYearMonthFilters()
        {
            YearOptions.Clear();
            YearOptions.Add(new DecisionFilterOption { Value = null, Name = "Tất cả" });
            foreach (var y in _decisionData.GetDistinctYears())
                YearOptions.Add(new DecisionFilterOption { Value = y, Name = y.ToString() });

            MonthOptions.Clear();
            MonthOptions.Add(new DecisionFilterOption { Value = null, Name = "Tất cả" });
            for (int m = 1; m <= 12; m++)
                MonthOptions.Add(new DecisionFilterOption { Value = m, Name = m.ToString() });

            SelectedYearOption = YearOptions.FirstOrDefault();
            SelectedMonthOption = MonthOptions.FirstOrDefault();
        }

        private (int? year, int? month) GetSelectedYearMonth()
        {
            return (SelectedYearOption?.Value, SelectedMonthOption?.Value);
        }

        private static string NormalizeKeyword(string keyword)
        {
            var text = keyword?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Khi chọn trong ComboBox, text có thể ở dạng "<DecisionNumber> (ID:<n>)".
            var markerIndex = text.IndexOf("(ID:", StringComparison.OrdinalIgnoreCase);
            if (markerIndex > 0)
                text = text.Substring(0, markerIndex).Trim();

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private void ReloadDecisionList(string keyword = null, bool keepCurrentSelection = true, bool autoSelectFirstIfNone = true)
        {
            DecisionHeaderRow pendingDecisionToLoad = null;
            try
            {
                _isLoadingDecisionList = true;

                var (year, month) = GetSelectedYearMonth();
                var list = _decisionData.GetAllFiltered(year, month, string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim());
                var currentId = keepCurrentSelection ? SelectedDecision?.Id : (int?)null;

                Decisions.Clear();
                foreach (var row in list)
                    Decisions.Add(row);

                if (currentId.HasValue)
                {
                    var still = Decisions.FirstOrDefault(x => x.Id == currentId.Value);
                    if (still != null)
                    {
                        SelectedDecision = still;
                        pendingDecisionToLoad = still;
                        return;
                    }
                }

                if (Decisions.Count > 0)
                {
                    if (autoSelectFirstIfNone)
                    {
                        SelectedDecision = Decisions[0];
                        pendingDecisionToLoad = Decisions[0];
                    }
                    else
                    {
                        // Nếu đang lọc theo text và item cũ không còn trong danh sách,
                        // phải clear SelectedDecision để tránh SelectedItem trỏ tới object
                        // không tồn tại trong ItemsSource (dễ gây lỗi Index out of range trên ComboBox).
                        SelectedDecision = null;
                    }
                }
                else
                {
                    if (autoSelectFirstIfNone)
                        BeginNewDecision();
                    else
                        SelectedDecision = null;
                }
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi tải danh sách quyết định: {ex.Message}");
            }
            finally
            {
                _isLoadingDecisionList = false;
                if (pendingDecisionToLoad != null && SelectedDecision == pendingDecisionToLoad)
                    LoadDecisionToForm(pendingDecisionToLoad);
            }
        }

        private void LoadDecisionToForm(DecisionHeaderRow row)
        {

            _suppressDecisionNumberFilter = true;
            DecisionNumberText = row?.DecisionNumber ?? string.Empty;
            _suppressDecisionNumberFilter = false;

            SelectedIssuingLevel = row == null
                ? null
                : IssuingLevels.FirstOrDefault(x => x.Id == row.IssuingLevelId);
            Signer = row?.Signer ?? string.Empty;
            SignedDate = row?.SignedDate;
            Note = row?.Note ?? string.Empty;

            LoadDecisionDetails();
            LoadAttachments();
        }

        private void BeginNewDecision()
        {
            SelectedDecision = null;

            _suppressDecisionNumberFilter = true;
            DecisionNumberText = string.Empty;
            _suppressDecisionNumberFilter = false;

            SelectedIssuingLevel = null;
            Signer = string.Empty;
            SignedDate = null;
            Note = string.Empty;

            DecisionDetails.Clear();
            DecisionAttachments.Clear();
            SelectedDetail = null;
            SelectedAttachment = null;
        }

        private void OpenDecisionDialog(bool isEditMode)
        {
            if (isEditMode && SelectedDecision == null)
            {
                _messageService.Warning("Vui lòng chọn quyết định cần sửa!");
                return;
            }

            var dialogDecision = isEditMode ? SelectedDecision : null;
            var dialog = new DecisionEditorDialog(IssuingLevels, dialogDecision, DecisionNumberText);
            if (dialog.ShowDialog() != true)
                return;

            if (dialog.DeleteRequested)
            {
                DeleteDecision(dialog.DecisionId);
                return;
            }

            SaveDecision(dialog);
        }

        private void SaveDecision(DecisionEditorDialog dialog)
        {
            if (string.IsNullOrWhiteSpace(dialog.DecisionNumber))
            {
                _messageService.Warning("Vui lòng nhập số quyết định!");
                return;
            }

            if (dialog.SelectedIssuingLevel == null)
            {
                _messageService.Warning("Vui lòng chọn cấp ban hành!");
                return;
            }

            try
            {
                var isInsert = !dialog.IsEditMode || SelectedDecision == null;
                if (isInsert)
                {
                    var row = new DecisionHeaderRow
                    {
                        DecisionNumber = dialog.DecisionNumber.Trim(),
                        DecisionContent = string.Empty,
                        IssuingLevelId = dialog.SelectedIssuingLevel.Id,
                        Note = dialog.Note,
                        Signer = dialog.Signer,
                        SignedDate = dialog.SignedDate,
                        TotalRows = 0
                    };

                    var newId = _decisionData.Insert(row);

                    ReloadDecisionList(keyword: null, keepCurrentSelection: false);
                    var selected = Decisions.FirstOrDefault(x => x.Id == newId);
                    SelectedDecision = selected;

                    _messageService.Info("Thêm quyết định mới thành công!");
                }
                else
                {
                    var total = DecisionDetails.Count;
                    var row = new DecisionHeaderRow
                    {
                        Id = dialog.DecisionId,
                        DecisionNumber = dialog.DecisionNumber.Trim(),
                        DecisionContent = string.Empty,
                        IssuingLevelId = dialog.SelectedIssuingLevel.Id,
                        Note = dialog.Note,
                        Signer = dialog.Signer,
                        SignedDate = dialog.SignedDate,
                        TotalRows = total
                    };

                    _decisionData.Update(row);
                    ReloadDecisionList(keyword: null, keepCurrentSelection: true);
                    SelectedDecision = Decisions.FirstOrDefault(x => x.Id == row.Id);

                    _messageService.Info("Cập nhật quyết định thành công!");
                }
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi lưu quyết định: {ex.Message}");
            }
        }

        private void DeleteDecision(int decisionId)
        {
            if (decisionId <= 0)
            {
                _messageService.Warning("Vui lòng chọn quyết định cần xóa!");
                return;
            }

            if (!_messageService.Confirm(
                "Bạn có chắc chắn muốn xóa quyết định này?\n\nLưu ý: Tất cả danh sách khen thưởng và file đính kèm trong quyết định cũng sẽ bị xóa."))
                return;

            try
            {
                _decisionData.Delete(decisionId);
                BeginNewDecision();
                ReloadDecisionList(keyword: null, keepCurrentSelection: false);
                _messageService.Info("Xóa quyết định thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi xóa quyết định: {ex.Message}");
            }
        }

        private void LoadDecisionDetails()
        {
            try
            {
                DecisionDetails.Clear();
                if (SelectedDecision == null) return;

                foreach (var detail in _decisionData.GetDetails(SelectedDecision.Id))
                    DecisionDetails.Add(detail);

                SelectedDetail = DecisionDetails.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi tải danh sách khen thưởng: {ex.Message}");
            }
        }

        private void LoadAttachments()
        {
            try
            {
                DecisionAttachments.Clear();
                SelectedAttachment = null;
                if (SelectedDecision == null || SelectedDetail == null) return;

                foreach (var item in _attachmentData.GetByDecision(SelectedDecision.Id, SelectedDetail.Id))
                    DecisionAttachments.Add(item);
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi tải file đính kèm: {ex.Message}");
            }
        }

        private void AddDetail()
        {
            if (SelectedDecision == null)
            {
                _messageService.Warning("Vui lòng chọn hoặc lưu quyết định trước!");
                return;
            }

            var dialog = new AddEditRewardDialog();
            if (dialog.ShowDialog() != true) return;

            try
            {
                var nextStt = DecisionDetails.Count == 0 ? 1 : DecisionDetails.Max(x => x.STT) + 1;

                var d = new DecisionDetailDisplay
                {
                    DecisionId = SelectedDecision.Id,
                    STT = nextStt,
                    SoldierId = dialog.SoldierId,
                    HoTen = dialog.HoTen,
                    CapBac = dialog.CapBac,
                    NamSinh = int.TryParse(dialog.NamSinh, out var y) ? (int?)y : null,
                    ChucVuDonVi = dialog.ChucVuDonVi,
                    NhapNgu = dialog.NhapNgu,
                    QueQuan = dialog.QueQuan,
                    RewardFormId = dialog.RewardFormId,
                    HinhThucKT = dialog.HinhThucKT,
                    HoanCanh = dialog.HoanCanh,
                    GhiChu = dialog.GhiChu,
                };

                _decisionData.InsertDetail(SelectedDecision.Id, d);
                _decisionData.UpdateTotalRows(SelectedDecision.Id);
                LoadDecisionDetails();
                SelectedDecision.TotalRows = DecisionDetails.Count;
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi thêm dòng: {ex.Message}");
            }
        }

        private void EditDetail()
        {
            if (SelectedDecision == null)
            {
                _messageService.Warning("Vui lòng chọn quyết định trước!");
                return;
            }

            if (SelectedDetail == null)
            {
                _messageService.Warning("Vui lòng chọn dòng cần sửa!");
                return;
            }

            var dialog = new AddEditRewardDialog(SelectedDetail);
            if (dialog.ShowDialog() != true) return;

            try
            {
                SelectedDetail.HoTen = dialog.HoTen;
                SelectedDetail.CapBac = dialog.CapBac;
                SelectedDetail.NamSinh = int.TryParse(dialog.NamSinh, out var y) ? (int?)y : null;
                SelectedDetail.ChucVuDonVi = dialog.ChucVuDonVi;
                SelectedDetail.NhapNgu = dialog.NhapNgu;
                SelectedDetail.QueQuan = dialog.QueQuan;
                SelectedDetail.SoldierId = dialog.SoldierId;
                SelectedDetail.RewardFormId = dialog.RewardFormId;
                SelectedDetail.HinhThucKT = dialog.HinhThucKT;
                SelectedDetail.HoanCanh = dialog.HoanCanh;
                SelectedDetail.GhiChu = dialog.GhiChu;

                _decisionData.UpdateDetail(SelectedDetail);
                LoadDecisionDetails();
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi sửa dòng: {ex.Message}");
            }
        }

        private void DeleteDetail()
        {
            if (SelectedDecision == null)
            {
                _messageService.Warning("Vui lòng chọn quyết định trước!");
                return;
            }

            if (SelectedDetail == null)
            {
                _messageService.Warning("Vui lòng chọn dòng cần xóa!");
                return;
            }

            if (!_messageService.Confirm("Bạn có chắc chắn muốn xóa dòng này?"))
                return;

            try
            {
                _decisionData.DeleteDetail(SelectedDetail.Id);
                _decisionData.UpdateTotalRows(SelectedDecision.Id);
                LoadDecisionDetails();
                SelectedDecision.TotalRows = DecisionDetails.Count;
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi xóa dòng: {ex.Message}");
            }
        }

        private void AddAttachment()
        {
            if (SelectedDecision == null)
            {
                _messageService.Warning("Vui lòng chọn hoặc lưu quyết định trước!");
                return;
            }

            if (SelectedDetail == null)
            {
                _messageService.Warning("Vui lòng chọn cá nhân/tập thể để thêm file đính kèm!");
                return;
            }

            var ofd = new OpenFileDialog
            {
                Title = "Chọn file đính kèm",
                Filter = "Tất cả file|*.*"
            };

            if (ofd.ShowDialog() != true) return;

            try
            {
                var info = new FileInfo(ofd.FileName);
                var attachment = new DecisionAttachmentDisplay
                {
                    DecisionId = SelectedDecision.Id,
                    DecisionDetailId = SelectedDetail.Id,
                    FileName = info.Name,
                    FileType = info.Extension.TrimStart('.').ToLowerInvariant(),
                    FileSize = info.Length,
                    FilePath = string.Empty,
                    FileData = File.ReadAllBytes(ofd.FileName),
                    HasFileData = true,
                    UploadedBy = AppSession.CurrentUser != null
                        ? AppSession.CurrentUser.Username ?? string.Empty
                        : string.Empty
                };

                _attachmentData.Insert(attachment);
                LoadAttachments();
                _messageService.Info("Đã lưu file đính kèm vào cơ sở dữ liệu.");
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi thêm file đính kèm: {ex.Message}");
            }
        }

        private void DeleteAttachment()
        {
            if (SelectedAttachment == null)
            {
                _messageService.Warning("Vui lòng chọn file cần xóa!");
                return;
            }

            if (!_messageService.Confirm("Bạn có chắc muốn xóa file đính kèm này?"))
                return;

            try
            {
                _attachmentData.Delete(SelectedAttachment.Id);
                LoadAttachments();
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi xóa file: {ex.Message}");
            }
        }

        private void OpenAttachment()
        {
            if (SelectedAttachment == null)
            {
                _messageService.Warning("Vui lòng chọn file cần mở!");
                return;
            }

            try
            {
                var fileData = _attachmentData.GetFileData(SelectedAttachment.Id);
                if (fileData != null && fileData.Length > 0)
                {
                    _attachmentFileStorage.OpenBytesWithDefaultApp(fileData, SelectedAttachment.FileName);
                    return;
                }

                // Tương thích với file cũ chưa chuyển vào CSDL.
                _attachmentFileStorage.OpenWithDefaultApp(SelectedAttachment.FilePath);
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi mở file: {ex.Message}");
            }
        }

        private void DownloadAttachment()
        {
            if (SelectedAttachment == null)
            {
                _messageService.Warning("Vui lòng chọn file cần tải xuống!");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Lưu file",
                FileName = SelectedAttachment.FileName,
                Filter = "Tất cả file|*.*"
            };

            if (sfd.ShowDialog() != true) return;

            try
            {
                var fileData = _attachmentData.GetFileData(SelectedAttachment.Id);
                if (fileData != null && fileData.Length > 0)
                    _attachmentFileStorage.SaveBytesTo(fileData, sfd.FileName);
                else
                    _attachmentFileStorage.CopyTo(SelectedAttachment.FilePath, sfd.FileName);

                _messageService.Info("Tải xuống thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi tải xuống: {ex.Message}");
            }
        }

        private void LoadIssuingLevels()
        {
            try
            {
                IssuingLevels.Clear();
                foreach (var level in _issuingLevelData.GetAllDisplay())
                    IssuingLevels.Add(level);
            }
            catch (Exception ex)
            {
                _messageService.Error($"Lỗi tải cấp ban hành: {ex.Message}");
            }
        }

        private void MigrateLegacyAttachmentsToDatabase()
        {
            try
            {
                foreach (var legacy in _attachmentData.GetLegacyFilesWithoutData())
                {
                    var fullPath = _attachmentFileStorage.ResolvePath(legacy.FilePath);
                    if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
                        continue;

                    _attachmentData.StoreFileData(legacy.Id, File.ReadAllBytes(fullPath));
                }
            }
            catch
            {
                // Không chặn chương trình nếu file cũ đang nằm trên máy khác.
                // Bản ghi cũ vẫn được mở theo FilePath khi file vật lý còn tồn tại.
            }
        }
    }

    public class DecisionFilterOption
    {
        public int? Value { get; set; }
        public string Name { get; set; }
    }
}
