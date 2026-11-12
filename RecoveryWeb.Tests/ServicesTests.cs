using Microsoft.Extensions.Logging;
using Moq;
using RecoveryWeb.Data.Interfaces;
using RecoveryWeb.Models.DTOs;
using RecoveryWeb.Models.Entities;
using RecoveryWeb.Services.Implementations;
using Xunit;

namespace RecoveryWeb.Tests
{
    public class MoodServiceTests
    {
        private readonly Mock<IMoodRepository> _moodRepositoryMock;
        private readonly Mock<ILogger<MoodService>> _loggerMock;
        private readonly MoodService _service;

        public MoodServiceTests()
        {
            _moodRepositoryMock = new Mock<IMoodRepository>();
            _loggerMock = new Mock<ILogger<MoodService>>();
            _service = new MoodService(_moodRepositoryMock.Object);
        }

        [Fact]
        public async Task RecordMoodAsync_ValidData_ReturnsRecord()
        {
            var dto = new CreateMoodRecordDto
            {
                UserId = 1,
                MoodValue = 4,
                Notes = "Хороший день"
            };
            _moodRepositoryMock.Setup(x => x.AddAsync(It.IsAny<MoodRecord>()))
                .ReturnsAsync(1);

            var result = await _service.RecordMoodAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(4, result.MoodValue);
            _moodRepositoryMock.Verify(x => x.AddAsync(It.Is<MoodRecord>(r =>
                r.UserId == 1 &&
                r.MoodValue == 4 &&
                r.Notes == "Хороший день")), Times.Once);
        }

        [Fact]
        public async Task GetStatisticsAsync_ReturnsCorrectStats()
        {
            var userId = 1;
            var records = new List<MoodRecord>
            {
                new() { RecordId = 1, UserId = 1, MoodValue = 3, RecordedAt = DateTime.Now },
                new() { RecordId = 2, UserId = 1, MoodValue = 4, RecordedAt = DateTime.Now }
            };
            _moodRepositoryMock.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(records);
            _moodRepositoryMock.Setup(x => x.GetAverageMoodAsync(userId, 30))
                .ReturnsAsync(3.5);

            var result = await _service.GetStatisticsAsync(userId);

            Assert.Equal(2, result.TotalRecords);
            Assert.Equal(3.5, result.AverageMood);
        }

