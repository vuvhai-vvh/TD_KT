namespace TD_KT.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Cơ chế cũ: copy file vào thư mục ứng dụng và trả về đường dẫn tương đối.
        /// Vẫn giữ để đọc hồ sơ đã tải lên trước phiên bản lưu file trong CSDL.
        /// </summary>
        string SaveToAppStorage(string sourceFilePath);

        string ResolvePath(string filePath);

        void OpenWithDefaultApp(string filePath);

        void CopyTo(string fromFilePath, string toFilePath);

        /// <summary>Mở nội dung file lấy từ CSDL bằng ứng dụng mặc định của Windows.</summary>
        void OpenBytesWithDefaultApp(byte[] fileData, string fileName);

        /// <summary>Ghi nội dung file lấy từ CSDL xuống đường dẫn người dùng đã chọn.</summary>
        void SaveBytesTo(byte[] fileData, string toFilePath);
    }
}
