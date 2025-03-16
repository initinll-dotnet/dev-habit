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
            .Select(habit => habit.ToDto())
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
        var habitDto = await dbContext
            .Habits
            .Where(habit => habit.Id == id)
            .Select(habit => habit.ToDto())
            .FirstOrDefaultAsync();

        if (habitDto is null)
        {
            return NotFound();
        }

        return Ok(habitDto);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(CreateHabitDto createHabitDto)
    {
        var habit = createHabitDto.ToEntity();

        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync();

        var habitDto = habit.ToDto();

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }
}
