using System;
using System.Windows;
using TD_KT.Services;
using TD_KT.Views;

namespace TD_KT.ViewModels
{
    /// <summary>
    /// TAB Danh mục (Quản trị hệ thống) - MVVM
    /// Chỉ chịu trách nhiệm mở các dialog danh mục.
    /// </summary>
    public class CatalogViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;

        public RelayCommand OpenOrgUnitDialogCommand { get; }
        public RelayCommand OpenPositionDialogCommand { get; }
        public RelayCommand OpenHometownDialogCommand { get; }
        public RelayCommand OpenRewardFormDialogCommand { get; }
        public RelayCommand OpenIssuingLevelDialogCommand { get; }
        public RelayCommand OpenIntroConfigDialogCommand { get; }

        public CatalogViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            OpenOrgUnitDialogCommand = new RelayCommand(_ => _dialogService.ShowDialog(new OrgUnitDialog()));
            OpenPositionDialogCommand = new RelayCommand(_ => _dialogService.ShowDialog(new PositionDialog()));
            OpenHometownDialogCommand = new RelayCommand(_ => _dialogService.ShowDialog(new HometownDialog()));
            OpenRewardFormDialogCommand = new RelayCommand(_ => _dialogService.ShowDialog(new RewardFormDialog()));
            OpenIntroConfigDialogCommand = new RelayCommand(_ => _dialogService.ShowDialog(new IntroConfigDialog()));
        }
    }
}
