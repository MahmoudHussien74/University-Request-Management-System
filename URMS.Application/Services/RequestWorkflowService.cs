using Microsoft.Extensions.Logging;
using URMS.Application.Common.Helpers;

namespace URMS.Application.Services;

public class RequestWorkflowService : IRequestWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUniversityRequestRepository _requestRepo;
    private readonly IOtpService _otpService;
    private readonly IRequestNotificationService _notificationService;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<RequestWorkflowService> _logger;

    public RequestWorkflowService(
        IUnitOfWork unitOfWork,
        IUniversityRequestRepository requestRepo,
        IOtpService otpService,
        IRequestNotificationService notificationService,
        IOptions<EmailSettings> emailOptions,
        ILogger<RequestWorkflowService> logger)
    {
        _unitOfWork = unitOfWork;
        _requestRepo = requestRepo;
        _otpService = otpService;
        _notificationService = notificationService;
        _emailSettings = emailOptions.Value;
        _logger = logger;
    }

    public async Task<Result<UniversityRequestResponseDto>> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto)
    {
        var request = await _requestRepo.GetForWorkflowAsync(requestId);

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        var result = request.ReviewByAdvisor(advisorId, dto.IsApproved, dto.RejectionReason);
        if (result.IsFailure)
            return Result.Failure<UniversityRequestResponseDto>(result.Error);

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Request {RequestId} reviewed by advisor {AdvisorId}: Approved={IsApproved}",
            requestId, advisorId, dto.IsApproved);

        var advisor = await _requestRepo.GetUserByIdAsync(advisorId);
        return Result.Success(request.MapToDto(request.Student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> SendRequestToAdministrationAsync(int requestId, SendRequestToAdministrationDto dto, string advisorId)
    {
        var request = await _requestRepo.GetForAdministrationSendAsync(requestId);

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        // Infrastructure: generate OTP & token
        var otpCode = _otpService.GenerateOtpCode();
        var confirmationToken = Guid.NewGuid().ToString("N");
        var otpExpiresAt = DateTime.UtcNow.AddMinutes(_emailSettings.ExternalAdministrationOtpTtlMinutes);
        var otpCodeHash = _otpService.HashOtp(otpCode, confirmationToken);

        // Domain: state transition
        var result = request.SendToAdministration(
            advisorId, dto.AdministrationEmail,
            otpCodeHash, confirmationToken, otpExpiresAt, dto.Message);
        if (result.IsFailure)
            return Result.Failure<UniversityRequestResponseDto>(result.Error);

        // Infrastructure: send email
        var requestUrl = _emailSettings.ExternalAdministrationBaseUrl.TrimEnd('/');
        var reviewLink = $"{requestUrl}/{confirmationToken}";

        var sendEmailResult = await _notificationService.SendExternalAdministrationEmailAsync(request, dto, otpCode, reviewLink);
        if (sendEmailResult.IsFailure)
        {
            return Result.Failure<UniversityRequestResponseDto>(sendEmailResult.Error);
        }

        await _unitOfWork.CompleteAsync();

        var advisor = await _requestRepo.GetUserByIdAsync(advisorId);
        return Result.Success(request.MapToDto(request.Student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> RespondExternalRequestAsync(string token, ExternalAdministrationResponseDto dto)
    {
        var request = await _requestRepo.GetByTokenForWorkflowAsync(token);

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        // Infrastructure: OTP validation (before domain state transition)
        if (string.IsNullOrWhiteSpace(dto.Otp) || string.IsNullOrWhiteSpace(request.ExternalAdministrationOtpCodeHash) || !request.ExternalAdministrationOtpExpiresAt.HasValue)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidExternalAdministrationOtp);

        if (request.ExternalAdministrationOtpExpiresAt.Value < DateTime.UtcNow)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidExternalAdministrationOtp);

        if (!_otpService.VerifyOtp(dto.Otp, token, request.ExternalAdministrationOtpCodeHash))
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidExternalAdministrationOtp);

        // Domain: state transition
        var result = request.RespondByExternalAdministration(dto.IsApproved, dto.Notes);
        if (result.IsFailure)
            return Result.Failure<UniversityRequestResponseDto>(result.Error);

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("External administration responded to request {RequestId}: Approved={IsApproved}",
            request.Id, dto.IsApproved);

        return Result.Success(request.MapToDto(request.Student, request.Advisor, request.Administration));
    }

    public async Task<Result<UniversityRequestResponseDto>> ConfirmByAdministrationAsync(int requestId, string administrationId, AdministrationConfirmRequestDto dto)
    {
        var request = await _requestRepo.GetForWorkflowAsync(requestId);

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        var result = request.ConfirmByAdministration(administrationId, dto.IsApproved, dto.ConfirmationNotes);
        if (result.IsFailure)
            return Result.Failure<UniversityRequestResponseDto>(result.Error);

        await _unitOfWork.CompleteAsync();

        var admin = await _requestRepo.GetUserByIdAsync(administrationId);
        return Result.Success(request.MapToDto(request.Student, request.Advisor, admin));
    }

    public async Task<Result<UniversityRequestResponseDto>> OverrideStatusByAdminAsync(int requestId, string adminId, AdminOverrideRequestDto dto)
    {
        var request = await _requestRepo.GetForWorkflowAsync(requestId);

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        var result = request.OverrideStatusByAdmin(adminId, dto.TargetStatus, dto.ReasonOrNotes);
        if (result.IsFailure)
            return Result.Failure<UniversityRequestResponseDto>(result.Error);

        await _unitOfWork.CompleteAsync();

        var adminUser = await _requestRepo.GetUserByIdAsync(adminId);
        return Result.Success(request.MapToDto(request.Student, request.Advisor ?? adminUser, request.Administration ?? adminUser));
    }
}
