using Microsoft.Win32;

namespace TD_KT.Services
{
    public class FileDialogService : IFileDialogService
    {
        public bool TryPickFile(out string filePath)
        {
            filePath = null;

            var dlg = new OpenFileDialog
            {
                Title = "Chọn file văn bản",
                Filter = "Tất cả file|*.*|PDF|*.pdf|Word|*.doc;*.docx|Excel|*.xls;*.xlsx"
            };

            var ok = dlg.ShowDialog() == true;
            if (ok) filePath = dlg.FileName;
            return ok;
        }

        public bool TryPickSavePath(string defaultFileName, out string savePath)
        {
            savePath = null;

            var dlg = new SaveFileDialog
            {
                Title = "Lưu file",
                FileName = defaultFileName ?? "document"
            };

            var ok = dlg.ShowDialog() == true;
            if (ok) savePath = dlg.FileName;
            return ok;
        }
    }
}
