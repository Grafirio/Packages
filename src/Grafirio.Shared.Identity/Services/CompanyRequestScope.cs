using Microsoft.AspNetCore.Http;

namespace Grafirio.Shared.Identity.Services;

/// <summary>The same company selection is used by authorization and data access.</summary>
public static class CompanyRequestScope
{
    public const string HeaderName = "X-Company-Id";

    public static Guid? Resolve(HttpContext? context)
    {
        if (context is null) return null;
        var selections = new List<Guid>();
        Add(context.Request.Headers[HeaderName].ToArray());
        if (context.Request.RouteValues.TryGetValue("companyId", out var route)) Add([route?.ToString()]);
        Add(context.Request.Query["companyId"].ToArray());

        var claim = context.User.FindFirst("company_id")?.Value;
        Guid? tokenCompany = Guid.TryParse(claim, out var parsed) && parsed != Guid.Empty ? parsed : null;

        // Bridge credentials belong to one company. A browser selection must never retarget them.
        if (context.User.HasClaim(c => c.Type == "bridge_id") && selections.Any(id => id != tokenCompany))
            throw new BadHttpRequestException("Bridge company cannot be changed.");
        if (selections.Distinct().Count() > 1)
            throw new BadHttpRequestException("Company selections do not match.");
        return selections.Count > 0 ? selections[0] : tokenCompany;

        void Add(string?[] values)
        {
            if (values.Length == 0) return;
            if (values.Length != 1 || !Guid.TryParse(values[0], out var id) || id == Guid.Empty)
                throw new BadHttpRequestException("Invalid company selection.");
            selections.Add(id);
        }
    }
}
