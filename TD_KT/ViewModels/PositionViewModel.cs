using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using TD_KT.Data;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class PositionViewModel : BaseViewModel
    {
        private readonly PositionData _positionData;
        private readonly IMessageService _messageService;

        private ObservableCollection<PositionDisplayModel> _positions;
        public ObservableCollection<PositionDisplayModel> Positions
        {
            get => _positions;
            set => SetProperty(ref _positions, value);
        }

        public ICollectionView PositionsView { get; private set; }

        private PositionDisplayModel _selectedPosition;
        public PositionDisplayModel SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                if (SetProperty(ref _selectedPosition, value))
                {
                    FillEditForm();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); PositionsView?.Refresh(); }
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

        public PositionViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService())
        {
        }

        public PositionViewModel(IConnectionStringProvider csProvider, IMessageService messageService)
        {
            _positionData = new PositionData(csProvider);
            _messageService = messageService;

            Positions = new ObservableCollection<PositionDisplayModel>();
            PositionsView = CollectionViewSource.GetDefaultView(Positions);
            PositionsView.Filter = Filter;

            RefreshCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedPosition != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedPosition != null);

            LoadData();
        }

        private bool Filter(object obj)
        {
            if (obj is PositionDisplayModel m)
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    return true;

                var s = SearchText.ToLower();
                return (m.Name ?? "").ToLower().Contains(s);
            }
            return false;
        }

        private void LoadData()
        {
            try
            {
                Positions.Clear();
                foreach (var item in _positionData.GetAllDisplay())
                    Positions.Add(item);

                PositionsView?.Refresh();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được danh sách chức vụ.\n" + ex.Message);
            }
        }

        private void FillEditForm()
        {
            if (SelectedPosition == null)
                return;

            EditName = SelectedPosition.Name;
        }

        private void Add()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditName))
                {
                    _messageService.Warning("Vui lòng nhập tên chức vụ!");
                    return;
                }

                _positionData.Insert("", EditName.Trim());
                LoadData();
                _messageService.Info("Thêm chức vụ thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thêm được chức vụ.\n" + ex.Message);
            }
        }

        private void Edit()
        {
            try
            {
                if (SelectedPosition == null)
                {
                    _messageService.Warning("Vui lòng chọn dòng cần sửa!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditName))
                {
                    _messageService.Warning("Vui lòng nhập tên chức vụ!");
                    return;
                }

                _positionData.Update(SelectedPosition.Id, "", EditName.Trim());
                LoadData();
                _messageService.Info("Cập nhật chức vụ thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không cập nhật được chức vụ.\n" + ex.Message);
            }
        }

        private void Delete()
        {
            try
            {
                if (SelectedPosition == null)
                {
                    _messageService.Warning("Vui lòng chọn dòng cần xóa!");
                    return;
                }

                if (!_messageService.Confirm($"Bạn có chắc muốn xóa '{SelectedPosition.Name}'?"))
                    return;

                _positionData.Delete(SelectedPosition.Id);
                LoadData();
                _messageService.Info("Đã xóa chức vụ!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không xóa được chức vụ.\n" + ex.Message);
            }
        }
    }

    public class PositionDisplayModel
    {
        public int Stt { get; set; }
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
