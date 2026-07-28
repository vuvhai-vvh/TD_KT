namespace TD_KT.Services
{
    public interface IFileDialogService
    {
        bool TryPickFile(out string filePath);
        bool TryPickSavePath(string defaultFileName, out string savePath);
    }
}
