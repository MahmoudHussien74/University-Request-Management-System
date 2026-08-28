namespace URMS.Application.Common.Helpers;

public static class RequestMappingExtensions
{
    public static UniversityRequestResponseDto MapToDto(
        this UniversityRequest request,
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
            catch (JsonException)
            {
                // Invalid JSON in AdditionalDataJson — safe to ignore, field will be null
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

    public static (string StatusAr, string StatusEn) GetStatusDisplay(RequestStatus status, string? advisorName)
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

    public static string GetNextAction(RequestStatus status, string? advisorName)
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

    public static string GetNextActionEn(RequestStatus status, string? advisorName)
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
}
