using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using URMS.Application.Common.Helpers;
using URMS.Application.Contracts.Persistence;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Application.Services;

public class RequestWorkflowService : IRequestWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOtpService _otpService;
    private readonly IRequestNotificationService _notificationService;
    private readonly EmailSettings _emailSettings;

    public RequestWorkflowService(
        IUnitOfWork unitOfWork,
        IOtpService otpService,
        IRequestNotificationService notificationService,
        IOptions<EmailSettings> emailOptions)
    {
        _unitOfWork = unitOfWork;
        _otpService = otpService;
        _notificationService = notificationService;
        _emailSettings = emailOptions.Value;
    }

    public async Task<Result<UniversityRequestResponseDto>> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.Status != RequestStatus.Pending && request.Status != RequestStatus.AdvisorApproved)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForAdvisorReview);

        request.AdvisorId = advisorId;
        request.AdvisorReviewedAt = DateTime.UtcNow;

        var oldStatus = request.Status;

        if (dto.IsApproved)
        {
            request.Status = RequestStatus.AdvisorApproved;
        }
        else
        {
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = dto.RejectionReason;
        }

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = advisorId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = dto.IsApproved ? RequestLogMessages.ApprovedByAdvisor : RequestLogMessages.RejectedByAdvisor,
            Notes = dto.RejectionReason
        });

        await _unitOfWork.CompleteAsync();

        var advisor = await userRepo.GetByIdAsync(advisorId);
        return Result.Success(request.MapToDto(request.Student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> SendRequestToAdministrationAsync(int requestId, SendRequestToAdministrationDto dto, string advisorId)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition!).ThenInclude(f => f.Fields)
                  .Include(r => r.Advisor)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.Status == RequestStatus.Completed || request.Status == RequestStatus.Rejected)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForSendEmail);

        if (request.Status != RequestStatus.AdvisorApproved)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForSendEmail);

        request.AdvisorId = advisorId;
        request.AdvisorReviewedAt = DateTime.UtcNow;
        var oldStatus = request.Status;
        request.Status = RequestStatus.SentToAdministration;
        request.ExternalAdministrationEmail = dto.AdministrationEmail;
        request.ExternalAdministrationSentAt = DateTime.UtcNow;

        var otpCode = _otpService.GenerateOtpCode();
        var now = DateTime.UtcNow;
        var confirmationToken = Guid.NewGuid().ToString("N");
        request.ExternalAdministrationOtpSentAt = now;
        request.ExternalAdministrationOtpExpiresAt = now.AddMinutes(_emailSettings.ExternalAdministrationOtpTtlMinutes);
        request.ConfirmationToken = confirmationToken;
        request.ExternalAdministrationOtpCodeHash = _otpService.HashOtp(otpCode, confirmationToken);
        request.ExternalAdministrationResponseNotes = null;
        request.ExternalAdministrationRespondedAt = null;

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = advisorId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = RequestLogMessages.SentToAdministration,
            Notes = dto.Message
        });

        var requestUrl = _emailSettings.ExternalAdministrationBaseUrl.TrimEnd('/');
        var reviewLink = $"{requestUrl}/{confirmationToken}";

        var sendEmailResult = await _notificationService.SendExternalAdministrationEmailAsync(request, dto, otpCode, reviewLink);
        if (sendEmailResult.IsFailure)
        {
            return Result.Failure<UniversityRequestResponseDto>(sendEmailResult.Error);
        }

        await _unitOfWork.CompleteAsync();

        var advisor = await userRepo.GetByIdAsync(advisorId);
        return Result.Success(request.MapToDto(request.Student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> RespondExternalRequestAsync(string token, ExternalAdministrationResponseDto dto)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var request = await requestRepo.FindOneAsync(
            r => r.ConfirmationToken == token,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.ExternalAdministrationRespondedAt.HasValue)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForAdministrationConfirm);

        if (request.Status != RequestStatus.SentToAdministration)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForAdministrationConfirm);

        if (string.IsNullOrWhiteSpace(dto.Otp) || string.IsNullOrWhiteSpace(request.ExternalAdministrationOtpCodeHash) || !request.ExternalAdministrationOtpExpiresAt.HasValue)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidExternalAdministrationOtp);

        if (request.ExternalAdministrationOtpExpiresAt.Value < DateTime.UtcNow)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidExternalAdministrationOtp);

        if (!_otpService.VerifyOtp(dto.Otp, token, request.ExternalAdministrationOtpCodeHash))
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidExternalAdministrationOtp);

        var oldStatus = request.Status;
        request.ExternalAdministrationRespondedAt = DateTime.UtcNow;
        request.ExternalAdministrationResponseNotes = dto.Notes;
        request.ConfirmationToken = null;
        request.ExternalAdministrationOtpCodeHash = null;
        request.ExternalAdministrationOtpSentAt = null;
        request.ExternalAdministrationOtpExpiresAt = null;

        if (dto.IsApproved)
        {
            request.Status = RequestStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = dto.Notes;
        }

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = request.StudentId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = RequestLogMessages.ExternalAdministrationResponded,
            Notes = dto.Notes
        });

        await _unitOfWork.CompleteAsync();
        return Result.Success(request.MapToDto(request.Student, request.Advisor, request.Administration));
    }

    public async Task<Result<UniversityRequestResponseDto>> ConfirmByAdministrationAsync(int requestId, string administrationId, AdministrationConfirmRequestDto dto)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.Advisor)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.Status != RequestStatus.SentToAdministration)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForAdministrationConfirm);

        request.AdministrationId = administrationId;
        request.AdministrationConfirmedAt = DateTime.UtcNow;

        var oldStatus = request.Status;

        if (dto.IsApproved)
        {
            request.Status = RequestStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = dto.ConfirmationNotes;
        }

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = administrationId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = dto.IsApproved ? RequestLogMessages.ConfirmedByAdministration : RequestLogMessages.RejectedByAdministration,
            Notes = dto.ConfirmationNotes
        });

        await _unitOfWork.CompleteAsync();

        var admin = await userRepo.GetByIdAsync(administrationId);
        return Result.Success(request.MapToDto(request.Student, request.Advisor, admin));
    }

    public async Task<Result<UniversityRequestResponseDto>> OverrideStatusByAdminAsync(int requestId, string adminId, AdminOverrideRequestDto dto)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.Advisor)
                  .Include(r => r.Administration)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (dto.TargetStatus == RequestStatus.SentToAdministration)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForAdminOverride);

        var oldStatus = request.Status;
        request.Status = dto.TargetStatus;

        if (dto.TargetStatus == RequestStatus.Completed)
        {
            request.CompletedAt = DateTime.UtcNow;
            request.AdministrationId = adminId;
            request.AdministrationConfirmedAt = DateTime.UtcNow;

            if (request.AdvisorReviewedAt == null)
            {
                request.AdvisorId = adminId;
                request.AdvisorReviewedAt = DateTime.UtcNow;
            }
        }
        else if (dto.TargetStatus == RequestStatus.Rejected)
        {
            request.RejectionReason = dto.ReasonOrNotes;
        }
        else if (dto.TargetStatus == RequestStatus.AdvisorApproved)
        {
            request.AdvisorId = adminId;
            request.AdvisorReviewedAt = DateTime.UtcNow;
        }

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = adminId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = RequestLogMessages.AdminOverride,
            Notes = dto.ReasonOrNotes
        });

        await _unitOfWork.CompleteAsync();

        var adminUser = await userRepo.GetByIdAsync(adminId);
        return Result.Success(request.MapToDto(request.Student, request.Advisor ?? adminUser, request.Administration ?? adminUser));
    }
}
