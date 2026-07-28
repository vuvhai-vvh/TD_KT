using System.Windows;
using System.Windows.Controls;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class IntroConfigDialog : Window
    {
        private IntroConfigViewModel _viewModel;

        public IntroConfigDialog()
        {
            InitializeComponent();
            _viewModel = new IntroConfigViewModel();
            DataContext = _viewModel;
        }

        private void DgCouncilMembers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel.SelectedCouncilMember == null) return;

            txtCouncilFullName.Text = _viewModel.SelectedCouncilMember.FullName ?? string.Empty;
            txtCouncilRank.Text = _viewModel.SelectedCouncilMember.Rank ?? string.Empty;
            txtCouncilPosition.Text = _viewModel.SelectedCouncilMember.Position ?? string.Empty;
            txtCouncilRole.Text = _viewModel.SelectedCouncilMember.Role ?? string.Empty;
        }

        private void DgEmulationTeams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel.SelectedEmulationTeam == null) return;

            txtTeamName.Text = _viewModel.SelectedEmulationTeam.TeamName ?? string.Empty;
            txtTeamLeader.Text = _viewModel.SelectedEmulationTeam.Leader ?? string.Empty;
            txtTeamMembers.Text = _viewModel.SelectedEmulationTeam.Members ?? string.Empty;
        }

        private void BtnAddCouncilMember_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddCouncilMember(
                txtCouncilFullName.Text,
                txtCouncilRank.Text,
                txtCouncilPosition.Text,
                txtCouncilRole.Text
            );
            ClearCouncilForm();
        }

        private void BtnEditCouncilMember_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedCouncilMember != null)
            {
                _viewModel.SelectedCouncilMember.FullName = txtCouncilFullName.Text;
                _viewModel.SelectedCouncilMember.Rank = txtCouncilRank.Text;
                _viewModel.SelectedCouncilMember.Position = txtCouncilPosition.Text;
                _viewModel.SelectedCouncilMember.Role = txtCouncilRole.Text;
            }
        }

        private void BtnDeleteCouncilMember_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteCouncilMember();
        }

        private void BtnAddEmulationTeam_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddEmulationTeam(
                txtTeamName.Text,
                txtTeamLeader.Text,
                txtTeamMembers.Text
            );
            ClearTeamForm();
        }

        private void BtnEditEmulationTeam_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedEmulationTeam != null)
            {
                _viewModel.SelectedEmulationTeam.TeamName = txtTeamName.Text;
                _viewModel.SelectedEmulationTeam.Leader = txtTeamLeader.Text;
                _viewModel.SelectedEmulationTeam.Members = txtTeamMembers.Text;
            }
        }

        private void BtnDeleteEmulationTeam_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteEmulationTeam();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadData();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CouncilFunctions = txtCouncilFunctions.Text;
            _viewModel.Save();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearCouncilForm()
        {
            txtCouncilFullName.Text = "";
            txtCouncilRank.Text = "";
            txtCouncilPosition.Text = "";
            txtCouncilRole.Text = "";
        }

        private void ClearTeamForm()
        {
            txtTeamName.Text = "";
            txtTeamLeader.Text = "";
            txtTeamMembers.Text = "";
        }
    }
}