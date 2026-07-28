using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TD_KT.Data;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class OrgUnitViewModel : BaseViewModel
    {
        private readonly OrgUnitData _orgUnitData;
        private readonly IMessageService _messageService;

        private ObservableCollection<OrgUnitDisplayModel> _orgUnits;
        public ObservableCollection<OrgUnitDisplayModel> OrgUnits
        {
            get => _orgUnits;
            set => SetProperty(ref _orgUnits, value);
        }

        public ICollectionView OrgUnitsView { get; private set; }

        private OrgUnitDisplayModel _selectedOrgUnit;
        public OrgUnitDisplayModel SelectedOrgUnit
        {
            get => _selectedOrgUnit;
            set
            {
                if (SetProperty(ref _selectedOrgUnit, value))
                {
                    FillEditForm();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); OrgUnitsView?.Refresh(); }
        }

        public ObservableCollection<OrgUnit> ParentOptions { get; private set; }

        private OrgUnit _selectedParent;
        public OrgUnit SelectedParent
        {
            get => _selectedParent;
            set => SetProperty(ref _selectedParent, value);
        }

        private string _editSymbol;
        public string EditSymbol
        {
            get => _editSymbol;
            set => SetProperty(ref _editSymbol, value);
        }

        private string _editName;
        public string EditName
        {
            get => _editName;
            set => SetProperty(ref _editName, value);
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public OrgUnitViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService())
        {
        }

        public OrgUnitViewModel(IConnectionStringProvider csProvider, IMessageService messageService)
        {
            _orgUnitData = new OrgUnitData(csProvider);
            _messageService = messageService;

            OrgUnits = new ObservableCollection<OrgUnitDisplayModel>();
            ParentOptions = new ObservableCollection<OrgUnit>();

            OrgUnitsView = CollectionViewSource.GetDefaultView(OrgUnits);
            OrgUnitsView.Filter = Filter;

            RefreshCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedOrgUnit != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedOrgUnit != null);

            LoadData();
        }

        private bool Filter(object obj)
        {
            if (obj is OrgUnitDisplayModel m)
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    return true;

                var s = SearchText.ToLower();
                return (m.Name ?? "").ToLower().Contains(s) || (m.Symbol ?? "").ToLower().Contains(s);
            }
            return false;
        }

        private void LoadData()
        {
            try
            {
                OrgUnits.Clear();
                foreach (var item in _orgUnitData.GetAllDisplay().Where(o => !OrgUnitData.IsInternalOrgUnit(o)))
                    OrgUnits.Add(item);

                ParentOptions.Clear();
                ParentOptions.Add(new OrgUnit { Id = 0, Name = "Không có" });
                foreach (var ou in _orgUnitData.GetAll().Where(o => !OrgUnitData.IsInternalOrgUnit(o)))
                    ParentOptions.Add(ou);

                SelectedParent = ParentOptions.FirstOrDefault();
                OrgUnitsView?.Refresh();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được danh sách cơ quan/đơn vị.\n" + ex.Message);
            }
        }

        private void FillEditForm()
        {
            if (SelectedOrgUnit == null)
                return;

            EditSymbol = SelectedOrgUnit.Symbol;
            EditName = SelectedOrgUnit.Name;

            if (SelectedOrgUnit.ParentId.HasValue)
                SelectedParent = ParentOptions.FirstOrDefault(p => p.Id == SelectedOrgUnit.ParentId.Value) ?? ParentOptions.FirstOrDefault();
            else
                SelectedParent = ParentOptions.FirstOrDefault();
        }

        private void Add()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditName))
                {
                    _messageService.Warning("Vui lòng nhập tên!");
                    return;
                }

                var parentId = (SelectedParent != null && SelectedParent.Id != 0) ? (int?)SelectedParent.Id : null;
                _orgUnitData.Insert(EditSymbol ?? string.Empty, EditName.Trim(), parentId, string.Empty);

                LoadData();
                _messageService.Info("Thêm cơ quan/đơn vị thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thêm được cơ quan/đơn vị.\n" + ex.Message);
            }
        }

        private void Edit()
        {
            try
            {
                if (SelectedOrgUnit == null)
                {
                    _messageService.Warning("Vui lòng chọn dòng cần sửa!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditName))
                {
                    _messageService.Warning("Vui lòng nhập tên!");
                    return;
                }

                var parentId = (SelectedParent != null && SelectedParent.Id != 0) ? (int?)SelectedParent.Id : null;
                _orgUnitData.Update(SelectedOrgUnit.Id, EditSymbol ?? string.Empty, EditName.Trim(), parentId, string.Empty);

                LoadData();
                _messageService.Info("Cập nhật cơ quan/đơn vị thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không cập nhật được cơ quan/đơn vị.\n" + ex.Message);
            }
        }

        private void Delete()
        {
            try
            {
                if (SelectedOrgUnit == null)
                {
                    _messageService.Warning("Vui lòng chọn dòng cần xóa!");
                    return;
                }

                if (!_messageService.Confirm($"Bạn có chắc muốn xóa '{SelectedOrgUnit.Name}'?"))
                    return;

                _orgUnitData.Delete(SelectedOrgUnit.Id);
                LoadData();
                _messageService.Info("Đã xóa cơ quan/đơn vị!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không xóa được cơ quan/đơn vị.\n" + ex.Message);
            }
        }
    }

    public class OrgUnitDisplayModel
    {
        public int Stt { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public int? ParentId { get; set; }
        public string ParentName { get; set; }
    }
}
