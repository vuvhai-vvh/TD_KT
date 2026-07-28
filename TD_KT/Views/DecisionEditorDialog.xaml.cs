using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TD_KT.Data;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class DecisionEditorDialog : Window
    {
        public bool DeleteRequested { get; private set; }
        public bool IsEditMode { get; }
        public int DecisionId { get; }

        public string DecisionNumber => txtDecisionNumber.Text?.Trim() ?? string.Empty;
        public IssuingLevelDisplayModel SelectedIssuingLevel => cboIssuingLevel.SelectedItem as IssuingLevelDisplayModel;
        public string Signer => txtSigner.Text?.Trim() ?? string.Empty;
        public DateTime? SignedDate => dpSignedDate.SelectedDate;
        public string Note => txtNote.Text?.Trim() ?? string.Empty;

        public DecisionEditorDialog(IEnumerable<IssuingLevelDisplayModel> issuingLevels, DecisionHeaderRow selectedDecision, string suggestedDecisionNumber)
        {
            InitializeComponent();

            cboIssuingLevel.ItemsSource = issuingLevels;
            IsEditMode = selectedDecision != null;
            DecisionId = selectedDecision?.Id ?? 0;
            txtDialogTitle.Text = IsEditMode ? "CHỈNH SỬA QUYẾT ĐỊNH" : "THÊM QUYẾT ĐỊNH";

            txtDecisionNumber.Text = selectedDecision?.DecisionNumber ?? (suggestedDecisionNumber ?? string.Empty);
            txtSigner.Text = selectedDecision?.Signer ?? string.Empty;
            dpSignedDate.SelectedDate = selectedDecision?.SignedDate;
            txtNote.Text = selectedDecision?.Note ?? string.Empty;

            if (selectedDecision != null)
            {
                foreach (var item in cboIssuingLevel.Items)
                {
                    if (item is IssuingLevelDisplayModel level && level.Id == selectedDecision.IssuingLevelId)
                    {
                        cboIssuingLevel.SelectedItem = level;
                        break;
                    }
                }
            }

            btnDelete.Visibility = IsEditMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested = true;
            DialogResult = true;
        }
    }
}