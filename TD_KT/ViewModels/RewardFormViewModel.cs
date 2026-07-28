using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using TD_KT.Data;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class RewardFormViewModel : BaseViewModel
    {
        private readonly RewardFormData _rewardFormData;
        private readonly IMessageService _messageService;

        private ObservableCollection<RewardFormDisplayModel> _rewardForms;
        public ObservableCollection<RewardFormDisplayModel> RewardForms
        {
            get => _rewardForms;
            set => SetProperty(ref _rewardForms, value);
        }

        public ICollectionView RewardFormsView { get; private set; }

        private RewardFormDisplayModel _selectedRewardForm;
        public RewardFormDisplayModel SelectedRewardForm
        {
            get => _selectedRewardForm;
            set
            {
                if (SetProperty(ref _selectedRewardForm, value))
                {
                    FillEditForm();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); RewardFormsView?.Refresh(); }
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

        public RewardFormViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService())
        {
        }

        public RewardFormViewModel(IConnectionStringProvider csProvider, IMessageService messageService)
        {
            _rewardFormData = new RewardFormData(csProvider);
            _messageService = messageService;

            RewardForms = new ObservableCollection<RewardFormDisplayModel>();
            RewardFormsView = CollectionViewSource.GetDefaultView(RewardForms);
            RewardFormsView.Filter = Filter;

            RefreshCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedRewardForm != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedRewardForm != null);

            LoadData();
        }

        private bool Filter(object obj)
        {
            if (obj is RewardFormDisplayModel m)
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
                RewardForms.Clear();
                foreach (var item in _rewardFormData.GetAllDisplay())
                    RewardForms.Add(item);

                RewardFormsView?.Refresh();
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được danh sách hình thức khen thưởng.\n" + ex.Message);
            }
        }

        private void FillEditForm()
        {
            if (SelectedRewardForm == null)
                return;

            EditName = SelectedRewardForm.Name;
        }

        private void Add()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditName))
                {
                    _messageService.Warning("Vui lòng nhập tên hình thức khen thưởng!");
                    return;
                }

                _rewardFormData.Insert("", EditName.Trim());
                LoadData();
                _messageService.Info("Thêm hình thức khen thưởng thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không thêm được hình thức khen thưởng.\n" + ex.Message);
            }
        }

        private void Edit()
        {
            try
            {
                if (SelectedRewardForm == null)
                {
                    _messageService.Warning("Vui lòng chọn dòng cần sửa!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditName))
                {
                    _messageService.Warning("Vui lòng nhập tên hình thức khen thưởng!");
                    return;
                }

                _rewardFormData.Update(SelectedRewardForm.Id, "", EditName.Trim());
                LoadData();
                _messageService.Info("Cập nhật hình thức khen thưởng thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không cập nhật được hình thức khen thưởng.\n" + ex.Message);
            }
        }

        private void Delete()
        {
            try
            {
                if (SelectedRewardForm == null)
                {
                    _messageService.Warning("Vui lòng chọn dòng cần xóa!");
                    return;
                }

                if (!_messageService.Confirm($"Bạn có chắc muốn xóa '{SelectedRewardForm.Name}'?"))
                    return;

                _rewardFormData.Delete(SelectedRewardForm.Id);
                LoadData();
                _messageService.Info("Đã xóa hình thức khen thưởng!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không xóa được hình thức khen thưởng.\n" + ex.Message);
            }
        }
    }

    public class RewardFormDisplayModel
    {
        public int Stt { get; set; }
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}