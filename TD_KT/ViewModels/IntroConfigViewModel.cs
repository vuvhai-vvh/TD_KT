using System;
using System.Collections.ObjectModel;
using System.Linq;
using TD_KT.Data;
using TD_KT.Services;

namespace TD_KT.ViewModels
{
    public class IntroConfigViewModel : BaseViewModel
    {
        public static event Action ConfigSaved;
        private readonly IntroConfigData _data;
        private readonly IMessageService _message;

        private string _councilFunctions;
        public string CouncilFunctions
        {
            get => _councilFunctions;
            set => SetProperty(ref _councilFunctions, value);
        }

        public ObservableCollection<CouncilMemberModel> CouncilMembers { get; }
        public ObservableCollection<EmulationTeamModel> EmulationTeams { get; }

        private CouncilMemberModel _selectedCouncilMember;
        public CouncilMemberModel SelectedCouncilMember
        {
            get => _selectedCouncilMember;
            set => SetProperty(ref _selectedCouncilMember, value);
        }

        private EmulationTeamModel _selectedEmulationTeam;
        public EmulationTeamModel SelectedEmulationTeam
        {
            get => _selectedEmulationTeam;
            set => SetProperty(ref _selectedEmulationTeam, value);
        }

        public IntroConfigViewModel()
            : this(new ConnectionStringProvider("Database"), new MessageService())
        {
        }

        public IntroConfigViewModel(IConnectionStringProvider csProvider, IMessageService messageService)
        {
            _data = new IntroConfigData(csProvider ?? new ConnectionStringProvider("Database"));
            _message = messageService ?? new MessageService();

            CouncilMembers = new ObservableCollection<CouncilMemberModel>();
            EmulationTeams = new ObservableCollection<EmulationTeamModel>();

            LoadData();
        }

        public void LoadData()
        {
            try
            {
                CouncilFunctions = _data.GetContent("functions");
                LoadCouncilMembers();
                LoadEmulationTeams();
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi tải dữ liệu cấu hình giới thiệu: {ex.Message}");
            }
        }

        private static string[] SplitLines(string content)
        {
            return (content ?? string.Empty)
                .Replace("\r\n", "\n")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void LoadCouncilMembers()
        {
            CouncilMembers.Clear();

            var content = _data.GetContent("council_members");
            var lines = SplitLines(content);

            int stt = 1;
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 4) continue;

                CouncilMembers.Add(new CouncilMemberModel
                {
                    Stt = stt++,
                    FullName = parts[0].Trim(),
                    Rank = parts[1].Trim(),
                    Position = parts[2].Trim(),
                    Role = parts[3].Trim()
                });
            }
        }

        private void LoadEmulationTeams()
        {
            EmulationTeams.Clear();

            var content = _data.GetContent("emulation_teams");
            var lines = SplitLines(content);

            int stt = 1;
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 3) continue;

                EmulationTeams.Add(new EmulationTeamModel
                {
                    Stt = stt++,
                    TeamName = parts[0].Trim(),
                    Leader = parts[1].Trim(),
                    Members = parts[2].Trim()
                });
            }
        }

        public void AddCouncilMember(string fullName, string rank, string position, string role)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return;

            CouncilMembers.Add(new CouncilMemberModel
            {
                Stt = CouncilMembers.Count + 1,
                FullName = fullName?.Trim(),
                Rank = rank?.Trim(),
                Position = position?.Trim(),
                Role = role?.Trim()
            });
        }

        public void DeleteCouncilMember()
        {
            if (SelectedCouncilMember == null) return;

            CouncilMembers.Remove(SelectedCouncilMember);
            Reindex(CouncilMembers);
        }

        public void AddEmulationTeam(string teamName, string leader, string members)
        {
            if (string.IsNullOrWhiteSpace(teamName)) return;

            EmulationTeams.Add(new EmulationTeamModel
            {
                Stt = EmulationTeams.Count + 1,
                TeamName = teamName?.Trim(),
                Leader = leader?.Trim(),
                Members = members?.Trim()
            });
        }

        public void DeleteEmulationTeam()
        {
            if (SelectedEmulationTeam == null) return;

            EmulationTeams.Remove(SelectedEmulationTeam);
            Reindex(EmulationTeams);
        }

        private static void Reindex<T>(ObservableCollection<T> list) where T : IHasStt
        {
            int stt = 1;
            foreach (var item in list)
                item.Stt = stt++;
        }

        public void Save()
        {
            try
            {
                _data.Upsert("functions", "Chức năng", CouncilFunctions ?? string.Empty);

                var councilContent = string.Join("\n",
                    CouncilMembers.Select(m => $"{m.FullName}|{m.Rank}|{m.Position}|{m.Role}"));
                _data.Upsert("council_members", "Hội đồng", councilContent);

                var teamContent = string.Join("\n",
                    EmulationTeams.Select(t => $"{t.TeamName}|{t.Leader}|{t.Members}"));
                _data.Upsert("emulation_teams", "Tổ thi đua", teamContent);

                _message.Info("Lưu cấu hình thành công!");
                ConfigSaved?.Invoke();
            }
            catch (Exception ex)
            {
                _message.Error($"Lỗi lưu cấu hình: {ex.Message}");
            }
        }
    }

    public interface IHasStt
    {
        int Stt { get; set; }
    }

    public class CouncilMemberModel : BaseViewModel, IHasStt
    {
    private int _stt;
    public int Stt
    {
        get => _stt;
        set => SetProperty(ref _stt, value);
    }

    private string _fullName;
    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    private string _rank;
    public string Rank
    {
        get => _rank;
        set => SetProperty(ref _rank, value);
    }

    private string _position;
    public string Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    private string _role;
    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }
}

    public class EmulationTeamModel : BaseViewModel, IHasStt
    {
        private int _stt;
        public int Stt
        {
            get => _stt;
            set => SetProperty(ref _stt, value);
        }

        private string _teamName;
        public string TeamName
        {
            get => _teamName;
            set => SetProperty(ref _teamName, value);
        }

        private string _leader;
        public string Leader
        {
            get => _leader;
            set => SetProperty(ref _leader, value);
        }

        private string _members;
        public string Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }
    }
}