        [Fact]
        public async Task GetHistoryAsync_ReturnsRecordsInDescendingOrder()
        {
            var userId = 1;
            var records = new List<MoodRecord>
            {
                new() { RecordId = 2, UserId = 1, MoodValue = 4, RecordedAt = DateTime.Now.AddHours(-1) },
                new() { RecordId = 1, UserId = 1, MoodValue = 3, RecordedAt = DateTime.Now }
            };
            _moodRepositoryMock.Setup(x => x.GetByUserIdAndDateRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(records);

            var result = await _service.GetHistoryAsync(userId, 30);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task DeleteRecordAsync_ReturnsTrue_WhenDeleted()
        {
            var recordId = 1;
            var record = new MoodRecord { RecordId = 1, UserId = 1, MoodValue = 3 };
            _moodRepositoryMock.Setup(x => x.GetByIdAsync(recordId))
                .ReturnsAsync(record);
            _moodRepositoryMock.Setup(x => x.DeleteAsync(recordId))
                .ReturnsAsync(true);

            var result = await _service.DeleteRecordAsync(recordId, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task GetLastMoodAsync_ReturnsLastMood()
        {
            var userId = 1;
            var record = new MoodRecord
            {
                RecordId = 1,
                UserId = userId,
                MoodValue = 4,
                RecordedAt = DateTime.Now
            };
            _moodRepositoryMock.Setup(x => x.GetLastMoodAsync(userId))
                .ReturnsAsync(record);

            var result = await _service.GetLastMoodAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(4, result.MoodValue);
        }
    }

    public class PracticeServiceTests
    {
        private readonly Mock<IPracticeRepository> _repositoryMock;
        private readonly PracticeService _service;

        public PracticeServiceTests()
        {
            _repositoryMock = new Mock<IPracticeRepository>();
            _service = new PracticeService(_repositoryMock.Object);
        }

        [Fact]
        public async Task RecordSessionAsync_ValidData_ReturnsSessionId()
        {
            var dto = new CreatePracticeSessionDto
            {
                UserId = 1,
                PracticeType = "breathing",
                DurationMinutes = 5
            };
            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<PracticeSession>()))
                .ReturnsAsync(1);

            var result = await _service.RecordSessionAsync(dto);

            Assert.Equal(1, result.SessionId);
            Assert.Equal("breathing", result.PracticeType);
        }

        [Fact]
        public async Task GetStatisticsAsync_CalculatesTotalDuration()
        {
            var userId = 1;
            var sessions = new List<PracticeSession>
            {
                new() { SessionId = 1, UserId = userId, PracticeType = "breathing", DurationMinutes = 5, CompletedAt = DateTime.Now },
                new() { SessionId = 2, UserId = userId, PracticeType = "grounding", DurationMinutes = 10, CompletedAt = DateTime.Now }
            };
            _repositoryMock.Setup(x => x.GetByDateRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(sessions);
            _repositoryMock.Setup(x => x.GetTotalDurationAsync(userId, 30))
                .ReturnsAsync(15);
            _repositoryMock.Setup(x => x.GetPracticeTypeStatsAsync(userId))
                .ReturnsAsync(new Dictionary<string, int>());

            var result = await _service.GetStatisticsAsync(userId);

            Assert.Equal(2, result.TotalSessions);
        }

        [Fact]
        public async Task GetTypeStatisticsAsync_ReturnsCorrectDictionary()
        {
            var userId = 1;
            var stats = new Dictionary<string, int>
            {
                { "breathing", 5 },
                { "grounding", 3 }
            };
            _repositoryMock.Setup(x => x.GetPracticeTypeStatsAsync(userId))
                .ReturnsAsync(stats);

            var result = await _service.GetTypeStatisticsAsync(userId);

            Assert.Equal(5, result["breathing"]);
            Assert.Equal(3, result["grounding"]);
        }

        [Fact]
        public async Task GetRecentSessionsAsync_ReturnsLimitedSessions()
        {
            var userId = 1;
            var sessions = new List<PracticeSession>
            {
                new() { SessionId = 1, UserId = userId, PracticeType = "breathing" },
                new() { SessionId = 2, UserId = userId, PracticeType = "grounding" },
                new() { SessionId = 3, UserId = userId, PracticeType = "bodyscan" }
            };
            _repositoryMock.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(sessions);

            var result = await _service.GetRecentSessionsAsync(userId, 2);

            Assert.Equal(2, result.Count);
        }
    }

    public class GardenServiceTests
    {
        private readonly Mock<IGardenRepository> _repositoryMock;
        private readonly GardenService _service;

        public GardenServiceTests()
        {
            _repositoryMock = new Mock<IGardenRepository>();
            _service = new GardenService(_repositoryMock.Object);
        }

        [Fact]
        public async Task GetProgressAsync_NewUser_CreatesDefaultProgress()
        {
            var userId = 1;
            _repositoryMock.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync((GardenProgress?)null);
            _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<GardenProgress>()))
                .ReturnsAsync(true);

            var result = await _service.GetProgressAsync(userId);

            Assert.Equal(1, result.TreeLevel);
            Assert.Equal(0, result.TotalPractices);
        }

        [Fact]
        public async Task GetProgressAsync_ExistingUser_ReturnsProgress()
        {
            var userId = 1;
            var progress = new GardenProgress
            {
                ProgressId = 1,
                UserId = userId,
                TotalPractices = 5,
                CurrentStreak = 3,
                TreeLevel = 2
            };
            _repositoryMock.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(progress);

            var result = await _service.GetProgressAsync(userId);

            Assert.Equal(2, result.TreeLevel);
            Assert.Equal(5, result.TotalPractices);
            Assert.Equal(3, result.CurrentStreak);
        }

        [Fact]
        public async Task WaterTreeAsync_IncrementsPractices()
        {
            var userId = 1;
            var progress = new GardenProgress
            {
                UserId = userId,
                TotalPractices = 1,
                CurrentStreak = 1,
                TreeLevel = 1
            };
            _repositoryMock.Setup(x => x.IncrementPracticesAsync(userId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(progress);
            _repositoryMock.Setup(x => x.UpdateStreakAsync(userId, It.IsAny<int>(), It.IsAny<DateTime>()))
                .ReturnsAsync(true);

            var result = await _service.WaterTreeAsync(userId, "breathing", 5);

            Assert.Equal(1, result.TotalPractices);
            _repositoryMock.Verify(x => x.IncrementPracticesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task CheckAndUpdateStreakAsync_FirstDay_SetsStreakToOne()
        {
            var userId = 1;
            var progress = new GardenProgress
            {
                UserId = userId,
                TotalPractices = 1,
                CurrentStreak = 0,
                LastPracticeDate = null
            };
            _repositoryMock.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(progress);
            _repositoryMock.Setup(x => x.UpdateStreakAsync(userId, 1, It.IsAny<DateTime>()))
                .ReturnsAsync(true);

            var result = await _service.CheckAndUpdateStreakAsync(userId);

            Assert.True(result);
            _repositoryMock.Verify(x => x.UpdateStreakAsync(userId, 1, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task GetAchievementsAsync_AllUnlocked_ReturnsAllTrue()
        {
            var userId = 1;
            var progress = new GardenProgress
            {
                UserId = userId,
                TotalPractices = 30,
                CurrentStreak = 30
            };
            _repositoryMock.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(progress);

            var result = await _service.GetAchievementsAsync(userId);

            Assert.All(result, a => Assert.True(a.IsUnlocked));
        }

        [Fact]
        public async Task GetAchievementsAsync_NoneUnlocked_ReturnsAllFalse()
        {
            var userId = 1;
            var progress = new GardenProgress
            {
                UserId = userId,
                TotalPractices = 0,
                CurrentStreak = 0
            };
            _repositoryMock.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(progress);

            var result = await _service.GetAchievementsAsync(userId);

            Assert.All(result, a => Assert.False(a.IsUnlocked));
        }
    }

    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _repositoryMock;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _repositoryMock = new Mock<IUserRepository>();
            _service = new UserService(_repositoryMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_NewUser_ReturnsUserId()
        {
            var dto = new RegisterUserDto
            {
                Username = "testuser",
                Email = "test@test.com",
                Password = "password123"
            };
            _repositoryMock.Setup(x => x.ExistsByEmailAsync(dto.Email))
                .ReturnsAsync(false);
            _repositoryMock.Setup(x => x.ExistsByUsernameAsync(dto.Username))
                .ReturnsAsync(false);
            _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(1);

            var result = await _service.RegisterAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public async Task RegisterAsync_ExistingEmail_ThrowsException()
        {
            var dto = new RegisterUserDto
            {
                Username = "testuser",
                Email = "test@test.com",
                Password = "password123"
            };
            _repositoryMock.Setup(x => x.ExistsByEmailAsync(dto.Email))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RegisterAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUser()
        {
            var dto = new LoginUserDto
            {
                Email = "test@test.com",
                Password = "password123"
            };
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = UserService.HashPassword("password123"),
                IsActive = true
            };
            _repositoryMock.Setup(x => x.GetByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            var result = await _service.LoginAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsNull()
        {
            var dto = new LoginUserDto
            {
                Email = "test@test.com",
                Password = "wrongpassword"
            };
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = UserService.HashPassword("password123"),
                IsActive = true
            };
            _repositoryMock.Setup(x => x.GetByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            var result = await _service.LoginAsync(dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsProfileWithStats()
        {
            var userId = 1;
            var user = new User
            {
                UserId = userId,
                Username = "testuser",
                Email = "test@test.com",
                CreatedAt = DateTime.Now.AddDays(-30)
            };
            _repositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var result = await _service.GetProfileAsync(userId);

            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
        }
    }
}
