using System;
using System.Collections.ObjectModel;
using TD_KT.Data;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    /// <summary>
    /// Danh mục quê quán theo 2 cấp (cấp tỉnh và cấp xã) theo yêu cầu.
    /// DB không cần sửa: vẫn dùng dbo.Locations(Level, ParentId).
    /// Quy ước:
    /// - Level = 1: Tỉnh/TP
    /// - Level = 2: Xã/Phường/Đặc khu (ParentId = Id của Tỉnh/TP)
    /// </summary>
    public class HometownViewModel : BaseViewModel
    {
        private readonly LocationData _data;
        private readonly IMessageService _message;

        public ObservableCollection<Location> Provinces { get; }
        public ObservableCollection<Location> Communes { get; }

        private Location _selectedProvince;
        public Location SelectedProvince
        {
            get => _selectedProvince;
            set
            {
                if (SetProperty(ref _selectedProvince, value))
                {
                    SelectedCommune = null;
                    LoadCommunes();
                }
            }
        }

        private Location _selectedCommune;
        public Location SelectedCommune
        {
            get => _selectedCommune;
            set => SetProperty(ref _selectedCommune, value);
        }

        private string _selectedHometown;
        public string SelectedHometown
        {
            get => _selectedHometown;
            private set => SetProperty(ref _selectedHometown, value);
        }

        public HometownViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService())
        {
        }

        public HometownViewModel(IConnectionStringProvider csProvider, IMessageService messageService)
        {
            _data = new LocationData(csProvider ?? new ConnectionStringProvider("Database"));
            _message = messageService ?? new MessageService();

            Provinces = new ObservableCollection<Location>();
            Communes = new ObservableCollection<Location>();

            LoadProvinces();
        }

        public void LoadProvinces()
        {
            try
            {
                Provinces.Clear();
                // Load theo ParentId (đúng yêu cầu): ParentId NULL = danh sách Tỉnh/TP
                foreach (var loc in _data.GetByParent(parentId: null))
                    Provinces.Add(loc);

                SelectedProvince = null;
                Communes.Clear();
                SelectedCommune = null;
                SelectedHometown = string.Empty;
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi tải danh sách Tỉnh/TP: {ex.Message}");
            }
        }

        private void LoadCommunes()
        {
            Communes.Clear();
            if (SelectedProvince == null) return;

            try
            {
                // Load theo ParentId (đúng yêu cầu): ParentId = Id của Tỉnh/TP
                foreach (var loc in _data.GetByParent(parentId: SelectedProvince.Id))
                    Communes.Add(loc);
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi tải danh sách Xã/Phường/Đặc khu: {ex.Message}");
            }
        }

        public void AddProvince(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _message.Warning("Vui lòng nhập tên Tỉnh/TP!");
                return;
            }

            try
            {
                _data.Insert(name.Trim(), level: 1, parentId: null);
                LoadProvinces();
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi thêm Tỉnh/TP: {ex.Message}");
            }
        }

        public void DeleteProvince()
        {
            if (SelectedProvince == null) return;
            if (!_message.Confirm($"Bạn có chắc muốn xóa '{SelectedProvince.Name}'?")) return;

            try
            {
                _data.Delete(SelectedProvince.Id);
                LoadProvinces();
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi xóa Tỉnh/TP: {ex.Message}");
            }
        }

        public void AddCommune(string name)
        {
            if (SelectedProvince == null)
            {
                _message.Warning("Vui lòng chọn Tỉnh/TP trước!");
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                _message.Warning("Vui lòng nhập tên Xã/Phường/Đặc khu!");
                return;
            }

            try
            {
                _data.Insert(name.Trim(), level: 2, parentId: SelectedProvince.Id);
                LoadCommunes();
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi thêm Xã/Phường/Đặc khu: {ex.Message}");
            }
        }

        public void DeleteCommune()
        {
            if (SelectedCommune == null) return;
            if (!_message.Confirm($"Bạn có chắc muốn xóa '{SelectedCommune.Name}'?")) return;

            try
            {
                _data.Delete(SelectedCommune.Id);
                SelectedCommune = null;
                LoadCommunes();
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi xóa Xã/Phường/Đặc khu: {ex.Message}");
            }
        }

        private string GetSelectedHometown()
        {
            if (SelectedCommune != null && SelectedProvince != null)
                return $"{SelectedCommune.Name}, {SelectedProvince.Name}";

            if (SelectedProvince != null)
                return SelectedProvince.Name;

            return string.Empty;
        }

        public void ConfirmSelection()
        {
            SelectedHometown = GetSelectedHometown();
        }
    }
}
