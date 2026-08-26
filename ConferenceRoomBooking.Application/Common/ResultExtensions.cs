using ConferenceRoomBooking.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Application.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => new NotFoundObjectResult(new { error = result.Error }),
            ResultErrorType.Conflict => new ConflictObjectResult(new { error = result.Error }),
            _ => new BadRequestObjectResult(new { error = result.Error })
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => new NotFoundObjectResult(new { error = result.Error }),
            ResultErrorType.Conflict => new ConflictObjectResult(new { error = result.Error }),
            _ => new BadRequestObjectResult(new { error = result.Error })
        };
    }
}