namespace URMS.Application.Contracts.Requests;

public interface IUniversityRequestService :
    IRequestCreationService,
    IRequestWorkflowService,
    IRequestQueryService
{
}
