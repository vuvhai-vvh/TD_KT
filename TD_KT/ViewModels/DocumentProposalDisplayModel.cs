using System;

namespace TD_KT.ViewModels
{
    public class DocumentProposalDisplayModel
    {
        public int Stt { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string ProposingUnit { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public string FilePath { get; set; }
        public bool HasFileData { get; set; }

        public string UploadDateText
        {
            get { return UploadDate.ToString("dd/MM/yyyy HH:mm"); }
        }
    }
}
