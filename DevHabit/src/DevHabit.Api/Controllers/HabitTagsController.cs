using DevHabit.Api.Database;
using DevHabit.Api.DTOs.HabitTags;
using DevHabit.Api.Entities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;


[ApiController]
[Route("habits/{habitId}/tags")]
public sealed class HabitTagsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> UpsertHabitTags(string habitId, UpsertHabitTagsDto upsertHabitTagsDto)
    {
        // Retrieve the habit along with its associated tags from the database
        var habit = await dbContext.Habits
            .Include(h => h.HabitTags)
            .FirstOrDefaultAsync(h => h.Id == habitId);

        // If the habit does not exist, return a 404 Not Found response
        if (habit is null)
        {
            return NotFound();
        }

        // Get the current tag IDs associated with the habit
        var currentTagIds = habit.HabitTags
            .Select(ht => ht.TagId)
            .ToHashSet();

        // If the current tags are the same as the tags in the request, return a 204 No Content response
        if (currentTagIds.SetEquals(upsertHabitTagsDto.TagIds))
        {
            return NoContent();
        }

        // Retrieve the existing tag IDs from the database that match the requested tag IDs
        var existingTagIds = await dbContext.Tags
            .Where(t => upsertHabitTagsDto.TagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        // If the number of existing tags does not match the number of requested tags, return a 400 Bad Request response
        if (existingTagIds.Count != upsertHabitTagsDto.TagIds.Count)
        {
            return BadRequest("one or more tag ids is invalid");
        }

        // Remove tags that are no longer associated with the habit
        habit.HabitTags.RemoveAll(ht => !upsertHabitTagsDto.TagIds.Contains(ht.TagId));

        // Determine which tags need to be added
        var tagIdsToAdd = upsertHabitTagsDto.TagIds
            .Except(currentTagIds).ToArray();

        // Add the new tags to the habit
        habit.HabitTags.AddRange(tagIdsToAdd
            .Select(tagId => new HabitTag
            {
                HabitId = habitId,
                TagId = tagId,
                CreatedAtUtc = DateTime.UtcNow,
            }));

        // Save the changes to the database
        await dbContext.SaveChangesAsync();

        // Return a 204 No Content response
        return NoContent();
    }

    [HttpDelete("{tagId}")]
    public async Task<IActionResult> DeleteHabitTag(string habitId, string tagId)
    {
        // Retrieve the habit tag from the database
        var habitTag = await dbContext.HabitTags
            .SingleOrDefaultAsync(ht => ht.HabitId == habitId && ht.TagId == tagId);

        // If the habit tag does not exist, return a 404 Not Found response
        if (habitTag is null)
        {
            return NotFound();
        }

        // Remove the habit tag from the database
        dbContext.HabitTags.Remove(habitTag);

        // Save the changes to the database
        await dbContext.SaveChangesAsync();

        // Return a 204 No Content response
        return NoContent();
    }
}
