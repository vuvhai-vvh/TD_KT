using System.Windows;

namespace TD_KT.Services
{
    public interface IHometownPickerService
    {
        /// <summary>
        /// Mở cửa sổ chọn Quê quán và trả về chuỗi đã chọn. Trả về null nếu hủy.
        /// </summary>
        string PickHometown(Window owner);
    }
}
