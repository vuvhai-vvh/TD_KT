using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TD_KT.Data;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    /// <summary>
    /// ViewModel quản lý danh mục: Cấp ban hành (IssuingLevels).
    /// </summary>
    public class IssuingLevelViewModel : BaseViewModel
    {
        private readonly IssuingLevelData _data;
        private readonly IMessageService _messageService;

        private ObservableCollection<IssuingLevelDisplayModel> _items;
        public ObservableCollection<IssuingLevelDisplayModel> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        public ICollectionView ItemsView { get; private set; }

        private IssuingLevelDisplayModel _selectedItem;
        public IssuingLevelDisplayModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    EditName = _selectedItem?.Name;
                }
            }
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

        public IssuingLevelViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService())
        {
        }

        public IssuingLevelViewModel(IConnectionStringProvider csProvider, IMessageService messageService)
        {
            _data = new IssuingLevelData(csProvider);
            _messageService = messageService;

            Items = new ObservableCollection<IssuingLevelDisplayModel>();
            ItemsView = CollectionViewSource.GetDefaultView(Items);
            ItemsView.Filter = _ => true;

            RefreshCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedItem != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedItem != null);

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Items.Clear();
                foreach (var i in _data.GetAllDisplay(onlyActive: false))
                    Items.Add(i);
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được danh mục Cấp ban hành." + ex.Message);
            }
        }

        private void Add()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditName))
                {
                    _messageService.Warning("Vui lòng nhập tên cấp ban hành!");
                    return;
                }

                _data.Insert("", EditName.Trim());
                LoadData();
                _messageService.Info("Thêm cấp ban hành thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thêm được cấp ban hành." + ex.Message);
            }
        }

        private void Edit()
        {
            try
            {
                if (SelectedItem == null)
                {
                    _messageService.Warning("Vui lòng chọn dòng cần sửa!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditName))
                {
                    _messageService.Warning("Vui lòng nhập tên cấp ban hành!");
                    return;
                }

                _data.Update(SelectedItem.Id, "", EditName.Trim());
                LoadData();
                _messageService.Info("Cập nhật cấp ban hành thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không cập nhật được cấp ban hành." + ex.Message);
            }
        }

        private void Delete()
        {
            try
            {
                if (SelectedItem == null)
                {
                    _messageService.Warning("Vui lòng chọn dòng cần xóa!");
                    return;
                }

                if (!_messageService.Confirm("Xóa cấp ban hành đã chọn?")) return;

                _data.Delete(SelectedItem.Id);
                LoadData();
                _messageService.Info("Xóa cấp ban hành thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không xóa được cấp ban hành." + ex.Message);
            }
        }
    }
}
