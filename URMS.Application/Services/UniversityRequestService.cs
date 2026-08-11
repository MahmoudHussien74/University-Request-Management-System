namespace URMS.Application.Services;

public class UniversityRequestService : IUniversityRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFormDefinitionService _formService;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;

    public UniversityRequestService(
        IUnitOfWork unitOfWork,
        IFormDefinitionService formService,
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

    public async Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetMyRequestsAsync(
        string studentId,
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        IQueryable<UniversityRequest> query = requestRepo.GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .Where(r => r.StudentId == studentId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "title")
            {
                query = query.Where(r => r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term)));
            }
            else if (col == "reason" || col == "rejectionreason")
            {
                query = query.Where(r => r.RejectionReason != null && r.RejectionReason.ToLower().Contains(term));
            }
            else
            {
                query = query.Where(r =>
                    (r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term))) ||
                    (r.RejectionReason != null && r.RejectionReason.ToLower().Contains(term))
                );
            }
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        List<UniversityRequest> requests;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            requests = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            requests = await query.ToListAsync();
        }

        var dtos = requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Administration)).ToList();

        var paginatedResult = new PaginatedList<UniversityRequestResponseDto>(dtos, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetAllRequestsAsync(
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        IQueryable<UniversityRequest> query = requestRepo.GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "title")
            {
                query = query.Where(r => r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term)));
            }
            else if (col == "code" || col == "universitycode")
            {
                query = query.Where(r => r.Student != null && r.Student.Student != null && r.Student.Student.UniversityCode.ToLower().Contains(term));
            }
            else if (col == "studentname" || col == "name")
            {
                query = query.Where(r => r.Student != null && (
                    r.Student.FirstNameAr.ToLower().Contains(term) || r.Student.LastNameAr.ToLower().Contains(term) ||
                    (r.Student.SecondNameAr != null && r.Student.SecondNameAr.ToLower().Contains(term)) ||
                    (r.Student.ThirdNameAr != null && r.Student.ThirdNameAr.ToLower().Contains(term)) ||
                    r.Student.FirstNameEn.ToLower().Contains(term) || r.Student.LastNameEn.ToLower().Contains(term) ||
                    (r.Student.SecondNameEn != null && r.Student.SecondNameEn.ToLower().Contains(term)) ||
                    (r.Student.ThirdNameEn != null && r.Student.ThirdNameEn.ToLower().Contains(term))
                ));
            }
            else if (col == "advisorname")
            {
                query = query.Where(r => r.Advisor != null && (
                    r.Advisor.FirstNameAr.ToLower().Contains(term) || r.Advisor.LastNameAr.ToLower().Contains(term) ||
                    (r.Advisor.SecondNameAr != null && r.Advisor.SecondNameAr.ToLower().Contains(term)) ||
                    (r.Advisor.ThirdNameAr != null && r.Advisor.ThirdNameAr.ToLower().Contains(term)) ||
                    r.Advisor.FirstNameEn.ToLower().Contains(term) || r.Advisor.LastNameEn.ToLower().Contains(term) ||
                    (r.Advisor.SecondNameEn != null && r.Advisor.SecondNameEn.ToLower().Contains(term)) ||
                    (r.Advisor.ThirdNameEn != null && r.Advisor.ThirdNameEn.ToLower().Contains(term))
                ));
            }
            else
            {
                query = query.Where(r =>
                    (r.Student != null && (
                        r.Student.FirstNameAr.ToLower().Contains(term) || r.Student.LastNameAr.ToLower().Contains(term) ||
                        (r.Student.SecondNameAr != null && r.Student.SecondNameAr.ToLower().Contains(term)) ||
                        (r.Student.ThirdNameAr != null && r.Student.ThirdNameAr.ToLower().Contains(term)) ||
                        r.Student.FirstNameEn.ToLower().Contains(term) || r.Student.LastNameEn.ToLower().Contains(term) ||
                        (r.Student.SecondNameEn != null && r.Student.SecondNameEn.ToLower().Contains(term)) ||
                        (r.Student.ThirdNameEn != null && r.Student.ThirdNameEn.ToLower().Contains(term)) ||
                        (r.Student.Student != null && r.Student.Student.UniversityCode.ToLower().Contains(term))
                    )) ||
                    (r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term))) ||
                    (r.Advisor != null && (
                        r.Advisor.FirstNameAr.ToLower().Contains(term) || r.Advisor.LastNameAr.ToLower().Contains(term) ||
                        (r.Advisor.SecondNameAr != null && r.Advisor.SecondNameAr.ToLower().Contains(term)) ||
                        (r.Advisor.ThirdNameAr != null && r.Advisor.ThirdNameAr.ToLower().Contains(term)) ||
                        r.Advisor.FirstNameEn.ToLower().Contains(term) || r.Advisor.LastNameEn.ToLower().Contains(term) ||
                        (r.Advisor.SecondNameEn != null && r.Advisor.SecondNameEn.ToLower().Contains(term)) ||
                        (r.Advisor.ThirdNameEn != null && r.Advisor.ThirdNameEn.ToLower().Contains(term))
                    ))
                );
            }
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        List<UniversityRequest> requests;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            requests = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            requests = await query.ToListAsync();
        }

        var dtos = requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Administration)).ToList();

        var paginatedResult = new PaginatedList<UniversityRequestResponseDto>(dtos, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetAdvisorRequestsAsync(
        string advisorId,
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        IQueryable<UniversityRequest> query = requestRepo.GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .Where(r => r.AdvisorId == advisorId || (r.Student.Student != null && r.Student.Student.AcademicAdvisorId == advisorId));

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "title")
            {
                query = query.Where(r => r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term)));
            }
            else if (col == "code" || col == "universitycode")
            {
                query = query.Where(r => r.Student != null && r.Student.Student != null && r.Student.Student.UniversityCode.ToLower().Contains(term));
            }
            else if (col == "studentname" || col == "name")
            {
                query = query.Where(r => r.Student != null && (
                    r.Student.FirstNameAr.ToLower().Contains(term) || r.Student.LastNameAr.ToLower().Contains(term) ||
                    (r.Student.SecondNameAr != null && r.Student.SecondNameAr.ToLower().Contains(term)) ||
                    (r.Student.ThirdNameAr != null && r.Student.ThirdNameAr.ToLower().Contains(term)) ||
                    r.Student.FirstNameEn.ToLower().Contains(term) || r.Student.LastNameEn.ToLower().Contains(term) ||
                    (r.Student.SecondNameEn != null && r.Student.SecondNameEn.ToLower().Contains(term)) ||
                    (r.Student.ThirdNameEn != null && r.Student.ThirdNameEn.ToLower().Contains(term))
                ));
            }
            else
            {
                query = query.Where(r =>
                    (r.Student != null && (
                        r.Student.FirstNameAr.ToLower().Contains(term) || r.Student.LastNameAr.ToLower().Contains(term) ||
                        (r.Student.SecondNameAr != null && r.Student.SecondNameAr.ToLower().Contains(term)) ||
                        (r.Student.ThirdNameAr != null && r.Student.ThirdNameAr.ToLower().Contains(term)) ||
                        r.Student.FirstNameEn.ToLower().Contains(term) || r.Student.LastNameEn.ToLower().Contains(term) ||
                        (r.Student.SecondNameEn != null && r.Student.SecondNameEn.ToLower().Contains(term)) ||
                        (r.Student.ThirdNameEn != null && r.Student.ThirdNameEn.ToLower().Contains(term)) ||
                        (r.Student.Student != null && r.Student.Student.UniversityCode.ToLower().Contains(term))
                    )) ||
                    (r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term)))
                );
            }
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        List<UniversityRequest> requests;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            requests = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            requests = await query.ToListAsync();
        }

        var dtos = requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Administration)).ToList();

        var paginatedResult = new PaginatedList<UniversityRequestResponseDto>(dtos, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId, string callerUserId, IList<string> callerRoles)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.AsNoTracking()
                  .AsSplitQuery()
                  .Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.Administration)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        // IDOR Protection / Ownership & Access Check
        var isSuperAdminOrSecretary = callerRoles.Contains(AppRoles.SuperAdmin) || callerRoles.Contains(AppRoles.CollegeSecretary);
        var isOwnerStudent = request.StudentId == callerUserId;
        var isAssignedAdvisor = request.AdvisorId == callerUserId || request.Student?.Student?.AcademicAdvisorId == callerUserId;

        if (!isSuperAdminOrSecretary && !isOwnerStudent && !isAssignedAdvisor)
        {
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.UnauthorizedAccess);
        }

        return Result.Success(MapToDto(request, request.Student!, request.Advisor, request.Administration));
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

        var otpCode = GenerateOtpCode();
        var now = DateTime.UtcNow;
        var confirmationToken = Guid.NewGuid().ToString("N");
        request.ExternalAdministrationOtpSentAt = now;
        request.ExternalAdministrationOtpExpiresAt = now.AddMinutes(_emailSettings.ExternalAdministrationOtpTtlMinutes);
        request.ConfirmationToken = confirmationToken;
        request.ExternalAdministrationOtpCodeHash = HashOtp(otpCode, confirmationToken);
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
        var reviewLink = $"{requestUrl}/{token}";

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
            <p>You may review and respond to this request using the following link:</p>
            <p><a href='{reviewLink}'>Review & Respond to Request</a></p>
        ";

        try
        {
            await _emailService.SendEmailAsync(dto.AdministrationEmail, subject, body);
        }
        catch
        {
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.EmailSendingFailed);
        }

        await _unitOfWork.CompleteAsync();

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

        if (!VerifyOtp(dto.Otp, token, request.ExternalAdministrationOtpCodeHash))
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
            var isEnglish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);

            historyLogsDto = request.HistoryLogs.OrderByDescending(l => l.ActionDate)
                .Select(l =>
                {
                    var oldDisplay = GetStatusDisplay(l.OldStatus, null);
                    var newDisplay = GetStatusDisplay(l.NewStatus, null);

                    var nameAr = l.ActionBy?.FullNameAr ?? "النظام";
                    var nameEn = l.ActionBy?.FullNameEn ?? "System";

                    var msgAr = l.ActionMessage;
                    var msgEn = RequestLogMessages.GetEnglishMessage(l.ActionMessage);

                    var nameDisplay = isEnglish ? nameEn : nameAr;
                    var oldStatusDisplay = isEnglish ? oldDisplay.StatusEn : oldDisplay.StatusAr;
                    var newStatusDisplay = isEnglish ? newDisplay.StatusEn : newDisplay.StatusAr;
                    var msgDisplay = isEnglish ? msgEn : msgAr;

                    return new RequestHistoryLogDto(
                        ActionByName: nameDisplay,
                        OldStatusName: oldStatusDisplay,
                        NewStatusName: newStatusDisplay,
                        ActionMessage: msgDisplay,
                        ActionDate: l.ActionDate,
                        ActionByNameAr: nameAr,
                        ActionByNameEn: nameEn,
                        OldStatusNameAr: oldDisplay.StatusAr,
                        OldStatusNameEn: oldDisplay.StatusEn,
                        NewStatusNameAr: newDisplay.StatusAr,
                        NewStatusNameEn: newDisplay.StatusEn,
                        ActionMessageAr: msgAr,
                        ActionMessageEn: msgEn,
                        Notes: l.Notes
                    );
                }).ToList();
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

    private static string HashOtp(string otp, string saltKey)
    {
        using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(saltKey));
        var bytes = System.Text.Encoding.UTF8.GetBytes(otp);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyOtp(string otp, string saltKey, string hash)
    {
        var computedHash = HashOtp(otp, saltKey);
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(computedHash),
            System.Text.Encoding.UTF8.GetBytes(hash));
    }
}
