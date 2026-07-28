using System;

namespace TD_KT.ViewModels
{
    public class WeeklyUnitScoreDisplayModel : BaseViewModel
    {
        public int OrgUnitId { get; }
        public string OrgUnitName { get; }

        private int _groupId;
        /// <summary>
        /// Nhóm thi đua (1..4) để tô màu và xếp hạng riêng.
        /// </summary>
        public int GroupId
        {
            get => _groupId;
            set => SetProperty(ref _groupId, value);
        }

        private int _totalViolations;
        public int TotalViolations
        {
            get => _totalViolations;
            set => SetProperty(ref _totalViolations, value);
        }

        private decimal _bonusPoints;
        public decimal BonusPoints
        {
            get => _bonusPoints;
            set
            {
                if (SetProperty(ref _bonusPoints, value))
                    RecalcTotal();
            }
        }

        private decimal? _scoreArea1;
        public decimal? ScoreArea1
        {
            get => _scoreArea1;
            set
            {
                if (SetProperty(ref _scoreArea1, value))
                    RecalcTotal();
            }
        }

        private decimal? _scoreArea2;
        public decimal? ScoreArea2
        {
            get => _scoreArea2;
            set
            {
                if (SetProperty(ref _scoreArea2, value))
                    RecalcTotal();
            }
        }

        private decimal? _scoreArea3;
        public decimal? ScoreArea3
        {
            get => _scoreArea3;
            set
            {
                if (SetProperty(ref _scoreArea3, value))
                    RecalcTotal();
            }
        }

        private decimal? _scoreArea4;
        public decimal? ScoreArea4
        {
            get => _scoreArea4;
            set
            {
                if (SetProperty(ref _scoreArea4, value))
                    RecalcTotal();
            }
        }

        // Theo yêu cầu mới: cột hiển thị "Tổng điểm của tuần" = (avg 4 mặt) + Điểm cộng
        private decimal? _averageScore;
        public decimal? AverageScore
        {
            get => _averageScore;
            private set => SetProperty(ref _averageScore, value);
        }

        private int? _ranking;
        public int? Ranking
        {
            get => _ranking;
            set => SetProperty(ref _ranking, value);
        }

        public WeeklyUnitScoreDisplayModel(int orgUnitId, string orgUnitName, int groupId = 0)
        {
            OrgUnitId = orgUnitId;
            OrgUnitName = orgUnitName ?? string.Empty;
            _groupId = groupId;
        }

        public void SetScores(decimal? s1, decimal? s2, decimal? s3, decimal? s4, decimal? totalScore, int? rank, int totalViolations = 0, decimal bonusPoints = 0m)
        {
            _scoreArea1 = s1;
            _scoreArea2 = s2;
            _scoreArea3 = s3;
            _scoreArea4 = s4;
            _bonusPoints = bonusPoints;
            _totalViolations = totalViolations;
            _averageScore = totalScore;
            _ranking = rank;

            OnPropertyChanged(nameof(ScoreArea1));
            OnPropertyChanged(nameof(ScoreArea2));
            OnPropertyChanged(nameof(ScoreArea3));
            OnPropertyChanged(nameof(ScoreArea4));
            OnPropertyChanged(nameof(BonusPoints));
            OnPropertyChanged(nameof(TotalViolations));
            OnPropertyChanged(nameof(AverageScore));
            OnPropertyChanged(nameof(Ranking));
        }

        public void SetComputedScores(decimal s1, decimal s2, decimal s3, decimal s4, int totalViolations, decimal bonusPoints, int? ranking)
        {
            ScoreArea1 = s1;
            ScoreArea2 = s2;
            ScoreArea3 = s3;
            ScoreArea4 = s4;
            TotalViolations = totalViolations;
            BonusPoints = bonusPoints;
            Ranking = ranking;
            RecalcTotal();
        }

        private void RecalcTotal()
        {
            // avg 4 mặt (đủ 4 cột) + bonus
            if (!ScoreArea1.HasValue || !ScoreArea2.HasValue || !ScoreArea3.HasValue || !ScoreArea4.HasValue)
            {
                AverageScore = null;
                return;
            }

            var avg = (ScoreArea1.Value + ScoreArea2.Value + ScoreArea3.Value + ScoreArea4.Value) / 4m;
            var total = avg + BonusPoints;

            AverageScore = total;
        }
    }
}
