using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;


[ApiController]
[Route("habits")]
public sealed class HabitsController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public HabitsController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<HabitsCollectionDto>> GetHabits()
    {
        var habits = await dbContext
            .Habits
            .Select(habit => new HabitDto
            {
                Id = habit.Id,
                Name = habit.Name,
                Description = habit.Description,
                Type = habit.Type,
                Frequency = new FrequencyDto
                {
                    Type = habit.Frequency.Type,
                    TimesPerPeriod = habit.Frequency.TimesPerPeriod
                },
                Target = new TargetDto
                {
                    Value = habit.Target.Value,
                    Unit = habit.Target.Unit
                },
                Status = habit.Status,
                IsArchived = habit.IsArchived,
                EndDate = habit.EndDate,
                Milestone = habit.Milestone != null ?
                    new MilestoneDto
                    {
                        Target = habit.Milestone.Target,
                        Current = habit.Milestone.Current
                    }
                    : null,
                CreatedAtUtc = habit.CreatedAtUtc ?? DateTime.UtcNow,
                UpdatedAtUtc = habit.UpdatedAtUtc,
                LastCompletedAtUtc = habit.LastCompletedAtUtc
            })
            .ToListAsync();

        var habitsCollection = new HabitsCollectionDto
        {
            Data = habits
        };

        return Ok(habitsCollection);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HabitDto>> GetHabit(string id)
    {
        var habit = await dbContext
            .Habits
            .Where(habit => habit.Id == id)
            .Select(habit => new HabitDto
            {
                Id = habit.Id,
                Name = habit.Name,
                Description = habit.Description,
                Type = habit.Type,
                Frequency = new FrequencyDto
                {
                    Type = habit.Frequency.Type,
                    TimesPerPeriod = habit.Frequency.TimesPerPeriod
                },
                Target = new TargetDto
                {
                    Value = habit.Target.Value,
                    Unit = habit.Target.Unit
                },
                Status = habit.Status,
                IsArchived = habit.IsArchived,
                EndDate = habit.EndDate,
                Milestone = habit.Milestone != null ?
                    new MilestoneDto
                    {
                        Target = habit.Milestone.Target,
                        Current = habit.Milestone.Current
                    }
                    : null,
                CreatedAtUtc = habit.CreatedAtUtc ?? DateTime.UtcNow,
                UpdatedAtUtc = habit.UpdatedAtUtc,
                LastCompletedAtUtc = habit.LastCompletedAtUtc
            })
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        return Ok(habit);
    }
}
