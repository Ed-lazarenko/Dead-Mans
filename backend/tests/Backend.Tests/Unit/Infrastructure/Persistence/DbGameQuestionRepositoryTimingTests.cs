using backend.Data;
using backend.Data.Entities;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameQuestionRepositoryTimingTests
{
    [Fact]
    public async Task CreateCategoryAsync_UsesInjectedClock()
    {
        var timestamp = new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var repository = new DbGameQuestionRepository(
            dbContext,
            new FixedTimeProvider(timestamp)
        );

        var result = await repository.CreateCategoryAsync("Clock category");

        var category = await dbContext.QuestionCategories.SingleAsync(x => x.Id == result.Id);
        Assert.Equal(timestamp.UtcDateTime, category.CreatedAtUtc);
        Assert.Equal(timestamp.UtcDateTime, category.UpdatedAtUtc);
    }

    [Fact]
    public async Task SoftDeleteQuestionAsync_UsesOneInjectedTimestamp()
    {
        var timestamp = new DateTimeOffset(2036, 5, 6, 7, 8, 9, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var categoryId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        dbContext.QuestionCategories.Add(CreateCategory(categoryId, timestamp.AddDays(-2).UtcDateTime));
        dbContext.QuestionDefinitions.Add(
            new QuestionDefinition
            {
                Id = questionId,
                ExternalCode = "clock-delete",
                CategoryId = categoryId,
                Text = "Delete me?",
                Reward = 1,
                Revision = 1,
                IsEnabled = true,
                Priority = 1,
                CreatedAtUtc = timestamp.AddDays(-1).UtcDateTime,
                UpdatedAtUtc = timestamp.AddDays(-1).UtcDateTime
            }
        );
        await dbContext.SaveChangesAsync();
        var repository = new DbGameQuestionRepository(
            dbContext,
            new FixedTimeProvider(timestamp)
        );

        var deleted = await repository.SoftDeleteQuestionAsync(questionId);

        Assert.True(deleted);
        var question = await dbContext.QuestionDefinitions.SingleAsync();
        Assert.True(question.IsDeleted);
        Assert.False(question.IsEnabled);
        Assert.Equal(timestamp.UtcDateTime, question.DeletedAtUtc);
        Assert.Equal(question.DeletedAtUtc, question.UpdatedAtUtc);
    }

    [Fact]
    public async Task SetCategoryEnabledAsync_UsesInjectedClockForEveryQuestion()
    {
        var timestamp = new DateTimeOffset(2037, 6, 7, 8, 9, 10, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var categoryId = Guid.NewGuid();
        var previousTimestamp = timestamp.AddDays(-1).UtcDateTime;
        dbContext.QuestionCategories.Add(CreateCategory(categoryId, previousTimestamp));
        dbContext.QuestionDefinitions.AddRange(
            CreateQuestion(categoryId, "clock-enable-1", previousTimestamp),
            CreateQuestion(categoryId, "clock-enable-2", previousTimestamp)
        );
        await dbContext.SaveChangesAsync();
        var repository = new DbGameQuestionRepository(
            dbContext,
            new FixedTimeProvider(timestamp)
        );

        var updated = await repository.SetCategoryEnabledAsync(categoryId, isEnabled: false);

        Assert.True(updated);
        var questions = await dbContext.QuestionDefinitions.ToArrayAsync();
        Assert.All(questions, question =>
        {
            Assert.False(question.IsEnabled);
            Assert.Equal(timestamp.UtcDateTime, question.UpdatedAtUtc);
        });
    }

    private static QuestionCategory CreateCategory(Guid id, DateTime timestamp) =>
        new()
        {
            Id = id,
            Name = $"Category {id:N}",
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp
        };

    private static QuestionDefinition CreateQuestion(
        Guid categoryId,
        string externalCode,
        DateTime timestamp
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalCode = externalCode,
            CategoryId = categoryId,
            Text = "Question?",
            Reward = 1,
            Revision = 1,
            IsEnabled = true,
            Priority = 1,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp
        };

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
