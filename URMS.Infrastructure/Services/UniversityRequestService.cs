using System.Security.Cryptography;
using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using URMS.Application.Contracts.Persistence;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;
using URMS.Domain.Entities;
using URMS.Domain.Enums;
using URMS.Domain.Settings;
namespace URMS.Infrastructure.Services;

public class UniversityRequestService : IUniversityRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly URMS.Application.Contracts.Forms.IFormDefinitionService _formService;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;

    public UniversityRequestService(
        IUnitOfWork unitOfWork,
        URMS.Application.Contracts.Forms.IFormDefinitionService formService,
        IEmailService emailService,
        IOptions<EmailSettings> emailOptions)
    {
        _unitOfWork = unitOfWork;
        _formService = formService;
        _emailService = emailService;
        _emailSettings = emailOptions.Value;
    }

    public async Task<Result<UniversityRequestResponseDto>> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var student = await userRepo.FindOneAsync(
            u => u.Id == studentId,
            q => q.Include(u => u.Student)
        );

        if (student is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.StudentNotFound);

        if (!student.IsApproved)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.StudentNotApproved);

        // Validate dynamic form rules (FormDefinitionId is now required)
        var formValResult = await _formService.ValidateSubmissionAnswersAsync(dto.FormDefinitionId, dto.AdditionalData);
        if (formValResult.IsFailure)
            return Result.Failure<UniversityRequestResponseDto>(formValResult.Error);

        string? jsonMetadata = dto.AdditionalData is not null && dto.AdditionalData.Count > 0
            ? JsonSerializer.Serialize(dto.AdditionalData)
            : null;

        // Auto-assign the student's academic advisor to the request
        var advisorId = student.Student?.AcademicAdvisorId;
        ApplicationUser? advisor = null;
        if (!string.IsNullOrEmpty(advisorId))
        {
            advisor = await userRepo.GetByIdAsync(advisorId);
        }

        var request = new UniversityRequest
        {
            StudentId = studentId,
            FormDefinitionId = dto.FormDefinitionId,
            Status = RequestStatus.Pending,
            AdditionalDataJson = jsonMetadata,
            AdvisorId = advisorId,
            CreatedAt = DateTime.UtcNow
        };

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = studentId,
            OldStatus = RequestStatus.Pending,
            NewStatus = RequestStatus.Pending,
            ActionMessage = RequestLogMessages.CreatedByStudent
        });

        await requestRepo.AddAsync(request);
        await _unitOfWork.CompleteAsync();

        var formRepo = _unitOfWork.Repository<FormDefinition>();
        request.FormDefinition = await formRepo.GetByIdAsync(request.FormDefinitionId!.Value);

        return Result.Success(MapToDto(request, student, advisor, null));
    }


    public List<RequestStatusInfoDto> GetRequestStatuses()
    {
        return new List<RequestStatusInfoDto>
        {
            new(0, RequestStatus.Pending.ToString(), "معلق (في انتظار مراجعة المرشد)", "Pending Advisor Review"),
            new(1, RequestStatus.AdvisorApproved.ToString(), "موافق عليه من المرشد الأكاديمي", "Approved by Advisor"),
            new(2, RequestStatus.SentToAdministration.ToString(), "أُرسِل إلى شؤون الطلاب / الإدارة", "Sent to Administration"),
            new(3, RequestStatus.Completed.ToString(), "مكتمل / تم التنفيذ", "Completed"),
            new(4, RequestStatus.Rejected.ToString(), "مرفوض", "Rejected")
        };
    }

    public async Task<Result<List<UniversityRequestResponseDto>>> GetMyRequestsAsync(string studentId)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var requests = await requestRepo.FindAllAsync(
            r => r.StudentId == studentId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.Administration)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy),
            orderBy: q => q.OrderByDescending(r => r.CreatedAt)
        );

        return Result.Success(requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Administration)).ToList());
    }

    public async Task<Result<List<UniversityRequestResponseDto>>> GetAllRequestsAsync(RequestStatus? status = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var requests = await requestRepo.FindAllAsync(
            r => !status.HasValue || r.Status == status.Value,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy),
            orderBy: q => q.OrderByDescending(r => r.CreatedAt)
        );

        return Result.Success(requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Administration)).ToList());
    }

    public async Task<Result<List<UniversityRequestResponseDto>>> GetAdvisorRequestsAsync(string advisorId, RequestStatus? status = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var requests = await requestRepo.FindAllAsync(
            r => (r.AdvisorId == advisorId || (r.Student.Student != null && r.Student.Student.AcademicAdvisorId == advisorId))
                 && (!status.HasValue || r.Status == status.Value),
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.Administration)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy),
            orderBy: q => q.OrderByDescending(r => r.CreatedAt)
        );

        return Result.Success(requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Administration)).ToList());
    }

    public async Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.Administration)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        return Result.Success(MapToDto(request, request.Student, request.Advisor, request.Administration));
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
        return Result.Success(MapToDto(request, request.Student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> SendRequestToAdministrationAsync(int requestId, SendRequestToAdministrationDto dto, string advisorId)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition).ThenInclude(f => f.Fields)
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

        var otpCode = GenerateOtpCode();
        var now = DateTime.UtcNow;
        request.ExternalAdministrationOtpSentAt = now;
        request.ExternalAdministrationOtpExpiresAt = now.AddMinutes(_emailSettings.ExternalAdministrationOtpTtlMinutes);
        request.ExternalAdministrationOtpCodeHash = HashOtp(otpCode);
        request.ConfirmationToken = Guid.NewGuid().ToString("N");
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

        await _unitOfWork.CompleteAsync();

        Dictionary<string, string>? additionalData = null;
        if (!string.IsNullOrEmpty(request.AdditionalDataJson))
        {
            try
            {
                additionalData = JsonSerializer.Deserialize<Dictionary<string, string>>(request.AdditionalDataJson);
            }
            catch
            {
                additionalData = null;
            }
        }

        static string BuildAdditionalDataHtml(FormDefinition? formDefinition, Dictionary<string, string>? additionalData)
        {
            if (additionalData is null || !additionalData.Any())
                return string.Empty;

            var rows = new List<string>();
            foreach (var kvp in additionalData)
            {
                var fieldLabel = formDefinition?.Fields?.FirstOrDefault(f => f.FieldKey == kvp.Key)?.LabelEn ?? kvp.Key;
                var fieldValue = System.Net.WebUtility.HtmlEncode(kvp.Value);
                rows.Add($"<tr><td style='padding:8px;border:1px solid #ddd;'><strong>{fieldLabel}</strong></td><td style='padding:8px;border:1px solid #ddd;'>{fieldValue}</td></tr>");
            }

            return $@"
                <p><strong>Student Answers:</strong></p>
                <table style='border-collapse:collapse;width:100%;margin-bottom:16px;'>
                    {string.Join("", rows)}
                </table>
            ";
        }

        var requestUrl = _emailSettings.ExternalAdministrationBaseUrl.TrimEnd('/');
        var token = request.ConfirmationToken!;
        var approveLink = $"{requestUrl}/request/{token}/approve";
        var rejectLink = $"{requestUrl}/request/{token}/reject";

        var studentName = request.Student.FullNameEn;
        var formTitle = request.FormDefinition?.TitleEn ?? "University Request";
        var answersHtml = BuildAdditionalDataHtml(request.FormDefinition, additionalData);
        var subject = $"New student request for administration - {studentName}";
        var body = $@"
            <p>Hello,</p>
            <p>A new student request has been submitted for your review by the academic advisor.</p>
            <p><strong>Student Name:</strong> {studentName}</p>
            <p><strong>Request Type:</strong> {formTitle}</p>
            {answersHtml}
            <p><strong>Advisor Message:</strong> {System.Net.WebUtility.HtmlEncode(dto.Message ?? string.Empty)}</p>
            <p><strong>Verification Code:</strong> {otpCode}</p>
            <p>This code expires after {_emailSettings.ExternalAdministrationOtpTtlMinutes} minutes.</p>
            <p>You may respond using one of the following links:</p>
            <p><a href='{approveLink}'>Approve Request</a></p>
            <p><a href='{rejectLink}'>Reject Request</a></p>
            <p>If the interface is not displayed, please open the link in a browser that supports requests.</p>
        ";

        await _emailService.SendEmailAsync(dto.AdministrationEmail, subject, body);

        var advisor = await userRepo.GetByIdAsync(advisorId);
        return Result.Success(MapToDto(request, request.Student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> GetRequestByTokenAsync(string token)
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

        return Result.Success(MapToDto(request, request.Student, request.Advisor, request.Administration));
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

        if (!VerifyOtp(dto.Otp, request.ExternalAdministrationOtpCodeHash))
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
        return Result.Success(MapToDto(request, request.Student, request.Advisor, request.Administration));
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
        return Result.Success(MapToDto(request, request.Student, request.Advisor, admin));
    }

    public async Task<Result<UniversityRequestResponseDto>> WithdrawRequestAsync(int requestId, string studentId)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.Administration)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.StudentId != studentId)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.Status != RequestStatus.Pending)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForWithdraw);

        var oldStatus = request.Status;
        request.Status = RequestStatus.Rejected;
        request.RejectionReason = "تم سحب الطلب بواسطة الطالب";

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = studentId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = RequestLogMessages.WithdrawnByStudent,
            Notes = request.RejectionReason
        });

        await _unitOfWork.CompleteAsync();
        return Result.Success(MapToDto(request, request.Student, request.Advisor, request.Administration));
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
        return Result.Success(MapToDto(request, request.Student, request.Advisor ?? adminUser, request.Administration ?? adminUser));
    }

    private static UniversityRequestResponseDto MapToDto(
        UniversityRequest request,
        ApplicationUser student,
        ApplicationUser? advisor,
        ApplicationUser? staff)
    {
        Dictionary<string, string>? additionalData = null;
        if (!string.IsNullOrEmpty(request.AdditionalDataJson))
        {
            try
            {
                additionalData = JsonSerializer.Deserialize<Dictionary<string, string>>(request.AdditionalDataJson);
            }
            catch
            {
                // Fallback if parsing fails
            }
        }

        List<RequestHistoryLogDto>? historyLogsDto = null;
        if (request.HistoryLogs != null && request.HistoryLogs.Any())
        {
            historyLogsDto = request.HistoryLogs.OrderByDescending(l => l.ActionDate)
                .Select(l => new RequestHistoryLogDto(
                    l.ActionBy?.FullNameAr ?? "System",
                    l.OldStatus.ToString(),
                    l.NewStatus.ToString(),
                    l.ActionMessage,
                    l.ActionDate
                )).ToList();
        }

        var dto = request.Adapt<UniversityRequestResponseDto>();

        var statusNames = GetStatusDisplay(request.Status, advisor?.FullNameAr);
        var nextAction = GetNextAction(request.Status, advisor?.FullNameAr);
        var nextActionEn = GetNextActionEn(request.Status, advisor?.FullNameEn);

        return dto with
        {
            StudentNameAr = student.FullNameAr,
            StudentNameEn = student.FullNameEn,
            UniversityCode = student.Student?.UniversityCode,
            AdvisorName = advisor?.FullNameAr,
            AdditionalData = additionalData,
            HistoryLogs = historyLogsDto,
            StatusAr = statusNames.StatusAr,
            StatusEn = statusNames.StatusEn,
            NextAction = nextAction,
            NextActionEn = nextActionEn,
            CanWithdraw = request.Status == RequestStatus.Pending,
            CanConfirm = request.Status == RequestStatus.SentToAdministration
        };
    }

    private static (string StatusAr, string StatusEn) GetStatusDisplay(RequestStatus status, string? advisorName)
    {
        return status switch
        {
            RequestStatus.Pending => ("في انتظار مراجعة المرشد الأكاديمي", "Pending Advisor Review"),
            RequestStatus.AdvisorApproved => ("موافق عليه من المرشد الأكاديمي", "Approved by Advisor"),
            RequestStatus.SentToAdministration => ("أُرسِل إلى شؤون الطلاب / الإدارة", "Sent to Administration"),
            RequestStatus.Completed => ("مكتمل، تم تنفيذ الطلب", "Completed"),
            RequestStatus.Rejected => ("مرفوض", "Rejected"),
            _ => (status.ToString(), status.ToString())
        };
    }

    private static string GetNextAction(RequestStatus status, string? advisorName)
    {
        return status switch
        {
            RequestStatus.Pending => advisorName is not null ? $"في انتظار مراجعة المرشد الأكاديمي {advisorName}" : "في انتظار مراجعة المرشد الأكاديمي",
            RequestStatus.AdvisorApproved => "الطلب الآن مع شؤون الطلاب للمراجعة",
            RequestStatus.SentToAdministration => "الطلب الآن مع شؤون الطلاب/الإدارة للمراجعة",
            RequestStatus.Completed => "تم تنفيذ الطلب بنجاح",
            RequestStatus.Rejected => "الطلب مرفوض.",
            _ => string.Empty
        };
    }

    private static string GetNextActionEn(RequestStatus status, string? advisorName)
    {
        return status switch
        {
            RequestStatus.Pending => advisorName is not null ? $"Awaiting review by Advisor {advisorName}" : "Awaiting review by Advisor",
            RequestStatus.AdvisorApproved => "Request is now with staff for confirmation",
            RequestStatus.SentToAdministration => "Request is now with administration for review",
            RequestStatus.Completed => "Request completed successfully",
            RequestStatus.Rejected => "Request rejected.",
            _ => string.Empty
        };
    }

    private static string GenerateOtpCode(int length = 6)
    {
        const string digits = "0123456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => digits[b % digits.Length]).ToArray());
    }

    private static string HashOtp(string otp)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(otp);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyOtp(string otp, string hash)
    {
        var otpHash = HashOtp(otp);
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(otpHash),
            System.Text.Encoding.UTF8.GetBytes(hash));
    }

}
