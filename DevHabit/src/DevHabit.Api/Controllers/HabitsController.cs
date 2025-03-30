using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services.Sorting;

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
    public async Task<ActionResult<PaginationResult<HabitDto>>> GetHabits(
        [FromQuery] HabitsQueryParameters query,
        SortMappingProvider sortMappingProvider)
    {
        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provider sort parameter isn't valid: '{query.Sort}'");
        }

        query.Search ??= query.Search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider
            .GetMapping<HabitDto, Habit>(query.Sort ?? string.Empty);

        IQueryable<HabitDto>? habitsQuery = dbContext.Habits
            .Where(habit =>
                    query.Search == null ||
                    habit.Name.ToLower().Contains(query.Search) ||
                    habit.Description != null && habit.Description.ToLower().Contains(query.Search))
            .Where(habit => query.Type == null || habit.Type == query.Type)
            .Where(habit => query.Status == null || habit.Status == query.Status)
            .ApplySort(query.Sort, sortMappings)
            .Select(habit => habit.ToHabitDto());

        var paginationResult = await PaginationResult<HabitDto>
            .CreateAsync(habitsQuery, query.Page, query.PageSize);

        return Ok(paginationResult);
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
