namespace OrdoWiki.Web.Services.Contract;

using Models;
using Models.Requests;

public interface ITimelineService
{
    Task<ApiResponse<PagedResult<TimelineEventDto>>> GetEventsAsync(TimelineEventFilter filter);
    Task<ApiResponse<TimelineEventDto>> GetEventByIdAsync(Guid id);
    Task<ApiResponse<TimelineEventDto>> CreateAsync(CreateTimelineEventRequest request);
    Task<ApiResponse<TimelineEventDto>> UpdateAsync(UpdateTimelineEventRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
