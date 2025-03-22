using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.DTOs.Tags;

using FluentValidation;

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
            .Select(habit => habit.ToHabitDto())
            .ToListAsync();

        var habitsCollection = new HabitsCollectionDto
        {
            Data = habits
        };

        return Ok(habitsCollection);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsDto>> GetHabit(string id)
    {
        var habitDto = await dbContext
            .Habits
            .Include(habit => habit.Tags)
            .Where(habit => habit.Id == id)
            .Select(habit => habit.ToHabitWithTagsDto())
            .FirstOrDefaultAsync();

        if (habitDto is null)
        {
            return NotFound();
        }

        return Ok(habitDto);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator,
        ProblemDetailsFactory problemDetailsFactory)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

        //var validationResult = await validator.ValidateAsync(createHabitDto);

        //if (!validationResult.IsValid)
        //{
        //    var problem = problemDetailsFactory
        //        .CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest);

        //    problem.Extensions.Add("errors", validationResult.ToDictionary());

        //    return BadRequest(problem);
        //}

        var habit = createHabitDto.ToEntity();

        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync();

        var habitDto = habit.ToHabitDto();

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHabit(string id, UpdateHabitDto updateHabitDto)
    {
        var habit = await dbContext
            .Habits
            .Where(habit => habit.Id == id)
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromDto(updateHabitDto);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        var habit = await dbContext
            .Habits
            .Where(habit => habit.Id == id)
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        var habitDto = habit.ToHabitDto();
        patchDocument.ApplyTo(habitDto, ModelState);

        if (!TryValidateModel(habitDto))
        {
            return ValidationProblem(ModelState);
        }

        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHabit(string id)
    {
        var habit = await dbContext
            .Habits
            .FirstOrDefaultAsync(habit => habit.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        dbContext.Habits.Remove(habit);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
