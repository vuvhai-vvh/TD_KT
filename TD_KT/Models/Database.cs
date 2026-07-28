using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace TD_KT.Models
{
    public partial class Database : DbContext
    {
        public Database()
            : base("name=Database")
        {
        }

        public virtual DbSet<DecisionAttachment> DecisionAttachments { get; set; }
        public virtual DbSet<DecisionDetail> DecisionDetails { get; set; }
        public virtual DbSet<Decision> Decisions { get; set; }
        public virtual DbSet<DocumentProposal> DocumentProposals { get; set; }
        public virtual DbSet<IntroConfig> IntroConfigs { get; set; }
        public virtual DbSet<IssuingLevel> IssuingLevels { get; set; }
        public virtual DbSet<Location> Locations { get; set; }
        public virtual DbSet<OrgUnit> OrgUnits { get; set; }
        public virtual DbSet<Position> Positions { get; set; }
        public virtual DbSet<Rank> Ranks { get; set; }
        public virtual DbSet<RewardForm> RewardForms { get; set; }
        public virtual DbSet<RewardHistory> RewardHistories { get; set; }
        public virtual DbSet<RewardReportSnapshot> RewardReportSnapshots { get; set; }
        public virtual DbSet<SeniorityAward> SeniorityAwards { get; set; }
        public virtual DbSet<Soldier> Soldiers { get; set; }
        public virtual DbSet<sysdiagram> sysdiagrams { get; set; }
        public virtual DbSet<UnitDocumentProposal> UnitDocumentProposals { get; set; }
        public virtual DbSet<UnitScoreSummary> UnitScoreSummaries { get; set; }
        public virtual DbSet<UserPermission> UserPermissions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<WeeklyScore> WeeklyScores { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Decision>()
                .HasMany(e => e.RewardReportSnapshots)
                .WithRequired(e => e.Decision)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<IssuingLevel>()
                .HasMany(e => e.Decisions)
                .WithRequired(e => e.IssuingLevel)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<IssuingLevel>()
                .HasMany(e => e.RewardForms)
                .WithRequired(e => e.IssuingLevel)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<IssuingLevel>()
                .HasMany(e => e.RewardHistories)
                .WithRequired(e => e.IssuingLevel)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<IssuingLevel>()
                .HasMany(e => e.RewardReportSnapshots)
                .WithRequired(e => e.IssuingLevel)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Location>()
                .HasMany(e => e.Locations1)
                .WithOptional(e => e.Location1)
                .HasForeignKey(e => e.ParentId);

            modelBuilder.Entity<OrgUnit>()
                .HasMany(e => e.OrgUnits1)
                .WithOptional(e => e.OrgUnit1)
                .HasForeignKey(e => e.ParentId);

            modelBuilder.Entity<OrgUnit>()
                .HasMany(e => e.UnitScoreSummaries)
                .WithRequired(e => e.OrgUnit)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<OrgUnit>()
                .HasMany(e => e.Users)
                .WithRequired(e => e.OrgUnit)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Position>()
                .HasMany(e => e.Users)
                .WithRequired(e => e.Position)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Soldier>()
                .HasMany(e => e.SeniorityAwards)
                .WithRequired(e => e.Soldier)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UnitScoreSummary>()
                .Property(e => e.ScoreArea1)
                .HasPrecision(5, 2);

            modelBuilder.Entity<UnitScoreSummary>()
                .Property(e => e.ScoreArea2)
                .HasPrecision(5, 2);

            modelBuilder.Entity<UnitScoreSummary>()
                .Property(e => e.ScoreArea3)
                .HasPrecision(5, 2);

            modelBuilder.Entity<UnitScoreSummary>()
                .Property(e => e.ScoreArea4)
                .HasPrecision(5, 2);

            modelBuilder.Entity<UnitScoreSummary>()
                .Property(e => e.AverageScore)
                .HasPrecision(5, 2);
        }
    }
}
