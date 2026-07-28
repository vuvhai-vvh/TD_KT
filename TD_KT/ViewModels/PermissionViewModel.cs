using System;
using System.Collections.ObjectModel;
using TD_KT.Data;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class PermissionViewModel : BaseViewModel
    {
        private readonly UserPermissionData _permissionData;
        private readonly IMessageService _messageService;

        public ObservableCollection<UserPermissionDisplayModel> UserPermissions { get; private set; }
        public ObservableCollection<AdminPermissionDisplayModel> AdminPermissions { get; private set; }

        public RelayCommand ReloadCommand { get; }
        public RelayCommand SaveCommand { get; }

        public PermissionViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService())
        {
        }

        public PermissionViewModel(IConnectionStringProvider csProvider, IMessageService messageService)
        {
            _permissionData = new UserPermissionData(csProvider);
            _messageService = messageService;

            UserPermissions = new ObservableCollection<UserPermissionDisplayModel>();
            AdminPermissions = new ObservableCollection<AdminPermissionDisplayModel>();

            ReloadCommand = new RelayCommand(_ => LoadData());
            SaveCommand = new RelayCommand(_ => Save());

            LoadData();
        }

        public void LoadData()
        {
            try
            {
                _permissionData.EnsureUserPermissionsSeeded();

                UserPermissions.Clear();
                foreach (var m in _permissionData.GetUserPermissionsDisplay())
                    UserPermissions.Add(m);

                AdminPermissions.Clear();
                foreach (var m in _permissionData.GetAdminPermissionsDisplay())
                    AdminPermissions.Add(m);
            }
            catch (Exception ex)
            {
                _messageService.Error("Không tải được dữ liệu phân quyền.\n" + ex.Message);
            }
        }

        private void Save()
        {
            try
            {
                foreach (var p in UserPermissions)
                    _permissionData.UpsertUserPermissions(p);

                foreach (var a in AdminPermissions)
                    _permissionData.UpsertAdminPermissions(a);

                _messageService.Info("Lưu phân quyền thành công!");
            }
            catch (Exception ex)
            {
                _messageService.Error("Không lưu được phân quyền.\n" + ex.Message);
            }
        }
    }

    public class UserPermissionDisplayModel : BaseViewModel
    {
        public int Stt { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string OrgUnit { get; set; }
        public string Position { get; set; }

        private bool _viewTM;
        public bool ViewTM { get => _viewTM; set => SetProperty(ref _viewTM, value); }

        private bool _editTM;
        public bool EditTM { get => _editTM; set => SetProperty(ref _editTM, value); }

        private bool _viewCT;
        public bool ViewCT { get => _viewCT; set => SetProperty(ref _viewCT, value); }

        private bool _editCT;
        public bool EditCT { get => _editCT; set => SetProperty(ref _editCT, value); }

        private bool _viewHCKT;
        public bool ViewHCKT { get => _viewHCKT; set => SetProperty(ref _viewHCKT, value); }

        private bool _editHCKT;
        public bool EditHCKT { get => _editHCKT; set => SetProperty(ref _editHCKT, value); }

        private bool _viewNV;
        public bool ViewNV { get => _viewNV; set => SetProperty(ref _viewNV, value); }

        private bool _editNV;
        public bool EditNV { get => _editNV; set => SetProperty(ref _editNV, value); }

        private bool _canFinalizeScore;
        public bool CanFinalizeScore { get => _canFinalizeScore; set => SetProperty(ref _canFinalizeScore, value); }
    }

    public class AdminPermissionDisplayModel : BaseViewModel
    {
        public int Stt { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string OrgUnit { get; set; }
        public string Position { get; set; }

        private bool _canAccessAccountTab;
        public bool CanAccessAccountTab { get => _canAccessAccountTab; set => SetProperty(ref _canAccessAccountTab, value); }

        private bool _canAccessPermissionTab;
        public bool CanAccessPermissionTab { get => _canAccessPermissionTab; set => SetProperty(ref _canAccessPermissionTab, value); }

        private bool _canFinalizeScore;
        public bool CanFinalizeScore { get => _canFinalizeScore; set => SetProperty(ref _canFinalizeScore, value); }
        private bool _canLockWeek;
        public bool CanLockWeek { get => _canLockWeek; set => SetProperty(ref _canLockWeek, value); }
    }
}
