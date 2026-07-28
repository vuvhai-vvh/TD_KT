using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class ViolationEntryWindow : Window
    {
        private readonly WeeklyViewModel _viewModel;
        private readonly DispatcherTimer _recordDateTimeTimer;

        public ViolationEntryWindow(WeeklyViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            _recordDateTimeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _recordDateTimeTimer.Tick += (_, __) => _viewModel.UpdateRecordDateTimeToNow();
            _recordDateTimeTimer.Start();
        }

        // Thêm hàm này để có thể kéo cửa sổ
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnSaveViolation_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SaveViolation();
            if (!_viewModel.IsViolationEditing)
                Close();
        }

        private void BtnCancelViolation_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CancelViolationEdit();
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _recordDateTimeTimer.Stop();
            if (_viewModel.IsViolationEditing)
                _viewModel.CancelViolationEdit();

            base.OnClosing(e);
        }
    }
}